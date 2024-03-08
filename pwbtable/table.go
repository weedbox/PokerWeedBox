package pwbtable

import (
	"encoding/json"
	"fmt"

	"github.com/thoas/go-funk"
	"github.com/weedbox/pokerface"
)

type TableStateStatus string

const (
	TableStateStatus_TableCreated   TableStateStatus = "table_created"
	TableStateStatus_TablePausing   TableStateStatus = "table_pausing"
	TableStateStatus_TableRestoring TableStateStatus = "table_restoring"
	TableStateStatus_TableBalancing TableStateStatus = "table_balancing"
	TableStateStatus_TableClosed    TableStateStatus = "table_closed"

	TableStateStatus_TableGameOpened  TableStateStatus = "table_game_opened"
	TableStateStatus_TableGamePlaying TableStateStatus = "table_game_playing"
	TableStateStatus_TableGameSettled TableStateStatus = "table_game_settled"
	TableStateStatus_TableGameStandby TableStateStatus = "table_game_standby"
)

type Table struct {
	UpdateSerial int64       `json:"update_serial"`
	ID           string      `json:"id"`
	Meta         TableMeta   `json:"meta"`
	State        *TableState `json:"state"`
	UpdateAt     int64       `json:"update_at"`
}

type TableMeta struct {
	CompetitionID       string `json:"competition_id"`
	Rule                string `json:"rule"`
	Mode                string `json:"mode"`
	MaxDuration         int    `json:"max_duration"`
	TableMaxSeatCount   int    `json:"table_max_seat_count"`
	TableMinPlayerCount int    `json:"table_min_player_count"`
	MinChipUnit         int64  `json:"min_chip_unit"`
	ActionTime          int    `json:"action_time"`
}

type TableState struct {
	Status            TableStateStatus     `json:"status"`
	StartAt           int64                `json:"start_at"`
	SeatMap           []int                `json:"seat_map"`
	BlindState        *TableBlindState     `json:"blind_state"`
	CurrentDealerSeat int                  `json:"current_dealer_seat"`
	CurrentBBSeat     int                  `json:"current_bb_seat"`
	PlayerStates      []*TablePlayerState  `json:"player_states"`
	GameCount         int                  `json:"game_count"`
	GamePlayerIndexes []int                `json:"game_player_indexes"`
	GameState         *pokerface.GameState `json:"game_state"`
}

type TablePlayerGameAction struct {
	CompetitionID    string   `json:"competition_id"`
	TableID          string   `json:"table_id"`
	GameID           string   `json:"game_id"`
	GameCount        int      `json:"game_count"`
	Round            string   `json:"round"`
	UpdateAt         int64    `json:"update_at"`
	PlayerID         string   `json:"player_id"`
	Seat             int      `json:"seat"`
	Positions        []string `json:"positions"`
	Action           string   `json:"action"`
	Chips            int64    `json:"chips"`
	Bankroll         int64    `json:"bankroll"`
	InitialStackSize int64    `json:"initial_stack_size"`
	StackSize        int64    `json:"stack_size"`
	Pot              int64    `json:"pot"`
	Wager            int64    `json:"wager"`
}

type TablePlayerState struct {
	PlayerID          string                    `json:"player_id"`
	Seat              int                       `json:"seat"`
	Positions         []string                  `json:"positions"`
	IsParticipated    bool                      `json:"is_participated"`
	IsBetweenDealerBB bool                      `json:"is_between_dealer_bb"`
	Bankroll          int64                     `json:"bankroll"`
	IsIn              bool                      `json:"is_in"`
	GameStatistics    TablePlayerGameStatistics `json:"game_statistics"`
}

type TablePlayerGameStatistics struct {
	ActionTimes int    `json:"action_times"`
	RaiseTimes  int    `json:"raise_times"`
	CallTimes   int    `json:"call_times"`
	CheckTimes  int    `json:"check_times"`
	IsFold      bool   `json:"is_fold"`
	FoldRound   string `json:"fold_round"`
}

type TableBlindState struct {
	Level  int   `json:"level"`
	Ante   int64 `json:"ante"`
	Dealer int64 `json:"dealer"`
	SB     int64 `json:"sb"`
	BB     int64 `json:"bb"`
}

func (t Table) Clone() (*Table, error) {
	encoded, err := json.Marshal(t)
	if err != nil {
		return nil, err
	}

	var cloneTable Table
	if err := json.Unmarshal(encoded, &cloneTable); err != nil {
		return nil, err
	}

	return &cloneTable, nil
}

func (t Table) GetJSON() (string, error) {
	encoded, err := json.Marshal(t)
	if err != nil {
		return "", err
	}
	return string(encoded), nil
}

func (t Table) GetGameStateJSON() (string, error) {
	encoded, err := json.Marshal(t.State.GameState)
	if err != nil {
		return "", err
	}
	return string(encoded), nil
}

func (t Table) ParticipatedPlayers() []*TablePlayerState {
	return funk.Filter(t.State.PlayerStates, func(player *TablePlayerState) bool {
		return player.IsParticipated
	}).([]*TablePlayerState)
}

func (t Table) AlivePlayers() []*TablePlayerState {
	return funk.Filter(t.State.PlayerStates, func(player *TablePlayerState) bool {
		return player.Bankroll > 0
	}).([]*TablePlayerState)
}

func (t Table) GamePlayerIndex(playerID string) int {
	targetPlayerIdx := UnsetValue
	for idx, player := range t.State.PlayerStates {
		if player.PlayerID == playerID {
			targetPlayerIdx = idx
			break
		}
	}

	if targetPlayerIdx == UnsetValue {
		return UnsetValue
	}

	for gamePlayerIndex, playerIndex := range t.State.GamePlayerIndexes {
		if targetPlayerIdx == playerIndex {
			return gamePlayerIndex
		}
	}
	return UnsetValue
}

func (t Table) FindGamePlayerIdx(playerID string) int {
	for gamePlayerIdx, playerIdx := range t.State.GamePlayerIndexes {
		if playerIdx >= len(t.State.PlayerStates) {
			fmt.Printf("[DEBUG#FindGamePlayerIdx] TableID: %s, PlayerID: %s, GamePlayerIndexes: %+v, len(PlayerStates): %d, TableSerial: %d\n",
				t.ID,
				playerID,
				t.State.GamePlayerIndexes,
				len(t.State.PlayerStates),
				t.UpdateSerial,
			)
			continue
		}
		player := t.State.PlayerStates[playerIdx]
		if player.PlayerID == playerID {
			return gamePlayerIdx
		}
	}
	return UnsetValue
}

func (t Table) FindPlayerIdx(playerID string) int {
	for idx, player := range t.State.PlayerStates {
		if player.PlayerID == playerID {
			return idx
		}
	}
	return UnsetValue
}

func (t Table) PlayerSeatMap() map[string]int {
	playerSeatMap := make(map[string]int)
	for _, player := range t.State.PlayerStates {
		playerSeatMap[player.PlayerID] = player.Seat
	}
	return playerSeatMap
}

func (t Table) ShouldPause() bool {
	return t.State.BlindState.IsBreaking() || len(t.AlivePlayers()) < t.Meta.TableMinPlayerCount
}

func (bs TableBlindState) IsBreaking() bool {
	return bs.Level == -1
}

func (bs TableBlindState) IsSet() bool {
	return bs.Level != 0 && bs.Ante == UnsetValue && bs.Dealer == UnsetValue && bs.SB == UnsetValue && bs.BB == UnsetValue
}
