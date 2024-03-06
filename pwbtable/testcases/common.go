package testcases

import (
	"fmt"
	"testing"

	"github.com/google/uuid"
	"github.com/thoas/go-funk"
	"github.com/weedbox/PokerWeedBox/pwbtable"
)

func LogJSON(t *testing.T, msg string, jsonPrinter func() (string, error)) {
	json, _ := jsonPrinter()
	fmt.Printf("\n===== [%s] =====\n%s\n", msg, json)
}

func NewDefaultTableSetting(joinPlayers ...pwbtable.JoinPlayer) pwbtable.TableSetting {
	return pwbtable.TableSetting{
		TableID: uuid.New().String(),
		Meta: pwbtable.TableMeta{
			CompetitionID:       uuid.NewString(),
			Rule:                pwbtable.CompetitionRule_Default,
			Mode:                pwbtable.CompetitionMode_Cash,
			MaxDuration:         3,
			TableMaxSeatCount:   9,
			TableMinPlayerCount: 2,
			MinChipUnit:         10,
			ActionTime:          10,
		},
		JoinPlayers: joinPlayers,
	}
}

func currentPlayerMove(table *pwbtable.Table) (string, []string) {
	playerID := ""
	currGamePlayerIdx := table.State.GameState.Status.CurrentPlayer
	for gamePlayerIdx, playerIdx := range table.State.GamePlayerIndexes {
		if gamePlayerIdx == currGamePlayerIdx {
			playerID = table.State.PlayerStates[playerIdx].PlayerID
			break
		}
	}
	return playerID, table.State.GameState.Players[currGamePlayerIdx].AllowedActions
}

func findPlayerID(table *pwbtable.Table, position string) string {
	for _, playerIdx := range table.State.GamePlayerIndexes {
		player := table.State.PlayerStates[playerIdx]
		if funk.Contains(player.Positions, position) {
			return player.PlayerID
		}
	}
	return ""
}
