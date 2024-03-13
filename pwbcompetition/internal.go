package pwbcompetition

import (
	"strings"
	"sync"
	"time"

	"github.com/thoas/go-funk"
	pwbblind "github.com/weedbox/PokerWeedBox/pwbcompetition/blind"
	"github.com/weedbox/PokerWeedBox/pwbtable"
	"github.com/weedbox/pokerface"
	"github.com/weedbox/timebank"
)

func (ce *competitionEngine) newDefaultCompetitionPlayerData(tableID, playerID string, redeemChips int64, playerStatus CompetitionPlayerStatus) (CompetitionPlayer, PlayerCache) {
	joinAt := time.Now().Unix()
	playerCache := PlayerCache{
		PlayerID:   playerID,
		JoinAt:     joinAt,
		ReBuyTimes: 0,
		TableID:    tableID,
	}

	player := CompetitionPlayer{
		PlayerID:              playerID,
		CurrentTableID:        tableID,
		CurrentSeat:           UnsetValue,
		JoinAt:                joinAt,
		Status:                playerStatus,
		Rank:                  UnsetValue,
		Chips:                 redeemChips,
		IsReBuying:            false,
		ReBuyEndAt:            UnsetValue,
		ReBuyTimes:            0,
		AddonTimes:            0,
		BestWinningPotChips:   0,
		BestWinningCombo:      make([]string, 0),
		BestWinningType:       "",
		BestWinningPower:      0,
		TotalRedeemChips:      redeemChips,
		TotalGameCounts:       0,
		TotalWalkTimes:        0,
		TotalVPIPTimes:        0,
		TotalFoldTimes:        0,
		TotalPreflopFoldTimes: 0,
		TotalFlopFoldTimes:    0,
		TotalTurnFoldTimes:    0,
		TotalRiverFoldTimes:   0,
		TotalActionTimes:      0,
		TotalRaiseTimes:       0,
		TotalCallTimes:        0,
		TotalCheckTimes:       0,
		TotalProfitTimes:      0,
	}

	return player, playerCache
}

func (ce *competitionEngine) delay(interval time.Duration, fn func() error) error {
	var err error
	var wg sync.WaitGroup
	wg.Add(1)

	timebank.NewTimeBank().NewTask(interval, func(isCancelled bool) {
		defer wg.Done()

		if isCancelled {
			return
		}

		err = fn()
	})

	wg.Wait()
	return err
}

func (ce *competitionEngine) handleCompetitionTableCreated(table *pwbtable.Table, tableIdx int) {
	switch ce.competition.Meta.Mode {
	case CompetitionMode_Cash:
		if !ce.canStartCash() {
			return
		}

		ce.activateBlind()

		// auto start game if condition is reached
		if _, err := ce.StartCompetition(); err != nil {
			ce.emitErrorEvent("Cash Auto StartCompetition", "", err)
			return
		}

		ce.updateTableBlind(table.ID)

		if err := ce.tableManagerBackend.StartTableGame(table.ID); err != nil {
			ce.emitErrorEvent("Cash Auto StartTableGame", "", err)
			return
		}
	}
}

func (ce *competitionEngine) updatePauseCompetition(table *pwbtable.Table, tableIdx int) {
	shouldReOpenGame := false
	readyPlayersCount := 0
	alivePlayerCount := 0
	for _, p := range table.State.PlayerStates {
		if p.IsIn && p.Bankroll > 0 {
			readyPlayersCount++
		}

		if p.Bankroll > 0 {
			alivePlayerCount++
		}
	}

	switch ce.competition.Meta.Mode {
	case CompetitionMode_Cash:
		shouldReOpenGame = readyPlayersCount >= ce.competition.Meta.TableMinPlayerCount
	}

	// re-open game
	if shouldReOpenGame && !ce.competition.IsBreaking() {
		if err := ce.tableManagerBackend.TableGameOpen(table.ID); err != nil {
			ce.emitErrorEvent("Game Reopen", "", err)
			return
		}
		ce.emitEvent("Game Reopen:", "")
	}
}

func (ce *competitionEngine) addCompetitionTable(tableSetting TableSetting) (string, error) {
	// create table
	setting := NewTableSetting(ce.competition.ID, ce.competition.Meta, tableSetting)
	table, err := ce.tableManagerBackend.CreateTable(ce.tableOptions, setting)
	if err != nil {
		return "", err
	}

	// add table
	ce.competition.State.Tables = append(ce.competition.State.Tables, table)
	ce.emitEvent("[addCompetitionTable]", "")

	ce.onTableCreated(table)

	return table.ID, nil
}

func (ce *competitionEngine) settleCompetition(endCompetitionStatus CompetitionStateStatus) {
	ce.updatePlayerFinalRankings()

	// close competition
	ce.competition.State.Status = endCompetitionStatus
	if ce.competition.Meta.Mode == CompetitionMode_Cash {
		ce.competition.State.EndAt = time.Now().Unix()
	}

	// close blind
	ce.blind.End()

	// Emit event
	ce.emitEvent("settleCompetition", "")

	// clear caches
	ce.deletePlayerCachesByCompetition(ce.competition.ID)
}

func (ce *competitionEngine) deleteTable(tableIdx int) {
	ce.competition.State.Tables = append(ce.competition.State.Tables[:tableIdx], ce.competition.State.Tables[tableIdx+1:]...)
}

func (ce *competitionEngine) deletePlayer(playerIdx int) {
	ce.competition.State.Players = append(ce.competition.State.Players[:playerIdx], ce.competition.State.Players[playerIdx+1:]...)
}

func (ce *competitionEngine) closeCompetitionTable(table *pwbtable.Table, tableIdx int) {
	// clean data
	delete(ce.breakingPauseResumeStates, table.ID)

	// competition close table
	ce.deleteTable(tableIdx)
	ce.emitEvent("closeCompetitionTable", "")

	if len(ce.competition.State.Tables) == 0 && !ce.isEndStatus() {
		ce.CloseCompetition(CompetitionStateStatus_End)
	}
}

func (ce *competitionEngine) settleCompetitionTable(table *pwbtable.Table, tableIdx int) {
	ce.delay(time.Millisecond*500, func() error {
		ce.updatePlayerCompetitionTableRecords(table)
		ce.handleReBuy(table)
		switch ce.competition.Meta.Mode {
		case CompetitionMode_Cash:
			ce.handleCashTableSettlement(table)
		}

		ce.handleBreaking(table.ID)

		ce.emitEvent("Table Settlement", "")

		switch ce.competition.Meta.Mode {
		case CompetitionMode_Cash:
			if ce.shouldCloseCashTable(table.State.StartAt) {
				if err := ce.tableManagerBackend.CloseTable(table.ID); err != nil {
					ce.emitErrorEvent("Table Settlement -> Close Cash Table", "", err)
				}
			}
		}

		return nil
	})
}

func (ce *competitionEngine) handleCashTableSettlement(table *pwbtable.Table) {
	leavePlayerIDs := make([]string, 0)
	leavePlayerIndexes := make(map[string]int)
	for idx, cp := range ce.competition.State.Players {
		if cp.Status == CompetitionPlayerStatus_CashLeaving {
			leavePlayerIDs = append(leavePlayerIDs, cp.PlayerID)
			leavePlayerIndexes[cp.PlayerID] = idx
		}
	}

	if len(leavePlayerIDs) > 0 {
		ce.handleCashOut(table.ID, leavePlayerIndexes, leavePlayerIDs)
	}
}

func (ce *competitionEngine) handleCashOut(tableID string, leavePlayerIndexes map[string]int, leavePlayerIDs []string) {
	// TableEngine Player Leave
	if err := ce.tableManagerBackend.PlayersLeave(tableID, leavePlayerIDs); err != nil {
		ce.emitErrorEvent("handleCashOut -> PlayersLeave", strings.Join(leavePlayerIDs, ","), err)
	}

	// Cash Out
	for _, leavePlayerID := range leavePlayerIDs {
		if playerIdx, exist := leavePlayerIndexes[leavePlayerID]; exist {
			ce.onCompetitionPlayerCashOut(ce.competition.ID, ce.competition.State.Players[playerIdx])
			ce.deletePlayerCache(ce.competition.ID, leavePlayerID)
		}
	}

	// keep players that are cashing out
	newPlayers := make([]*CompetitionPlayer, 0)
	for _, cp := range ce.competition.State.Players {
		if _, exist := leavePlayerIndexes[cp.PlayerID]; !exist {
			newPlayers = append(newPlayers, cp)
		}
	}
	ce.competition.State.Players = newPlayers
}

func (ce *competitionEngine) handleBreaking(tableID string) {
	if !ce.competition.IsBreaking() {
		return
	}

	// check breakingPauseResumeStates
	if _, exist := ce.breakingPauseResumeStates[tableID]; !exist {
		ce.breakingPauseResumeStates[tableID] = make(map[int]bool)
	}
	if _, exist := ce.breakingPauseResumeStates[tableID][ce.competition.State.BlindState.CurrentLevelIndex]; !exist {
		ce.breakingPauseResumeStates[tableID][ce.competition.State.BlindState.CurrentLevelIndex] = false
	} else {
		// fmt.Println("[DEBUG#handleBreaking] already handle breaking & start timer")
		return
	}

	// already resume table games from breaking
	if ce.breakingPauseResumeStates[tableID][ce.competition.State.BlindState.CurrentLevelIndex] {
		return
	}

	// reopen table game
	endAt := ce.competition.State.BlindState.EndAts[ce.competition.State.BlindState.CurrentLevelIndex] + 1
	if err := timebank.NewTimeBank().NewTaskWithDeadline(time.Unix(endAt, 0), func(isCancelled bool) {
		if isCancelled {
			return
		}

		if ce.isEndStatus() {
			// fmt.Println("[DEBUG#handleBreaking] not reopen since competition status is:", ce.competition.State.Status)
			return
		}

		if ce.breakingPauseResumeStates[tableID][ce.competition.State.BlindState.CurrentLevelIndex] {
			return
		}

		tableIdx := ce.competition.FindTableIdx(func(t *pwbtable.Table) bool {
			return t.ID == tableID
		})
		if len(ce.competition.State.Tables) > tableIdx && tableIdx >= 0 {
			t := ce.competition.State.Tables[tableIdx]

			autoOpenGame := t.State.Status == pwbtable.TableStateStatus_TablePausing && len(t.AlivePlayers()) >= t.Meta.TableMinPlayerCount
			if !autoOpenGame {
				return
			}

			if err := ce.tableManagerBackend.TableGameOpen(tableID); err != nil {
				ce.emitErrorEvent("resume game from breaking & auto open next game", "", err)
			} else {
				ce.breakingPauseResumeStates[tableID][ce.competition.State.BlindState.CurrentLevelIndex] = true
			}
		} else {
			// fmt.Println("[DEBUG#handleBreaking] not find table at index:", tableIdx)
		}
	}); err != nil {
		ce.emitErrorEvent("new resume game task from breaking", "", err)
		return
	}
}

func (ce *competitionEngine) handleReBuy(table *pwbtable.Table) {
	if ce.competition.State.BlindState.IsStopBuyIn() {
		return
	}

	reBuyEndAt := time.Now().Add(time.Second * time.Duration(ce.competition.Meta.ReBuySetting.WaitingTime)).Unix()
	reBuyPlayerIDs := make([]string, 0)
	for _, player := range table.State.PlayerStates {
		if player.Bankroll > 0 {
			continue
		}

		rebuyPlayerIdx := ce.competition.FindPlayerIdx(func(competitionPlayer *CompetitionPlayer) bool {
			return competitionPlayer.PlayerID == player.PlayerID
		})
		if rebuyPlayerIdx == UnsetValue {
			// fmt.Printf("[handleReBuy#start] player (%s) is not in the competition\n", player.PlayerID)
			continue
		}

		cp := ce.competition.State.Players[rebuyPlayerIdx]
		if !cp.IsReBuying {
			if cp.ReBuyTimes < ce.competition.Meta.ReBuySetting.MaxTime {
				cp.Status = CompetitionPlayerStatus_ReBuyWaiting
				cp.IsReBuying = true
				cp.ReBuyEndAt = reBuyEndAt
				reBuyPlayerIDs = append(reBuyPlayerIDs, player.PlayerID)

				ce.emitPlayerEvent("re-buying", cp)
			}
		}
	}

	keepSeatModes := []CompetitionMode{
		CompetitionMode_Cash,
	}
	if !funk.Contains(keepSeatModes, ce.competition.Meta.Mode) {
		return
	}

	if len(reBuyPlayerIDs) > 0 {
		reBuyEndAtTime := time.Unix(reBuyEndAt, 0)
		if err := timebank.NewTimeBank().NewTaskWithDeadline(reBuyEndAtTime, func(isCancelled bool) {
			if isCancelled {
				// fmt.Println("[handleReBuy#after] rebuy timer is cancelled")
				return
			}

			leavePlayerIDs := make([]string, 0)
			leavePlayerIndexes := make(map[string]int)
			for _, reBuyPlayerID := range reBuyPlayerIDs {
				reBuyPlayerIdx := ce.competition.FindPlayerIdx(func(competitionPlayer *CompetitionPlayer) bool {
					return competitionPlayer.PlayerID == reBuyPlayerID
				})
				if reBuyPlayerIdx == UnsetValue {
					// fmt.Printf("[handleReBuy#after] player (%s) is not in the competition\n", reBuyPlayerID)
					continue
				}

				cp := ce.competition.State.Players[reBuyPlayerIdx]
				if cp.Chips > 0 {
					// fmt.Printf("[handleReBuy#after] player (%s) is already re buy (%d) chips\n", reBuyPlayerID, cp.Chips)
					continue
				}

				switch ce.competition.Meta.Mode {
				case CompetitionMode_Cash:
					leavePlayerIDs = append(leavePlayerIDs, reBuyPlayerID)
					leavePlayerIndexes[reBuyPlayerID] = reBuyPlayerIdx
				}
			}

			if len(leavePlayerIDs) > 0 {
				ce.emitEvent("re buy leave", strings.Join(leavePlayerIDs, ","))
				switch ce.competition.Meta.Mode {
				case CompetitionMode_Cash:
					ce.handleCashOut(table.ID, leavePlayerIndexes, leavePlayerIDs)
				}
			}
		}); err != nil {
			ce.emitErrorEvent("ReBuy Add Timer", "", err)
		}
	}
}

func (ce *competitionEngine) updatePlayerCompetitionTableRecords(table *pwbtable.Table) {
	gamePlayerPreflopFoldTimes := 0
	for _, player := range table.State.PlayerStates {
		if !player.IsParticipated {
			continue
		}

		playerIdx := ce.competition.FindPlayerIdx(func(competitionPlayer *CompetitionPlayer) bool {
			return competitionPlayer.PlayerID == player.PlayerID
		})
		if playerIdx == UnsetValue {
			// fmt.Printf("[updatePlayerCompetitionTableRecords#statistic] player (%s) is not in the competition\n", player.PlayerID)
			continue
		}

		cp := ce.competition.State.Players[playerIdx]
		cp.TotalGameCounts++
		if player.GameStatistics.IsFold {
			cp.TotalFoldTimes++
			switch player.GameStatistics.FoldRound {
			case pwbtable.GameRound_Preflop:
				cp.TotalPreflopFoldTimes++
				gamePlayerPreflopFoldTimes++
			case pwbtable.GameRound_Flop:
				cp.TotalFlopFoldTimes++
			case pwbtable.GameRound_Turn:
				cp.TotalTurnFoldTimes++
			case pwbtable.GameRound_River:
				cp.TotalRiverFoldTimes++
			}
		}
		cp.TotalActionTimes += player.GameStatistics.ActionTimes
		cp.TotalRaiseTimes += player.GameStatistics.RaiseTimes
		cp.TotalCallTimes += player.GameStatistics.CallTimes
		cp.TotalCheckTimes += player.GameStatistics.CheckTimes
	}

	for _, playerResult := range table.State.GameState.Result.Players {
		if playerResult.Changed <= 0 {
			continue
		}

		winnerGameIdx := playerResult.Idx
		tablePlayerIdx := table.State.GamePlayerIndexes[winnerGameIdx]
		tablePlayer := table.State.PlayerStates[tablePlayerIdx]

		playerIdx := ce.competition.FindPlayerIdx(func(competitionPlayer *CompetitionPlayer) bool {
			return competitionPlayer.PlayerID == tablePlayer.PlayerID
		})
		if playerIdx == UnsetValue {
			// fmt.Printf("[updatePlayerCompetitionTableRecords#winner] player (%s) is not in the competition\n", tablePlayer.PlayerID)
			continue
		}

		cp := ce.competition.State.Players[playerIdx]
		cp.TotalProfitTimes++

		gs := table.State.GameState
		gsPlayer := gs.GetPlayer(winnerGameIdx)
		if gsPlayer.VPIP {
			cp.TotalVPIPTimes++
		}

		if table.State.CurrentBBSeat == tablePlayer.Seat && tablePlayer.GameStatistics.ActionTimes == 0 && gamePlayerPreflopFoldTimes == len(table.State.GamePlayerIndexes)-1 {
			cp.TotalWalkTimes++
		}

		if playerResult.Changed > cp.BestWinningPotChips {
			cp.BestWinningPotChips = playerResult.Changed
		}

		if gsPlayer.Combination.Power >= cp.BestWinningPower {
			cp.BestWinningPower = gsPlayer.Combination.Power
			cp.BestWinningCombo = gsPlayer.Combination.Cards
			cp.BestWinningType = gsPlayer.Combination.Type
		}
	}

	playerRankingData := ce.GetParticipatedPlayerTableRankingData(ce.competition.ID, table.State.PlayerStates, table.State.GamePlayerIndexes)
	for playerID, rankData := range playerRankingData {
		playerIdx := ce.competition.FindPlayerIdx(func(competitionPlayer *CompetitionPlayer) bool {
			return competitionPlayer.PlayerID == playerID
		})
		if playerIdx == UnsetValue {
			// fmt.Printf("[updatePlayerCompetitionTableRecords#table-settlement] player (%s) is not in the competition\n", playerID)
			continue
		}

		cp := ce.competition.State.Players[playerIdx]
		cp.Rank = rankData.Rank
		cp.Chips = rankData.Chips
		ce.emitPlayerEvent("table-settlement", cp)
	}
}

func (ce *competitionEngine) shouldCloseCashTable(tableStartAt int64) bool {
	if ce.competition.Meta.Mode != CompetitionMode_Cash {
		return false
	}

	tableEndAt := time.Unix(tableStartAt, 0).Add(time.Second * time.Duration(ce.competition.Meta.MaxDuration)).Unix()
	return time.Now().Unix() > tableEndAt
}

func (ce *competitionEngine) updateTableBlind(tableID string) {
	level, ante, dealer, sb, bb := ce.competition.CurrentBlindData()
	if err := ce.tableManagerBackend.UpdateBlind(tableID, level, ante, dealer, sb, bb); err != nil {
		ce.emitErrorEvent("update blind", "", err)
	}
}

func (ce *competitionEngine) isEndStatus() bool {
	endStatuses := []CompetitionStateStatus{
		CompetitionStateStatus_End,
		CompetitionStateStatus_AutoEnd,
		CompetitionStateStatus_ForceEnd,
	}
	return funk.Contains(endStatuses, ce.competition.State.Status)
}

func (ce *competitionEngine) initBlind(meta CompetitionMeta) {
	options := &pwbblind.BlindOptions{
		ID:                   meta.Blind.ID,
		InitialLevel:         meta.Blind.InitialLevel,
		FinalBuyInLevelIndex: meta.Blind.FinalBuyInLevelIndex,
		Levels: funk.Map(meta.Blind.Levels, func(bl BlindLevel) pwbblind.BlindLevel {
			dealer := int64(0)
			if meta.Rule == CompetitionRule_ShortDeck {
				dealer = (int64(meta.Blind.DealerBlindTime) - 1) * bl.BB
			}
			return pwbblind.BlindLevel{
				Level: bl.Level,
				Ante:  bl.Ante,
				Blind: pokerface.BlindSetting{
					Dealer: dealer,
					SB:     bl.SB,
					BB:     bl.BB,
				},
				Duration: bl.Duration,
			}
		}).([]pwbblind.BlindLevel),
	}
	ce.blind.ApplyOptions(options)
	ce.blind.OnBlindStateUpdated(func(bs *pwbblind.BlindState) {
		if ce.isEndStatus() {
			return
		}

		ce.competition.State.BlindState.CurrentLevelIndex = bs.Status.CurrentLevelIndex
		// fmt.Println("[DEBUG#initBlind] BlindState.CurrentLevelIndex:", ce.competition.State.BlindState.CurrentLevelIndex)
		for _, table := range ce.competition.State.Tables {
			ce.updateTableBlind(table.ID)
			ce.handleBreaking(table.ID)
		}

		ce.emitEvent("Blind CurrentLevelIndex Update", "")

		if ce.competition.State.BlindState.IsStopBuyIn() {
			if ce.competition.State.Status != CompetitionStateStatus_StoppedBuyIn {
				ce.competition.State.Status = CompetitionStateStatus_StoppedBuyIn
				knockoutPlayerRankings := ce.GetSortedStopBuyInKnockoutPlayerRankings()
				for _, knockoutPlayerID := range knockoutPlayerRankings {
					playerCache, exist := ce.getPlayerCache(ce.competition.ID, knockoutPlayerID)
					if !exist {
						continue
					}

					cp := ce.competition.State.Players[playerCache.PlayerIdx]
					cp.Status = CompetitionPlayerStatus_Knockout
					cp.IsReBuying = false
					cp.ReBuyEndAt = UnsetValue
					cp.CurrentSeat = UnsetValue
					ce.emitPlayerEvent("Stopped BuyIn Knockout Players", cp)

					ce.competition.State.Rankings = append(ce.competition.State.Rankings, &CompetitionRank{
						PlayerID:   knockoutPlayerID,
						FinalChips: 0,
					})
				}
			}
		}
	})
	ce.blind.OnErrorUpdated(func(bs *pwbblind.BlindState, err error) {
		ce.emitErrorEvent("Blind Update Error", "", err)
	})
}

func (ce *competitionEngine) activateBlind() {
	bs, err := ce.blind.Start()
	if err != nil {
		ce.emitErrorEvent("Start Blind Error", "", err)
	} else {
		ce.competition.State.BlindState.CurrentLevelIndex = bs.Status.CurrentLevelIndex
		ce.competition.State.BlindState.FinalBuyInLevelIndex = bs.Status.FinalBuyInLevelIndex
		copy(ce.competition.State.BlindState.EndAts, bs.Status.LevelEndAts)
	}
}

func (ce *competitionEngine) updatePlayerFinalRankings() {
	// update final player rankings
	settleStatuses := []CompetitionStateStatus{
		CompetitionStateStatus_DelayedBuyIn,
		CompetitionStateStatus_StoppedBuyIn,
	}
	if funk.Contains(settleStatuses, ce.competition.State.Status) {
		finalRankings := ce.GetParticipatedPlayerCompetitionRankingData(ce.competition.ID, ce.competition.State.Players)
		for i := len(finalRankings) - 1; i >= 0; i-- {
			ranking := finalRankings[i]
			ce.competition.State.Rankings = append(ce.competition.State.Rankings, &CompetitionRank{
				PlayerID:   ranking.PlayerID,
				FinalChips: ranking.Chips,
			})
		}

		for i, j := 0, len(ce.competition.State.Rankings)-1; i < j; i, j = i+1, j-1 {
			ce.competition.State.Rankings[i], ce.competition.State.Rankings[j] = ce.competition.State.Rankings[j], ce.competition.State.Rankings[i]
		}
	}
}

func (ce *competitionEngine) canStartCash() bool {
	if ce.competition.State.Status != CompetitionStateStatus_Registering {
		return false
	}

	currentPlayerCount := 0
	for _, table := range ce.competition.State.Tables {
		for _, player := range table.State.PlayerStates {
			if player.IsIn && player.Bankroll > 0 {
				currentPlayerCount++
			}
		}
	}

	return currentPlayerCount >= ce.competition.Meta.MinPlayerCount
}
