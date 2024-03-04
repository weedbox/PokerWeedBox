package pwbtable

import (
	"github.com/weedbox/syncsaga"
)

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
	joinPlayerIDs := make([]string, 0)
	for idx, player := range players {
		reservedSeat := player.Seat
		joinPlayerIDs = append(joinPlayerIDs, player.PlayerID)

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

func (te *tableEngine) openGame() error {
	// TODO: implement openGame
	return nil
}

func (te *tableEngine) startGame() error {
	// TODO: implement startGame
	return nil
}
