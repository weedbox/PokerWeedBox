package actor

import (
	"fmt"
	"sync"
	"testing"

	"github.com/google/uuid"
	"github.com/stretchr/testify/assert"
	"github.com/weedbox/PokerWeedBox/pwbtable"
)

func TestActor_Basic(t *testing.T) {
	var wg sync.WaitGroup
	var tableEngine pwbtable.TableEngine

	// Initializing table
	// create manager & table
	actors := make([]Actor, 0)
	manager := pwbtable.NewManager()
	tableSetting := pwbtable.TableSetting{
		TableID: uuid.New().String(),
		Meta: pwbtable.TableMeta{
			CompetitionID:       uuid.New().String(),
			Rule:                pwbtable.CompetitionRule_Default,
			Mode:                pwbtable.CompetitionMode_Cash,
			MaxDuration:         10,
			TableMaxSeatCount:   9,
			TableMinPlayerCount: 2,
			MinChipUnit:         10,
			ActionTime:          10,
		},
	}
	tableEngineOption := pwbtable.NewTableEngineOptions()
	tableEngineOption.Interval = 1
	tableEngineCallbacks := pwbtable.NewTableEngineCallbacks()
	tableEngineCallbacks.OnTableUpdated = func(table *pwbtable.Table) {
		// Update table state via adapter
		for _, a := range actors {
			a.GetTable().UpdateTableState(table)
		}

		switch table.State.Status {
		case pwbtable.TableStateStatus_TableGameOpened:
			DebugPrintTableGameOpened(*table)
		case pwbtable.TableStateStatus_TableGameSettled:
			DebugPrintTableGameSettled(*table)
		case pwbtable.TableStateStatus_TablePausing:
			err := tableEngine.CloseTable()
			assert.Nil(t, err, "close table failed")
		case pwbtable.TableStateStatus_TableClosed:
			t.Log("table is closed")
			wg.Done()
			return
		}
	}
	tableEngineCallbacks.OnTableErrorUpdated = func(table *pwbtable.Table, err error) {
		t.Log("[Table] Error:", err)
	}
	table, err := manager.CreateTable(tableEngineOption, tableEngineCallbacks, tableSetting)
	assert.Nil(t, err, "create table failed")

	// get table engine
	tableEngine, err = manager.GetTableEngine(table.ID)
	assert.Nil(t, err, "get table engine failed")

	// Initializing bot
	redeemChips := int64(30000)
	seat := -1
	players := []pwbtable.JoinPlayer{
		{PlayerID: "Jeffrey", RedeemChips: redeemChips, Seat: seat},
		{PlayerID: "Chuck", RedeemChips: redeemChips, Seat: seat},
		{PlayerID: "Fred", RedeemChips: redeemChips, Seat: seat},
		{PlayerID: "Loz", RedeemChips: redeemChips, Seat: seat},
		{PlayerID: "Kimi", RedeemChips: redeemChips, Seat: seat},
	}

	// Preparing actors
	for _, p := range players {

		// Create new actor
		a := NewActor()

		// Initializing table engine adapter to communicate with table engine
		tc := NewTableEngineAdapter(tableEngine, table)
		a.SetAdapter(tc)

		// Initializing bot runner
		bot := NewBotRunner(p.PlayerID)
		bot.OnTableAutoJoinActionRequested(func(competitionID, tableID, playerID string) {
			assert.Nil(t, tableEngine.PlayerJoin(playerID), fmt.Sprintf("%s join error", playerID))
		})
		a.SetRunner(bot)

		actors = append(actors, a)
	}
	wg.Add(1)

	// Add players to table
	for _, p := range players {
		assert.Nil(t, tableEngine.PlayerReserve(p), fmt.Sprintf("%s reserve error", p.PlayerID))
	}

	// Start game
	tableEngine.UpdateBlind(1, 0, 0, 10, 20)
	err = tableEngine.StartTableGame()
	assert.Nil(t, err)

	wg.Wait()
}
