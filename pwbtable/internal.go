package pwbtable

import (
	"fmt"
	"sync"
	"time"

	"github.com/weedbox/pokerface"
	"github.com/weedbox/syncsaga"
)

func (te *tableEngine) delay(interval int, fn func() error) error {
	var err error
	var wg sync.WaitGroup
	wg.Add(1)

	te.tb.NewTask(time.Duration(interval)*time.Second, func(isCancelled bool) {
		defer wg.Done()

		if isCancelled {
			return
		}

		err = fn()
	})

	wg.Wait()
	return err
}

func (te *tableEngine) updateGameState(gs *pokerface.GameState) {
	te.table.State.GameState = gs

	event, ok := pokerface.GameEventBySymbol[gs.Status.CurrentEvent]
	if !ok {
		te.emitErrorEvent("handle updateGameState", "", ErrGameUnknownEvent)
		return
	}

	switch event {
	case pokerface.GameEvent_GameClosed:
		if err := te.onGameClosed(); err != nil {
			te.emitErrorEvent("onGameClosed", "", err)
		}
	default:
		te.emitEvent(gs.Status.CurrentEvent, "")
	}
}

func (te *tableEngine) validateGameMove(gamePlayerIdx int) error {
	// check table status
	if te.table.State.Status != TableStateStatus_TableGamePlaying {
		return ErrTablePlayerInvalidGameAction
	}

	// check game player index
	if gamePlayerIdx == UnsetValue {
		return ErrTablePlayerNotFound
	}

	return nil
}

func (te *tableEngine) batchAddPlayers(players []JoinPlayer) error {
	// decide seats
	availableSeats, err := RandomSeats(te.table.State.SeatMap, len(players))
	if err != nil {
		return err
	}

	// update table state
	newSeatMap := make([]int, len(te.table.State.SeatMap))
	copy(newSeatMap, te.table.State.SeatMap)
	newPlayers := make([]*TablePlayerState, 0)
	for idx, player := range players {
		reservedSeat := player.Seat

		// add new player
		var seat int
		if reservedSeat == UnsetValue {
			seat = availableSeats[idx]
		} else {
			if te.table.State.SeatMap[reservedSeat] == UnsetValue {
				seat = reservedSeat
			} else {
				return ErrTablePlayerSeatUnavailable
			}
		}

		// update state
		player := &TablePlayerState{
			PlayerID:          player.PlayerID,
			Seat:              seat,
			Positions:         []string{Position_Unknown},
			IsParticipated:    false,
			IsBetweenDealerBB: IsBetweenDealerBB(seat, te.table.State.CurrentDealerSeat, te.table.State.CurrentBBSeat, te.table.Meta.TableMaxSeatCount, te.table.Meta.Rule),
			Bankroll:          player.RedeemChips,
			IsIn:              false,
			GameStatistics:    TablePlayerGameStatistics{},
		}
		newPlayers = append(newPlayers, player)

		newPlayerIdx := len(te.table.State.PlayerStates) + len(newPlayers) - 1
		newSeatMap[seat] = newPlayerIdx
	}

	te.table.State.SeatMap = newSeatMap
	te.table.State.PlayerStates = append(te.table.State.PlayerStates, newPlayers...)

	te.playersAutoIn()

	return nil
}

func (te *tableEngine) playersAutoIn() {
	// Preparing ready group for waiting all players' join
	te.rg.Stop()
	te.rg.SetTimeoutInterval(15)
	te.rg.OnTimeout(func(rg *syncsaga.ReadyGroup) {
		// Auto Ready By Default
		states := rg.GetParticipantStates()
		for playerIdx, isReady := range states {
			if !isReady {
				rg.Ready(playerIdx)
			}
		}
	})
	te.rg.OnCompleted(func(rg *syncsaga.ReadyGroup) {
		for playerIdx, player := range te.table.State.PlayerStates {
			if !player.IsIn {
				te.table.State.PlayerStates[playerIdx].IsIn = true
			}
		}

		if te.table.State.GameCount <= 0 {
			if err := te.StartTableGame(); err != nil {
				te.emitErrorEvent("StartTableGame", "", err)
			}
		}
	})

	te.rg.ResetParticipants()
	for playerIdx := range te.table.State.PlayerStates {
		if !te.table.State.PlayerStates[playerIdx].IsIn {
			te.rg.Add(int64(playerIdx), false)
		}
	}

	te.rg.Start()
}

func (te *tableEngine) batchRemovePlayers(playerIDs []string) {
	newPlayerStates, newSeatMap, newGamePlayerIndexes := te.calcLeavePlayers(te.table.State.Status, playerIDs, te.table.State.PlayerStates, te.table.Meta.TableMaxSeatCount)
	te.table.State.PlayerStates = newPlayerStates
	te.table.State.SeatMap = newSeatMap
	te.table.State.GamePlayerIndexes = newGamePlayerIndexes
}

func (te *tableEngine) openGame(oldTable *Table) (*Table, error) {
	if !oldTable.State.BlindState.IsSet() {
		return oldTable, ErrTableOpenGameFailed
	}

	cloneTable, err := oldTable.Clone()
	if err != nil {
		return oldTable, err
	}

	cloneTable.State.Status = TableStateStatus_TableGameOpened

	for i := 0; i < len(cloneTable.State.PlayerStates); i++ {
		playerState := cloneTable.State.PlayerStates[i]

		if !playerState.IsIn {
			playerState.IsParticipated = false
			continue
		}

		if playerState.IsParticipated || playerState.IsBetweenDealerBB {
			playerState.IsParticipated = playerState.Bankroll > 0
			continue
		}

		playerState.IsParticipated = playerState.Bankroll > 0
	}

	if len(cloneTable.ParticipatedPlayers()) < cloneTable.Meta.TableMinPlayerCount {
		for i := 0; i < len(cloneTable.State.PlayerStates); i++ {
			playerState := cloneTable.State.PlayerStates[i]

			if playerState.Bankroll == 0 || !playerState.IsIn {
				continue
			}

			playerState.IsParticipated = true
			playerState.IsBetweenDealerBB = false
		}
	}

	newDealerPlayerIdx := FindDealerPlayerIndex(cloneTable.State.GameCount, cloneTable.State.CurrentDealerSeat, cloneTable.Meta.TableMinPlayerCount, cloneTable.Meta.TableMaxSeatCount, cloneTable.State.PlayerStates, cloneTable.State.SeatMap)
	newDealerTableSeatIdx := cloneTable.State.PlayerStates[newDealerPlayerIdx].Seat

	for i := 0; i < len(cloneTable.State.PlayerStates); i++ {
		playerState := cloneTable.State.PlayerStates[i]

		if !playerState.IsBetweenDealerBB {
			continue
		}

		if !playerState.IsParticipated {
			continue
		}

		if newDealerTableSeatIdx-cloneTable.State.CurrentDealerSeat < 0 {
			for j := cloneTable.State.CurrentDealerSeat + 1; j < newDealerTableSeatIdx+cloneTable.Meta.TableMaxSeatCount; j++ {
				if (j % cloneTable.Meta.TableMaxSeatCount) != playerState.Seat {
					continue
				}

				playerState.IsBetweenDealerBB = false
				playerState.IsParticipated = true
			}
		} else {
			for j := cloneTable.State.CurrentDealerSeat + 1; j < newDealerTableSeatIdx; j++ {
				if j != playerState.Seat {
					continue
				}

				playerState.IsBetweenDealerBB = false
				playerState.IsParticipated = true
			}
		}
	}

	gamePlayerIndexes := FindGamePlayerIndexes(newDealerTableSeatIdx, cloneTable.State.SeatMap, cloneTable.State.PlayerStates)
	if len(gamePlayerIndexes) < cloneTable.Meta.TableMinPlayerCount {
		fmt.Printf("[DEBUG#MTT#openGame] Competition (%s), Table (%s), TableMinPlayerCount: %d, GamePlayerIndexes: %+v\n", cloneTable.Meta.CompetitionID, cloneTable.ID, cloneTable.Meta.TableMinPlayerCount, gamePlayerIndexes)
		json, _ := cloneTable.GetJSON()
		fmt.Println(json)
		return oldTable, ErrTableOpenGameFailed
	}
	cloneTable.State.GamePlayerIndexes = gamePlayerIndexes

	positionMap := GetPlayerPositionMap(cloneTable.Meta.Rule, cloneTable.State.PlayerStates, cloneTable.State.GamePlayerIndexes)
	for playerIdx := 0; playerIdx < len(cloneTable.State.PlayerStates); playerIdx++ {
		positions, exist := positionMap[playerIdx]
		if exist && cloneTable.State.PlayerStates[playerIdx].IsParticipated {
			cloneTable.State.PlayerStates[playerIdx].Positions = positions
		}
	}

	cloneTable.State.GameCount = cloneTable.State.GameCount + 1
	cloneTable.State.CurrentDealerSeat = newDealerTableSeatIdx
	if len(gamePlayerIndexes) == 2 {
		bbPlayer := cloneTable.State.PlayerStates[gamePlayerIndexes[1]]
		cloneTable.State.CurrentBBSeat = bbPlayer.Seat
	} else if len(gamePlayerIndexes) > 2 {
		gameBBPlayerIdx := 2
		bbPlayer := cloneTable.State.PlayerStates[gamePlayerIndexes[gameBBPlayerIdx]]
		cloneTable.State.CurrentBBSeat = bbPlayer.Seat
	} else {
		cloneTable.State.CurrentBBSeat = UnsetValue
	}

	return cloneTable, nil
}

func (te *tableEngine) startGame() error {
	rule := te.table.Meta.Rule
	blind := te.table.State.BlindState

	// create game options
	opts := pokerface.NewStardardGameOptions()
	opts.Deck = pokerface.NewStandardDeckCards()

	if rule == CompetitionRule_ShortDeck {
		opts = pokerface.NewShortDeckGameOptions()
		opts.Deck = pokerface.NewShortDeckCards()
	} else if rule == CompetitionRule_Omaha {
		opts.HoleCardsCount = 4
		opts.RequiredHoleCardsCount = 2
	}

	// preparing blind
	opts.Ante = blind.Ante
	opts.Blind = pokerface.BlindSetting{
		Dealer: blind.Dealer,
		SB:     blind.SB,
		BB:     blind.BB,
	}

	// preparing players
	playerSettings := make([]*pokerface.PlayerSetting, 0)
	for _, playerIdx := range te.table.State.GamePlayerIndexes {
		player := te.table.State.PlayerStates[playerIdx]
		playerSettings = append(playerSettings, &pokerface.PlayerSetting{
			Bankroll:  player.Bankroll,
			Positions: player.Positions,
		})
	}
	opts.Players = playerSettings

	// create game
	te.game = NewGame(te.gameBackend, opts)
	te.game.OnGameStateUpdated(func(gs *pokerface.GameState) {
		te.updateGameState(gs)
	})
	te.game.OnGameErrorUpdated(func(gs *pokerface.GameState, err error) {
		te.table.State.GameState = gs
		go te.emitErrorEvent("OnGameErrorUpdated", "", err)
	})

	// start game
	if _, err := te.game.Start(); err != nil {
		return err
	}

	te.table.State.Status = TableStateStatus_TableGamePlaying
	return nil
}

func (te *tableEngine) settleGame() {
	te.table.State.Status = TableStateStatus_TableGameSettled

	for _, player := range te.table.State.GameState.Result.Players {
		playerIdx := te.table.State.GamePlayerIndexes[player.Idx]
		playerState := te.table.State.PlayerStates[playerIdx]
		playerState.Bankroll = player.Final
		if playerState.Bankroll == 0 {
			playerState.IsParticipated = false
		}
	}

	te.emitEvent("SettleTableGameResult", "")
}

func (te *tableEngine) continueGame() error {
	// Reset table state
	te.table.State.Status = TableStateStatus_TableGameStandby
	te.table.State.GamePlayerIndexes = make([]int, 0)
	te.table.State.GameState = nil
	for i := 0; i < len(te.table.State.PlayerStates); i++ {
		playerState := te.table.State.PlayerStates[i]
		playerState.Positions = make([]string, 0)
		playerState.GameStatistics.ActionTimes = 0
		playerState.GameStatistics.RaiseTimes = 0
		playerState.GameStatistics.CallTimes = 0
		playerState.GameStatistics.CheckTimes = 0
		playerState.GameStatistics.IsFold = false
		playerState.GameStatistics.FoldRound = ""
	}

	return te.delay(te.options.Interval, func() error {
		if te.table.State.Status == TableStateStatus_TableClosed {
			return nil
		}

		if te.table.ShouldPause() {
			te.table.State.Status = TableStateStatus_TablePausing
			te.emitEvent("ContinueGame -> Pause", "")
		} else {
			if te.table.State.Status == TableStateStatus_TableGameStandby && len(te.table.AlivePlayers()) >= te.table.Meta.TableMinPlayerCount {
				return te.TableGameOpen()
			}
		}
		return nil
	})
}

func (te *tableEngine) onGameClosed() error {
	te.settleGame()
	return te.continueGame()
}
