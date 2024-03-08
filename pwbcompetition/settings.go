package pwbcompetition

import (
	"github.com/weedbox/PokerWeedBox/pwbtable"
)

type CompetitionSetting struct {
	CompetitionID string          `json:"competition_id"`
	Meta          CompetitionMeta `json:"meta"`
	StartAt       int64           `json:"start_game_at"`
	DisableAt     int64           `json:"disable_game_at"`
	TableSettings []TableSetting  `json:"table_settings"`
}

type TableSetting struct {
	TableID     string                `json:"table_id"`
	JoinPlayers []pwbtable.JoinPlayer `json:"join_players"`
}

type JoinPlayer struct {
	PlayerID    string `json:"player_id"`
	RedeemChips int64  `json:"redeem_chips"`
}

func NewpwbtableSetting(competitionID string, competitionMeta CompetitionMeta, tableSetting TableSetting) pwbtable.TableSetting {
	return pwbtable.TableSetting{
		TableID: tableSetting.TableID,
		Meta: pwbtable.TableMeta{
			CompetitionID:       competitionID,
			Rule:                string(competitionMeta.Rule),
			Mode:                string(competitionMeta.Mode),
			MaxDuration:         competitionMeta.MaxDuration,
			TableMaxSeatCount:   competitionMeta.TableMaxSeatCount,
			TableMinPlayerCount: competitionMeta.TableMinPlayerCount,
			MinChipUnit:         competitionMeta.MinChipUnit,
			ActionTime:          competitionMeta.ActionTime,
		},
		JoinPlayers: tableSetting.JoinPlayers,
	}
}
