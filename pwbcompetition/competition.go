package pwbcompetition

import (
	"encoding/json"

	"github.com/thoas/go-funk"
	"github.com/weedbox/PokerWeedBox/pwbtable"
)

type CompetitionStateStatus string
type CompetitionPlayerStatus string
type CompetitionMode string
type CompetitionRule string
type CompetitionAdvanceRule string
type CompetitionAdvanceStatus string

const (
	// CompetitionStateStatus
	CompetitionStateStatus_Registering  CompetitionStateStatus = "registering"
	CompetitionStateStatus_DelayedBuyIn CompetitionStateStatus = "delayed_buy_in"
	CompetitionStateStatus_StoppedBuyIn CompetitionStateStatus = "stopped_buy_in"
	CompetitionStateStatus_End          CompetitionStateStatus = "end"
	CompetitionStateStatus_AutoEnd      CompetitionStateStatus = "auto_end"
	CompetitionStateStatus_ForceEnd     CompetitionStateStatus = "force_end"
	CompetitionStateStatus_Restoring    CompetitionStateStatus = "restoring"

	// CompetitionPlayerStatus
	CompetitionPlayerStatus_Playing      CompetitionPlayerStatus = "playing"
	CompetitionPlayerStatus_ReBuyWaiting CompetitionPlayerStatus = "re_buy_waiting"
	CompetitionPlayerStatus_Knockout     CompetitionPlayerStatus = "knockout"
	CompetitionPlayerStatus_CashLeaving  CompetitionPlayerStatus = "cash_leaving"

	// CompetitionMode
	CompetitionMode_Cash CompetitionMode = "cash"

	// CompetitionRule
	CompetitionRule_Default   CompetitionRule = "default"
	CompetitionRule_ShortDeck CompetitionRule = "short_deck"
	CompetitionRule_Omaha     CompetitionRule = "omaha"
)

type Competition struct {
	UpdateSerial int64             `json:"update_serial"`
	ID           string            `json:"id"`
	Meta         CompetitionMeta   `json:"meta"`
	State        *CompetitionState `json:"state"`
	UpdateAt     int64             `json:"update_at"`
}

type CompetitionMeta struct {
	Blind               Blind           `json:"blind"`
	MaxDuration         int             `json:"max_duration"`
	MinPlayerCount      int             `json:"min_player_count"`
	MaxPlayerCount      int             `json:"max_player_count"`
	TableMaxSeatCount   int             `json:"table_max_seat_count"`
	TableMinPlayerCount int             `json:"table_min_player_count"`
	Rule                CompetitionRule `json:"rule"`
	Mode                CompetitionMode `json:"mode"`
	ReBuySetting        ReBuySetting    `json:"re_buy_setting"`
	ActionTime          int             `json:"action_time"`
	MinChipUnit         int64           `json:"min_chip_unit"`
}

type CompetitionState struct {
	OpenAt     int64                  `json:"open_at"`
	DisableAt  int64                  `json:"disable_at"`
	StartAt    int64                  `json:"start_at"`
	EndAt      int64                  `json:"end_at"`
	BlindState *BlindState            `json:"blind_state"`
	Players    []*CompetitionPlayer   `json:"players"`
	Status     CompetitionStateStatus `json:"status"`
	Tables     []*pwbtable.Table      `json:"tables"`
	Rankings   []*CompetitionRank     `json:"rankings"`
	Statistic  *Statistic             `json:"statistic"`
}

type CompetitionRank struct {
	PlayerID   string `json:"player_id"`
	FinalChips int64  `json:"final_chips"`
}

type CompetitionPlayer struct {
	PlayerID       string `json:"player_id"`
	CurrentTableID string `json:"table_id"`
	CurrentSeat    int    `json:"seat"`
	JoinAt         int64  `json:"join_at"`

	// current info
	Status     CompetitionPlayerStatus `json:"status"`
	Rank       int                     `json:"rank"`
	Chips      int64                   `json:"chips"`
	IsReBuying bool                    `json:"is_re_buying"`
	ReBuyEndAt int64                   `json:"re_buy_end_at"`
	ReBuyTimes int                     `json:"re_buy_times"`
	AddonTimes int                     `json:"addon_times"`

	// statistics info
	// best
	BestWinningPotChips int64    `json:"best_winning_pot_chips"`
	BestWinningCombo    []string `json:"best_winning_combo"`
	BestWinningType     string   `json:"best_winning_type"`
	BestWinningPower    int      `json:"best_winning_power"`

	// accumulated info
	// competition/table
	TotalRedeemChips int64 `json:"total_redeem_chips"`
	TotalGameCounts  int64 `json:"total_game_counts"`

	// game: round & actions
	TotalWalkTimes        int64 `json:"total_walk_times"`
	TotalVPIPTimes        int   `json:"total_vpip_times"`
	TotalFoldTimes        int   `json:"total_fold_times"`
	TotalPreflopFoldTimes int   `json:"total_preflop_fold_times"`
	TotalFlopFoldTimes    int   `json:"total_flop_fold_times"`
	TotalTurnFoldTimes    int   `json:"total_turn_fold_times"`
	TotalRiverFoldTimes   int   `json:"total_river_fold_times"`
	TotalActionTimes      int   `json:"total_action_times"`
	TotalRaiseTimes       int   `json:"total_raise_times"`
	TotalCallTimes        int   `json:"total_call_times"`
	TotalCheckTimes       int   `json:"total_check_times"`
	TotalProfitTimes      int   `json:"total_profit_times"`
}

type Blind struct {
	ID                   string       `json:"id"`
	InitialLevel         int          `json:"initial_level"`
	FinalBuyInLevelIndex int          `json:"final_buy_in_level_idx"`
	DealerBlindTime      int          `json:"dealer_blind_time"`
	Levels               []BlindLevel `json:"levels"`
}

type BlindLevel struct {
	Level      int   `json:"level"`
	SB         int64 `json:"sb"`
	BB         int64 `json:"bb"`
	Ante       int64 `json:"ante"`
	Duration   int   `json:"duration"`
	AllowAddon bool  `json:"allow_addon"`
}

type BlindState struct {
	FinalBuyInLevelIndex int     `json:"final_buy_in_level_idx"`
	CurrentLevelIndex    int     `json:"current_level_index"`
	EndAts               []int64 `json:"end_ats"`
}

type Statistic struct {
	TotalBuyInCount int `json:"total_buy_in_count"`
}

type ReBuySetting struct {
	MaxTime     int `json:"max_time"`
	WaitingTime int `json:"waiting_time"`
}

// Competition Setters
func (c *Competition) AsPlayer() {
	c.State.Tables = nil
}

// Competition Getters
func (c Competition) GetJSON() (string, error) {
	encoded, err := json.Marshal(c)
	if err != nil {
		return "", err
	}
	return string(encoded), nil
}

func (c Competition) PlayingPlayerCount() int {
	return len(funk.Filter(c.State.Players, func(player *CompetitionPlayer) bool {
		return player.Chips > 0
	}).([]*CompetitionPlayer))
}

func (c Competition) IsTableExist(tableID string) bool {
	for _, table := range c.State.Tables {
		if table.ID == tableID {
			return true
		}
	}
	return false
}

func (c Competition) FindTableIdx(predicate func(*pwbtable.Table) bool) int {
	for idx, table := range c.State.Tables {
		if predicate(table) {
			return idx
		}
	}
	return UnsetValue
}

func (c Competition) FindPlayerIdx(predicate func(*CompetitionPlayer) bool) int {
	for idx, player := range c.State.Players {
		if predicate(player) {
			return idx
		}
	}
	return UnsetValue
}

func (c Competition) CurrentBlindLevel() BlindLevel {
	if c.State.BlindState.CurrentLevelIndex < 0 {
		return BlindLevel{}
	}
	return c.Meta.Blind.Levels[c.State.BlindState.CurrentLevelIndex]
}

func (c Competition) CurrentBlindData() (int, int64, int64, int64, int64) {
	bl := c.CurrentBlindLevel()
	dealer := int64(0)
	if c.Meta.Blind.DealerBlindTime > 0 {
		dealer = bl.Ante * (int64(c.Meta.Blind.DealerBlindTime) - 1)
	}
	return bl.Level, bl.Ante, dealer, bl.SB, bl.BB
}

func (c Competition) IsBreaking() bool {
	return c.CurrentBlindLevel().Level == -1
}

// BlindState Getters
func (bs BlindState) IsStopBuyIn() bool {
	if bs.FinalBuyInLevelIndex == NoStopBuyInIndex {
		return false
	}

	if bs.FinalBuyInLevelIndex == UnsetValue {
		return true
	}

	return bs.CurrentLevelIndex > bs.FinalBuyInLevelIndex
}

// CompetitionPlayer Getters
func (cp CompetitionPlayer) IsOverReBuyWaitingTime() bool {
	if !cp.IsReBuying && cp.ReBuyEndAt == UnsetValue {
		return true
	}
	return false
}
