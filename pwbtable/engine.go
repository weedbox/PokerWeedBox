package pwbtable

import (
	"errors"
	"fmt"
	"strings"
	"sync"
	"time"

	"github.com/thoas/go-funk"
	"github.com/weedbox/syncsaga"
	"github.com/weedbox/timebank"
)

var (
	ErrTableNoEmptySeats            = errors.New("table: no empty seats available")
	ErrTableInvalidCreateSetting    = errors.New("table: invalid create table setting")
	ErrTablePlayerNotFound          = errors.New("table: player not found")
	ErrTablePlayerInvalidGameAction = errors.New("table: player invalid game action")
	ErrTablePlayerInvalidAction     = errors.New("table: player invalid action")
	ErrTablePlayerSeatUnavailable   = errors.New("table: player seat unavailable")
	ErrTableOpenGameFailed          = errors.New("table: failed to open game")
)

type TableEngineOpt func(*tableEngine)

type TableEngine interface {
	OnTableUpdated(fn func(table *Table))
	OnTableErrorUpdated(fn func(table *Table, err error))
	OnTableStateUpdated(fn func(string, *Table))
	OnTablePlayerStateUpdated(fn func(string, string, *TablePlayerState))
	OnTablePlayerReserved(fn func(competitionID, tableID string, playerState *TablePlayerState))
	OnGamePlayerActionUpdated(fn func(TablePlayerGameAction))

	GetTable() *Table
	GetGame() Game
	CreateTable(tableSetting TableSetting) (*Table, error)
	PauseTable() error
	CloseTable() error
	StartTableGame() error
	TableGameOpen() error
	UpdateBlind(level int, ante, dealer, sb, bb int64)

	PlayerReserve(joinPlayer JoinPlayer) error
	PlayerJoin(playerID string) error
	PlayerRedeemChips(joinPlayer JoinPlayer) error
	PlayersLeave(playerIDs []string) error

	PlayerReady(playerID string) error
	PlayerPay(playerID string, chips int64) error
	PlayerBet(playerID string, chips int64) error
	PlayerRaise(playerID string, chipLevel int64) error
	PlayerCall(playerID string) error
	PlayerAllin(playerID string) error
	PlayerCheck(playerID string) error
	PlayerFold(playerID string) error
	PlayerPass(playerID string) error
}

type tableEngine struct {
	lock                      sync.Mutex
	options                   *TableEngineOptions
	table                     *Table
	game                      Game
	gameBackend               GameBackend
	rg                        *syncsaga.ReadyGroup
	tb                        *timebank.TimeBank
	onTableUpdated            func(*Table)
	onTableErrorUpdated       func(*Table, error)
	onTableStateUpdated       func(string, *Table)
	onTablePlayerStateUpdated func(string, string, *TablePlayerState)
	onTablePlayerReserved     func(competitionID, tableID string, playerState *TablePlayerState)
	onGamePlayerActionUpdated func(TablePlayerGameAction)
}

func NewTableEngine(options *TableEngineOptions, opts ...TableEngineOpt) TableEngine {
	callbacks := NewTableEngineCallbacks()
	te := &tableEngine{
		options:                   options,
		rg:                        syncsaga.NewReadyGroup(),
		tb:                        timebank.NewTimeBank(),
		onTableUpdated:            callbacks.OnTableUpdated,
		onTableErrorUpdated:       callbacks.OnTableErrorUpdated,
		onTableStateUpdated:       callbacks.OnTableStateUpdated,
		onTablePlayerStateUpdated: callbacks.OnTablePlayerStateUpdated,
		onTablePlayerReserved:     callbacks.OnTablePlayerReserved,
		onGamePlayerActionUpdated: callbacks.OnGamePlayerActionUpdated,
	}

	for _, opt := range opts {
		opt(te)
	}

	return te
}

func WithGameBackend(gb GameBackend) TableEngineOpt {
	return func(te *tableEngine) {
		te.gameBackend = gb
	}
}

func (te *tableEngine) OnTableUpdated(fn func(*Table)) {
	te.onTableUpdated = fn
}

func (te *tableEngine) OnTableErrorUpdated(fn func(*Table, error)) {
	te.onTableErrorUpdated = fn
}

func (te *tableEngine) OnTableStateUpdated(fn func(string, *Table)) {
	te.onTableStateUpdated = fn
}

func (te *tableEngine) OnTablePlayerStateUpdated(fn func(string, string, *TablePlayerState)) {
	te.onTablePlayerStateUpdated = fn
}

func (te *tableEngine) OnTablePlayerReserved(fn func(competitionID, tableID string, playerState *TablePlayerState)) {
	te.onTablePlayerReserved = fn
}

func (te *tableEngine) OnGamePlayerActionUpdated(fn func(TablePlayerGameAction)) {
	te.onGamePlayerActionUpdated = fn
}

func (te *tableEngine) GetTable() *Table {
	return te.table
}

func (te *tableEngine) GetGame() Game {
	return te.game
}

func (te *tableEngine) CreateTable(tableSetting TableSetting) (*Table, error) {
	// validate tableSetting
	if len(tableSetting.JoinPlayers) > tableSetting.Meta.TableMaxSeatCount {
		return nil, ErrTableInvalidCreateSetting
	}

	// create table instance
	table := &Table{
		ID: tableSetting.TableID,
	}

	// configure meta
	table.Meta = tableSetting.Meta

	// configure state
	state := TableState{
		GameCount: 0,
		StartAt:   UnsetValue,
		BlindState: &TableBlindState{
			Level:  0,
			Ante:   UnsetValue,
			Dealer: UnsetValue,
			SB:     UnsetValue,
			BB:     UnsetValue,
		},
		CurrentDealerSeat: UnsetValue,
		CurrentBBSeat:     UnsetValue,
		SeatMap:           NewDefaultSeatMap(tableSetting.Meta.TableMaxSeatCount),
		PlayerStates:      make([]*TablePlayerState, 0),
		GamePlayerIndexes: make([]int, 0),
		Status:            TableStateStatus_TableCreated,
	}
	table.State = &state
	te.table = table

	te.emitEvent("CreateTable", "")

	// handle auto join players
	if len(tableSetting.JoinPlayers) > 0 {
		if err := te.batchAddPlayers(tableSetting.JoinPlayers); err != nil {
			return nil, err
		}

		te.emitEvent("CreateTable -> Auto Add Players", "")
	}

	return te.table, nil
}

func (te *tableEngine) PauseTable() error {
	te.table.State.Status = TableStateStatus_TablePausing
	return nil
}

func (te *tableEngine) CloseTable() error {
	te.table.State.Status = TableStateStatus_TableClosed

	te.emitEvent("CloseTable", "")
	return nil
}

func (te *tableEngine) StartTableGame() error {
	te.table.State.StartAt = time.Now().Unix()
	te.emitEvent("StartTableGame", "")

	return te.TableGameOpen()
}

func (te *tableEngine) TableGameOpen() error {
	newTable, err := te.openGame(te.table)

	retry := 7
	if err != nil {
		if err == ErrTableOpenGameFailed {
			reopened := false

			for i := 0; i < retry; i++ {
				time.Sleep(time.Second * 3)
				newTable, err = te.openGame(te.table)
				if err != nil {
					if err == ErrTableOpenGameFailed {
						fmt.Printf("table (%s): failed to open game. retry %d time(s)...\n", te.table.ID, i+1)
						continue
					} else {
						return err
					}
				} else {
					reopened = true
					break
				}
			}

			if !reopened {
				return err
			}
		} else {
			return err
		}
	}
	te.table = newTable
	te.emitEvent("TableGameOpen", "")

	return te.startGame()
}

func (te *tableEngine) UpdateBlind(level int, ante, dealer, sb, bb int64) {
	te.table.State.BlindState.Level = level
	te.table.State.BlindState.Ante = ante
	te.table.State.BlindState.Dealer = dealer
	te.table.State.BlindState.SB = sb
	te.table.State.BlindState.BB = bb
}

func (te *tableEngine) PlayerReserve(joinPlayer JoinPlayer) error {
	te.lock.Lock()
	defer te.lock.Unlock()

	// find player index in PlayerStates
	targetPlayerIdx := te.table.FindPlayerIdx(joinPlayer.PlayerID)

	if targetPlayerIdx == UnsetValue {
		if len(te.table.State.PlayerStates) == te.table.Meta.TableMaxSeatCount {
			return ErrTableNoEmptySeats
		}

		// BuyIn
		if err := te.batchAddPlayers([]JoinPlayer{joinPlayer}); err != nil {
			return err
		}
	} else {
		// ReBuy
		// 補碼要檢查玩家是否介於 Dealer-BB 之間
		playerState := te.table.State.PlayerStates[targetPlayerIdx]
		playerState.IsBetweenDealerBB = IsBetweenDealerBB(playerState.Seat, te.table.State.CurrentDealerSeat, te.table.State.CurrentBBSeat, te.table.Meta.TableMaxSeatCount, te.table.Meta.Rule)
		playerState.Bankroll += joinPlayer.RedeemChips
	}

	te.emitEvent("PlayerReserve", joinPlayer.PlayerID)

	return nil
}

func (te *tableEngine) PlayerJoin(playerID string) error {
	playerIdx := te.table.FindPlayerIdx(playerID)
	if playerIdx == UnsetValue {
		return ErrTablePlayerNotFound
	}

	if te.table.State.PlayerStates[playerIdx].Seat == UnsetValue {
		return ErrTablePlayerInvalidAction
	}

	if te.table.State.PlayerStates[playerIdx].IsIn {
		return nil
	}

	te.table.State.PlayerStates[playerIdx].IsIn = true

	if te.table.State.Status == TableStateStatus_TableBalancing {
		te.rg.Ready(int64(playerIdx))
	}

	te.emitEvent("PlayerJoin", playerID)
	return nil
}

func (te *tableEngine) PlayerRedeemChips(joinPlayer JoinPlayer) error {
	// find player index in PlayerStates
	playerIdx := te.table.FindPlayerIdx(joinPlayer.PlayerID)
	if playerIdx == UnsetValue {
		return ErrTablePlayerNotFound
	}

	playerState := te.table.State.PlayerStates[playerIdx]
	if playerState.Bankroll == 0 {
		playerState.IsBetweenDealerBB = IsBetweenDealerBB(playerState.Seat, te.table.State.CurrentDealerSeat, te.table.State.CurrentBBSeat, te.table.Meta.TableMaxSeatCount, te.table.Meta.Rule)
	}
	playerState.Bankroll += joinPlayer.RedeemChips

	te.emitEvent("PlayerRedeemChips", joinPlayer.PlayerID)
	return nil
}

func (te *tableEngine) PlayersLeave(playerIDs []string) error {
	te.lock.Lock()
	defer te.lock.Unlock()

	te.batchRemovePlayers(playerIDs)
	te.emitEvent("PlayersLeave", strings.Join(playerIDs, ","))

	return nil
}

func (te *tableEngine) PlayerReady(playerID string) error {
	te.lock.Lock()
	defer te.lock.Unlock()

	gamePlayerIdx := te.table.FindGamePlayerIdx(playerID)
	if err := te.validateGameMove(gamePlayerIdx); err != nil {
		return err
	}

	_, err := te.game.Ready(gamePlayerIdx)
	return err
}

func (te *tableEngine) PlayerPay(playerID string, chips int64) error {
	te.lock.Lock()
	defer te.lock.Unlock()

	gamePlayerIdx := te.table.FindGamePlayerIdx(playerID)
	if err := te.validateGameMove(gamePlayerIdx); err != nil {
		return err
	}

	_, err := te.game.Pay(gamePlayerIdx, chips)
	return err
}

func (te *tableEngine) PlayerBet(playerID string, chips int64) error {
	te.lock.Lock()
	defer te.lock.Unlock()

	gamePlayerIdx := te.table.FindGamePlayerIdx(playerID)
	if err := te.validateGameMove(gamePlayerIdx); err != nil {
		return err
	}

	_, err := te.game.Bet(gamePlayerIdx, chips)
	if err == nil {
		playerIdx := te.table.State.GamePlayerIndexes[gamePlayerIdx]

		playerState := te.table.State.PlayerStates[playerIdx]
		playerState.GameStatistics.ActionTimes++
		if te.game.GetGameState().Status.CurrentRaiser == gamePlayerIdx {
			playerState.GameStatistics.RaiseTimes++
		}
	}
	return err
}

func (te *tableEngine) PlayerRaise(playerID string, chipLevel int64) error {
	te.lock.Lock()
	defer te.lock.Unlock()

	gamePlayerIdx := te.table.FindGamePlayerIdx(playerID)
	if err := te.validateGameMove(gamePlayerIdx); err != nil {
		return err
	}

	_, err := te.game.Raise(gamePlayerIdx, chipLevel)
	if err == nil {
		playerIdx := te.table.State.GamePlayerIndexes[gamePlayerIdx]

		playerState := te.table.State.PlayerStates[playerIdx]
		playerState.GameStatistics.ActionTimes++
		playerState.GameStatistics.RaiseTimes++
	}
	return err
}

func (te *tableEngine) PlayerCall(playerID string) error {
	te.lock.Lock()
	defer te.lock.Unlock()

	gamePlayerIdx := te.table.FindGamePlayerIdx(playerID)
	if err := te.validateGameMove(gamePlayerIdx); err != nil {
		return err
	}

	_, err := te.game.Call(gamePlayerIdx)
	if err == nil {
		playerIdx := te.table.State.GamePlayerIndexes[gamePlayerIdx]

		playerState := te.table.State.PlayerStates[playerIdx]
		playerState.GameStatistics.ActionTimes++
		playerState.GameStatistics.CallTimes++
	}
	return err
}

func (te *tableEngine) PlayerAllin(playerID string) error {
	te.lock.Lock()
	defer te.lock.Unlock()

	gamePlayerIdx := te.table.FindGamePlayerIdx(playerID)
	if err := te.validateGameMove(gamePlayerIdx); err != nil {
		return err
	}

	_, err := te.game.Allin(gamePlayerIdx)
	if err == nil {
		playerIdx := te.table.State.GamePlayerIndexes[gamePlayerIdx]

		playerState := te.table.State.PlayerStates[playerIdx]
		playerState.GameStatistics.ActionTimes++
		if te.game.GetGameState().Status.CurrentRaiser == gamePlayerIdx {
			playerState.GameStatistics.RaiseTimes++
		}
	}
	return err
}

func (te *tableEngine) PlayerCheck(playerID string) error {
	te.lock.Lock()
	defer te.lock.Unlock()

	gamePlayerIdx := te.table.FindGamePlayerIdx(playerID)
	if err := te.validateGameMove(gamePlayerIdx); err != nil {
		return err
	}

	_, err := te.game.Check(gamePlayerIdx)
	if err == nil {
		playerIdx := te.table.State.GamePlayerIndexes[gamePlayerIdx]

		playerState := te.table.State.PlayerStates[playerIdx]
		playerState.GameStatistics.ActionTimes++
		playerState.GameStatistics.CheckTimes++
	}
	return err
}

func (te *tableEngine) PlayerFold(playerID string) error {
	te.lock.Lock()
	defer te.lock.Unlock()

	gamePlayerIdx := te.table.FindGamePlayerIdx(playerID)
	if err := te.validateGameMove(gamePlayerIdx); err != nil {
		return err
	}

	_, err := te.game.Fold(gamePlayerIdx)
	if err == nil {
		playerIdx := te.table.State.GamePlayerIndexes[gamePlayerIdx]

		playerState := te.table.State.PlayerStates[playerIdx]
		playerState.GameStatistics.ActionTimes++
		playerState.GameStatistics.IsFold = true
		playerState.GameStatistics.FoldRound = te.game.GetGameState().Status.Round
	}
	return err
}

func (te *tableEngine) PlayerPass(playerID string) error {
	te.lock.Lock()
	defer te.lock.Unlock()

	gamePlayerIdx := te.table.FindGamePlayerIdx(playerID)
	if err := te.validateGameMove(gamePlayerIdx); err != nil {
		return err
	}

	_, err := te.game.Pass(gamePlayerIdx)
	return err
}

func (te *tableEngine) calcLeavePlayers(status TableStateStatus, leavePlayerIDs []string, currentPlayers []*TablePlayerState, tableMaxSeatCount int) ([]*TablePlayerState, []int, []int) {
	// calc delete target players in PlayerStates
	newPlayerStates := make([]*TablePlayerState, 0)
	for _, player := range currentPlayers {
		exist := funk.Contains(leavePlayerIDs, func(leavePlayerID string) bool {
			return player.PlayerID == leavePlayerID
		})
		if !exist {
			newPlayerStates = append(newPlayerStates, player)
		}
	}

	// calc seatMap
	newSeatMap := NewDefaultSeatMap(tableMaxSeatCount)
	for newPlayerIdx, player := range newPlayerStates {
		newSeatMap[player.Seat] = newPlayerIdx
	}

	// calc new gamePlayerIndexes
	newPlayerData := make(map[string]int)
	for newPlayerIdx, player := range newPlayerStates {
		newPlayerData[player.PlayerID] = newPlayerIdx
	}

	currentGamePlayerData := make(map[int]string) // key: currentPlayerIdx, value: currentPlayerID
	for _, playerIdx := range te.table.State.GamePlayerIndexes {
		currentGamePlayerData[playerIdx] = te.table.State.PlayerStates[playerIdx].PlayerID
	}
	gameStatuses := []TableStateStatus{
		TableStateStatus_TableGameOpened,
		TableStateStatus_TableGamePlaying,
		TableStateStatus_TableGameSettled,
	}
	newGamePlayerIndexes := make([]int, 0)
	if funk.Contains(gameStatuses, status) {
		for _, currentPlayerIdx := range te.table.State.GamePlayerIndexes {
			playerID := currentGamePlayerData[currentPlayerIdx]
			// sync newPlayerData player idx to newGamePlayerIndexes
			if newPlayerIdx, exist := newPlayerData[playerID]; exist {
				newGamePlayerIndexes = append(newGamePlayerIndexes, newPlayerIdx)
			}
		}
	} else {
		newGamePlayerIndexes = te.table.State.GamePlayerIndexes
	}

	return newPlayerStates, newSeatMap, newGamePlayerIndexes
}
