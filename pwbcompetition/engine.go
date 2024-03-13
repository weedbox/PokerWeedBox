package pwbcompetition

import (
	"encoding/json"
	"errors"
	"fmt"
	"sync"
	"time"

	"github.com/thoas/go-funk"
	pwbblind "github.com/weedbox/PokerWeedBox/pwbcompetition/blind"
	"github.com/weedbox/PokerWeedBox/pwbtable"
	"github.com/weedbox/timebank"
)

var (
	ErrCompetitionInvalidCreateSetting            = errors.New("competition: invalid create competition setting")
	ErrCompetitionStartRejected                   = errors.New("competition: already started")
	ErrCompetitionUpdateBlindInitialLevelRejected = errors.New("competition: not allowed to update blind initial level")
	ErrCompetitionLeaveRejected                   = errors.New("competition: not allowed to leave")
	ErrCompetitionNoRedeemChips                   = errors.New("competition: not redeem any chips")
	ErrCompetitionBuyInRejected                   = errors.New("competition: not allowed to buy in")
	ErrCompetitionReBuyRejected                   = errors.New("competition: not allowed to re-buy")
	ErrCompetitionPlayerNotFound                  = errors.New("competition: player not found")
	ErrCompetitionTableNotFound                   = errors.New("competition: table not found")
	ErrMatchTableReservePlayerFailed              = errors.New("competition: failed to balance player to table by match")
)

type CompetitionEngineOpt func(*competitionEngine)

type CompetitionEngine interface {
	// Others
	UpdateTable(table *pwbtable.Table)
	UpdateReserveTablePlayerState(tableID string, playerState *pwbtable.TablePlayerState)
	OnTableCreated(fn func(table *pwbtable.Table))

	// Events
	OnCompetitionUpdated(fn func(competition *Competition))
	OnCompetitionErrorUpdated(fn func(competition *Competition, err error))
	OnCompetitionPlayerUpdated(fn func(competitionID string, competitionPlayer *CompetitionPlayer))
	OnCompetitionPlayerCashOut(fn func(competitionID string, competitionPlayer *CompetitionPlayer))

	// Competition Actions
	GetCompetition() *Competition
	CreateCompetition(competitionSetting CompetitionSetting) (*Competition, error)
	CloseCompetition(endStatus CompetitionStateStatus) error
	StartCompetition() (int64, error)

	// Player Operations
	PlayerBuyIn(joinPlayer JoinPlayer) error
	PlayerCashOut(tableID, playerID string) error
}

type competitionEngine struct {
	mu                         sync.RWMutex
	competition                *Competition
	playerCaches               sync.Map // key: <competitionID.playerID>, value: PlayerCache
	tableOptions               *pwbtable.TableEngineOptions
	tableManagerBackend        TableManagerBackend
	onCompetitionUpdated       func(competition *Competition)
	onCompetitionErrorUpdated  func(competition *Competition, err error)
	onCompetitionPlayerUpdated func(competitionID string, competitionPlayer *CompetitionPlayer)
	onCompetitionPlayerCashOut func(competitionID string, competitionPlayer *CompetitionPlayer)
	breakingPauseResumeStates  map[string]map[int]bool // key: tableID, value: (k,v): (breaking blind level index, is resume from pause)
	blind                      pwbblind.Blind
	onTableCreated             func(table *pwbtable.Table)
}

func NewCompetitionEngine(opts ...CompetitionEngineOpt) CompetitionEngine {
	ce := &competitionEngine{
		playerCaches:               sync.Map{},
		onCompetitionUpdated:       func(competition *Competition) {},
		onCompetitionErrorUpdated:  func(competition *Competition, err error) {},
		onCompetitionPlayerUpdated: func(competitionID string, competitionPlayer *CompetitionPlayer) {},
		onCompetitionPlayerCashOut: func(competitionID string, competitionPlayer *CompetitionPlayer) {},
		blind:                      pwbblind.NewBlind(),
		onTableCreated:             func(table *pwbtable.Table) {},
	}

	for _, opt := range opts {
		opt(ce)
	}

	return ce
}

func WithTableOptions(opts *pwbtable.TableEngineOptions) CompetitionEngineOpt {
	return func(ce *competitionEngine) {
		ce.tableOptions = opts
	}
}

func WithTableManagerBackend(tmb TableManagerBackend) CompetitionEngineOpt {
	return func(ce *competitionEngine) {
		ce.tableManagerBackend = tmb
		ce.tableManagerBackend.OnTableUpdated(func(table *pwbtable.Table) {
			ce.UpdateTable(table)
		})
		ce.tableManagerBackend.OnTablePlayerReserved(func(tableID string, playerState *pwbtable.TablePlayerState) {
			ce.UpdateReserveTablePlayerState(tableID, playerState)
		})
	}
}

func (ce *competitionEngine) UpdateTable(table *pwbtable.Table) {
	tableIdx := ce.competition.FindTableIdx(func(t *pwbtable.Table) bool {
		return table.ID == t.ID
	})
	if tableIdx == UnsetValue {
		return
	}

	if ce.isEndStatus() {
		return
	}

	var cloneTable pwbtable.Table
	if encoded, err := json.Marshal(table); err == nil {
		json.Unmarshal(encoded, &cloneTable)
	} else {
		cloneTable = *table
	}
	ce.competition.State.Tables[tableIdx] = &cloneTable

	tableStatusHandlerMap := map[pwbtable.TableStateStatus]func(*pwbtable.Table, int){
		pwbtable.TableStateStatus_TableCreated:     ce.handleCompetitionTableCreated,
		pwbtable.TableStateStatus_TablePausing:     ce.updatePauseCompetition,
		pwbtable.TableStateStatus_TableClosed:      ce.closeCompetitionTable,
		pwbtable.TableStateStatus_TableGameSettled: ce.settleCompetitionTable,
	}
	handler, ok := tableStatusHandlerMap[table.State.Status]
	if !ok {
		return
	}
	handler(table, tableIdx)
}

func (ce *competitionEngine) UpdateReserveTablePlayerState(tableID string, playerState *pwbtable.TablePlayerState) {
	playerCache, exist := ce.getPlayerCache(ce.competition.ID, playerState.PlayerID)
	if !exist {
		return
	}

	cp := ce.competition.State.Players[playerCache.PlayerIdx]
	cp.CurrentSeat = playerState.Seat
	cp.CurrentTableID = tableID
	cp.Status = CompetitionPlayerStatus_Playing
	ce.emitPlayerEvent("[UpdateReserveTablePlayerState] player table seat updated", cp)
	ce.emitEvent(fmt.Sprintf("[UpdateReserveTablePlayerState] player (%s) is reserved to table (%s) at seat (%d)", cp.PlayerID, cp.CurrentTableID, cp.CurrentSeat), cp.PlayerID)
}

func (ce *competitionEngine) OnTableCreated(fn func(table *pwbtable.Table)) {
	ce.onTableCreated = fn
}

func (ce *competitionEngine) OnCompetitionUpdated(fn func(competition *Competition)) {
	ce.onCompetitionUpdated = fn
}

func (ce *competitionEngine) OnCompetitionErrorUpdated(fn func(competition *Competition, err error)) {
	ce.onCompetitionErrorUpdated = fn
}

func (ce *competitionEngine) OnCompetitionPlayerUpdated(fn func(competitionID string, competitionPlayer *CompetitionPlayer)) {
	ce.onCompetitionPlayerUpdated = fn
}

func (ce *competitionEngine) OnCompetitionPlayerCashOut(fn func(competitionID string, competitionPlayer *CompetitionPlayer)) {
	ce.onCompetitionPlayerCashOut = fn
}

func (ce *competitionEngine) GetCompetition() *Competition {
	return ce.competition
}

func (ce *competitionEngine) CreateCompetition(competitionSetting CompetitionSetting) (*Competition, error) {
	// validate competitionSetting
	now := time.Now()
	if competitionSetting.StartAt != UnsetValue && competitionSetting.StartAt < now.Unix() {
		return nil, ErrCompetitionInvalidCreateSetting
	}

	if competitionSetting.DisableAt < now.Unix() {
		return nil, ErrCompetitionInvalidCreateSetting
	}

	for _, tableSetting := range competitionSetting.TableSettings {
		if len(tableSetting.JoinPlayers) > competitionSetting.Meta.TableMaxSeatCount {
			return nil, ErrCompetitionInvalidCreateSetting
		}
	}

	// setup blind
	ce.initBlind(competitionSetting.Meta)

	// create competition instance
	endAts := make([]int64, 0)
	for i := 0; i < len(competitionSetting.Meta.Blind.Levels); i++ {
		endAts = append(endAts, 0)
	}
	ce.competition = &Competition{
		ID:   competitionSetting.CompetitionID,
		Meta: competitionSetting.Meta,
		State: &CompetitionState{
			OpenAt:    time.Now().Unix(),
			DisableAt: competitionSetting.DisableAt,
			StartAt:   competitionSetting.StartAt,
			EndAt:     UnsetValue,
			Players:   make([]*CompetitionPlayer, 0),
			Status:    CompetitionStateStatus_Registering,
			Tables:    make([]*pwbtable.Table, 0),
			Rankings:  make([]*CompetitionRank, 0),
			BlindState: &BlindState{
				FinalBuyInLevelIndex: competitionSetting.Meta.Blind.FinalBuyInLevelIndex,
				CurrentLevelIndex:    UnsetValue,
				EndAts:               endAts,
			},
			Statistic: &Statistic{
				TotalBuyInCount: 0,
			},
		},
	}

	switch ce.competition.Meta.Mode {
	case CompetitionMode_Cash:
		for _, tableSetting := range competitionSetting.TableSettings {
			if _, err := ce.addCompetitionTable(tableSetting); err != nil {
				return nil, err
			}
		}
	}

	// auto startCompetition when StartAt is reached
	if ce.competition.State.StartAt > 0 {
		autoStartTime := time.Unix(ce.competition.State.StartAt, 0)
		if err := timebank.NewTimeBank().NewTaskWithDeadline(autoStartTime, func(isCancelled bool) {
			if isCancelled {
				return
			}

			if ce.competition.State.Status == CompetitionStateStatus_Registering {
				ce.StartCompetition()
			}
		}); err != nil {
			return nil, err
		}
	}

	// AutoEnd (When Disable Time is reached)
	disableAutoCloseTime := time.Unix(ce.competition.State.DisableAt, 0)
	if err := timebank.NewTimeBank().NewTaskWithDeadline(disableAutoCloseTime, func(isCancelled bool) {
		if isCancelled {
			return
		}

		if ce.competition.State.Status == CompetitionStateStatus_Registering {
			if len(ce.competition.State.Players) < ce.competition.Meta.MinPlayerCount {
				ce.CloseCompetition(CompetitionStateStatus_AutoEnd)
			}
		}
	}); err != nil {
		return nil, err
	}

	ce.emitEvent("CreateCompetition", "")
	return ce.competition, nil
}

func (ce *competitionEngine) CloseCompetition(endStatus CompetitionStateStatus) error {
	ce.settleCompetition(endStatus)
	return nil
}

func (ce *competitionEngine) StartCompetition() (int64, error) {
	if ce.competition.State.Status != CompetitionStateStatus_Registering {
		return ce.competition.State.StartAt, ErrCompetitionStartRejected
	}

	// start the competition
	if ce.competition.Meta.Blind.FinalBuyInLevelIndex == UnsetValue || ce.competition.Meta.Blind.FinalBuyInLevelIndex < NoStopBuyInIndex {
		ce.competition.State.Status = CompetitionStateStatus_StoppedBuyIn
	} else {
		ce.competition.State.Status = CompetitionStateStatus_DelayedBuyIn
	}

	// update start & end at
	ce.competition.State.StartAt = time.Now().Unix()

	if ce.competition.Meta.Mode == CompetitionMode_Cash {
		ce.competition.State.EndAt = ce.competition.State.StartAt + int64((time.Duration(ce.competition.Meta.MaxDuration) * time.Second).Seconds())
	} else {
		ce.competition.State.EndAt = UnsetValue
	}

	switch ce.competition.Meta.Mode {
	case CompetitionMode_Cash:
		normalCloseTime := time.Unix(ce.competition.State.EndAt, 0)
		if err := timebank.NewTimeBank().NewTaskWithDeadline(normalCloseTime, func(isCancelled bool) {
			if isCancelled {
				return
			}

			if ce.isEndStatus() {
				return
			}

			if len(ce.competition.State.Tables) > 0 {
				noneCloseTableStatuses := []pwbtable.TableStateStatus{
					// playing
					pwbtable.TableStateStatus_TableGameOpened,
					pwbtable.TableStateStatus_TableGamePlaying,
					pwbtable.TableStateStatus_TableGameSettled,

					// not playing
					pwbtable.TableStateStatus_TableClosed,
				}
				if !funk.Contains(noneCloseTableStatuses, ce.competition.State.Tables[0].State.Status) {
					if err := ce.tableManagerBackend.CloseTable(ce.competition.State.Tables[0].ID); err != nil {
						ce.emitErrorEvent("end time auto close -> CloseTable", "", err)
					}
				}
			}
		}); err != nil {
			return ce.competition.State.StartAt, err
		}
	}
	ce.emitEvent("StartCompetition", "")
	return ce.competition.State.StartAt, nil
}

func (ce *competitionEngine) PlayerBuyIn(joinPlayer JoinPlayer) error {
	// validate join player data
	if joinPlayer.RedeemChips <= 0 {
		return ErrCompetitionNoRedeemChips
	}

	playerIdx := ce.competition.FindPlayerIdx(func(player *CompetitionPlayer) bool {
		return player.PlayerID == joinPlayer.PlayerID
	})
	isBuyIn := playerIdx == UnsetValue
	validStatuses := []CompetitionStateStatus{
		CompetitionStateStatus_Registering,
		CompetitionStateStatus_DelayedBuyIn,
	}
	if !funk.Contains(validStatuses, ce.competition.State.Status) {
		if playerIdx == UnsetValue {
			return ErrCompetitionBuyInRejected
		} else {
			return ErrCompetitionReBuyRejected
		}
	}

	if ce.competition.Meta.Mode == CompetitionMode_Cash {
		if !isBuyIn {
			cp := ce.competition.State.Players[playerIdx]

			if cp.Chips > 0 {
				return ErrCompetitionReBuyRejected
			}
		} else {
			// check ct buy in conditions
			if ce.competition.Meta.Mode == CompetitionMode_Cash {
				if len(ce.competition.State.Tables) == 0 {
					return ErrCompetitionTableNotFound
				}

				if len(ce.competition.State.Tables[0].State.PlayerStates) >= ce.competition.State.Tables[0].Meta.TableMaxSeatCount {
					return ErrCompetitionBuyInRejected
				}
			}
		}
	}

	tableID := ""
	if ce.competition.Meta.Mode == CompetitionMode_Cash && len(ce.competition.State.Tables) > 0 {
		tableID = ce.competition.State.Tables[0].ID
	}

	playerStatus := CompetitionPlayerStatus_Playing

	// do logic
	ce.mu.Lock()
	if isBuyIn {
		player, playerCache := ce.newDefaultCompetitionPlayerData(tableID, joinPlayer.PlayerID, joinPlayer.RedeemChips, playerStatus)
		ce.competition.State.Players = append(ce.competition.State.Players, &player)
		playerCache.PlayerIdx = len(ce.competition.State.Players) - 1
		ce.insertPlayerCache(ce.competition.ID, joinPlayer.PlayerID, playerCache)
		ce.emitEvent(fmt.Sprintf("PlayerBuyIn -> %s Buy In", joinPlayer.PlayerID), joinPlayer.PlayerID)
		ce.emitPlayerEvent("PlayerBuyIn -> Buy In", &player)
	} else {
		// ReBuy logic
		playerCache, exist := ce.getPlayerCache(ce.competition.ID, joinPlayer.PlayerID)
		if !exist {
			return ErrCompetitionPlayerNotFound
		}

		cp := ce.competition.State.Players[playerIdx]
		cp.Status = playerStatus
		cp.Chips = joinPlayer.RedeemChips
		cp.ReBuyTimes++
		playerCache.ReBuyTimes = cp.ReBuyTimes
		cp.IsReBuying = false
		cp.ReBuyEndAt = UnsetValue
		cp.TotalRedeemChips += joinPlayer.RedeemChips
		if ce.competition.Meta.Mode == CompetitionMode_Cash && len(ce.competition.State.Tables) > 0 {
			playerCache.TableID = ce.competition.State.Tables[0].ID
			cp.CurrentTableID = ce.competition.State.Tables[0].ID
		}
		ce.emitEvent(fmt.Sprintf("PlayerBuyIn -> %s Re Buy", joinPlayer.PlayerID), joinPlayer.PlayerID)
		ce.emitPlayerEvent("PlayerBuyIn -> Re Buy", cp)
	}
	defer ce.mu.Unlock()

	switch ce.competition.Meta.Mode {
	case CompetitionMode_Cash:
		// call tableEngine
		jp := pwbtable.JoinPlayer{
			PlayerID:    joinPlayer.PlayerID,
			RedeemChips: joinPlayer.RedeemChips,
			Seat:        pwbtable.UnsetValue,
		}
		if err := ce.tableManagerBackend.PlayerReserve(tableID, jp); err != nil {
			ce.emitErrorEvent("PlayerBuyIn -> PlayerReserve", joinPlayer.PlayerID, err)
		}
	}

	return nil
}

func (ce *competitionEngine) PlayerCashOut(tableID, playerID string) error {
	// validate leave conditions
	playerIdx := ce.competition.FindPlayerIdx(func(player *CompetitionPlayer) bool {
		return player.PlayerID == playerID
	})
	if playerIdx == UnsetValue {
		return ErrCompetitionLeaveRejected
	}

	if ce.competition.Meta.Mode != CompetitionMode_Cash {
		return ErrCompetitionLeaveRejected
	}

	// update player status
	cp := ce.competition.State.Players[playerIdx]
	cp.Status = CompetitionPlayerStatus_CashLeaving
	ce.emitPlayerEvent("PlayerCashOut -> Cash Leaving", cp)

	competitionNotStart := ce.competition.State.Status == CompetitionStateStatus_Registering
	pauseCompetition := false
	if ce.competition.State.Status == CompetitionStateStatus_DelayedBuyIn && len(ce.competition.State.Tables) > 0 {
		if ce.competition.State.Tables[0].State.Status == pwbtable.TableStateStatus_TablePausing {
			pauseCompetition = true
		}
	}

	if competitionNotStart || pauseCompetition {
		leavePlayerIndexes := map[string]int{
			playerID: playerIdx,
		}
		leavePlayerIDs := []string{playerID}
		ce.handleCashOut(tableID, leavePlayerIndexes, leavePlayerIDs)
	}

	return nil
}
