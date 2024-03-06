package testcases

import (
	"fmt"
	"sync"
	"testing"

	"github.com/stretchr/testify/assert"
	"github.com/thoas/go-funk"
	"github.com/weedbox/PokerWeedBox/pwbtable"
	"github.com/weedbox/pokerface"
)

func TestTableGame_Preflop_Walk(t *testing.T) {
	var wg sync.WaitGroup
	wg.Add(1)

	// given conditions
	playerIDs := []string{"Fred", "Jeffrey", "Chuck"}
	redeemChips := int64(15000)
	players := funk.Map(playerIDs, func(playerID string) pwbtable.JoinPlayer {
		return pwbtable.JoinPlayer{
			PlayerID:    playerID,
			RedeemChips: redeemChips,
			Seat:        -1,
		}
	}).([]pwbtable.JoinPlayer)

	// create manager & table
	var tableEngine pwbtable.TableEngine
	manager := pwbtable.NewManager()
	tableEngineOption := pwbtable.NewTableEngineOptions()
	tableEngineOption.Interval = 3
	tableEngineCallbacks := pwbtable.NewTableEngineCallbacks()
	tableEngineCallbacks.OnTableUpdated = func(table *pwbtable.Table) {
		switch table.State.Status {
		case pwbtable.TableStateStatus_TableGameOpened:
			DebugPrintTableGameOpened(*table)
		case pwbtable.TableStateStatus_TableGamePlaying:
			t.Logf("[%s] %s:", table.State.GameState.Status.Round, table.State.GameState.Status.CurrentEvent)
			event, ok := pokerface.GameEventBySymbol[table.State.GameState.Status.CurrentEvent]
			if !ok {
				return
			}

			switch event {
			case pokerface.GameEvent_ReadyRequested:
				for _, playerID := range playerIDs {
					assert.Nil(t, tableEngine.PlayerReady(playerID), fmt.Sprintf("%s ready error", playerID))
					t.Logf(fmt.Sprintf("%s ready", playerID))
				}
			case pokerface.GameEvent_AnteRequested:
				for _, playerID := range playerIDs {
					ante := table.State.BlindState.Ante
					assert.Nil(t, tableEngine.PlayerPay(playerID, ante), fmt.Sprintf("%s pay ante error", playerID))
					t.Logf(fmt.Sprintf("%s pay ante %d", playerID, ante))
				}
			case pokerface.GameEvent_BlindsRequested:
				blind := table.State.BlindState

				// pay sb
				sbPlayerID := findPlayerID(table, "sb")
				assert.Nil(t, tableEngine.PlayerPay(sbPlayerID, blind.SB), fmt.Sprintf("%s pay sb error", sbPlayerID))
				t.Logf(fmt.Sprintf("%s pay sb %d", sbPlayerID, blind.SB))

				// pay bb
				bbPlayerID := findPlayerID(table, "bb")
				assert.Nil(t, tableEngine.PlayerPay(bbPlayerID, blind.BB), fmt.Sprintf("%s pay bb error", bbPlayerID))
				t.Logf(fmt.Sprintf("%s pay bb %d", bbPlayerID, blind.BB))
			case pokerface.GameEvent_RoundStarted:
				playerID, actions := currentPlayerMove(table)
				if funk.Contains(actions, "fold") {
					t.Logf(fmt.Sprintf("%s's move: fold", playerID))
					assert.Nil(t, tableEngine.PlayerFold(playerID), fmt.Sprintf("%s fold error", playerID))
				}
			}
		case pwbtable.TableStateStatus_TableGameSettled:
			// check results
			assert.NotNil(t, table.State.GameState.Result, "invalid game result")
			assert.Equal(t, 1, table.State.GameCount)
			for _, playerResult := range table.State.GameState.Result.Players {
				playerIdx := table.State.GamePlayerIndexes[playerResult.Idx]
				player := table.State.PlayerStates[playerIdx]
				assert.Equal(t, playerResult.Final, player.Bankroll)
			}

			DebugPrintTableGameSettled(*table)

			if table.State.GameState.Status.CurrentEvent == pokerface.GameEventSymbols[pokerface.GameEvent_GameClosed] {
				wg.Done()
				return
			}
		}
	}
	tableEngineCallbacks.OnTableErrorUpdated = func(table *pwbtable.Table, err error) {
		t.Log("[Table] Error:", err)
	}
	table, err := manager.CreateTable(tableEngineOption, tableEngineCallbacks, NewDefaultTableSetting())
	assert.Nil(t, err, "create table failed")

	// get table engine
	tableEngine, err = manager.GetTableEngine(table.ID)
	assert.Nil(t, err, "get table engine failed")

	// players buy in
	for _, joinPlayer := range players {
		assert.Nil(t, tableEngine.PlayerReserve(joinPlayer), fmt.Sprintf("%s reserve error", joinPlayer.PlayerID))
		assert.Nil(t, tableEngine.PlayerJoin(joinPlayer.PlayerID), fmt.Sprintf("%s join error", joinPlayer.PlayerID))
	}

	// start game
	tableEngine.UpdateBlind(1, 0, 0, 10, 20)
	assert.Nil(t, tableEngine.StartTableGame(), "start table game failed")

	wg.Wait()
}
