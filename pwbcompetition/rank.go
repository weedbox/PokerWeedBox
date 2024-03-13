package pwbcompetition

import (
	"sort"

	"github.com/thoas/go-funk"
	"github.com/weedbox/PokerWeedBox/pwbtable"
)

type RankData struct {
	PlayerID string
	Rank     int
	Chips    int64
}

func (ce *competitionEngine) GetParticipatedPlayerTableRankingData(competitionID string, tablePlayers []*pwbtable.TablePlayerState, gamePlayerIndexes []int) map[string]RankData {
	playingPlayers := make([]pwbtable.TablePlayerState, 0)
	for _, playerIdx := range gamePlayerIndexes {
		player := tablePlayers[playerIdx]
		playingPlayers = append(playingPlayers, *player)
	}

	// sort result
	sort.Slice(playingPlayers, func(i int, j int) bool {
		playerCacheI, iExist := ce.getPlayerCache(competitionID, playingPlayers[i].PlayerID)
		playerCacheJ, jExist := ce.getPlayerCache(competitionID, playingPlayers[j].PlayerID)
		if playingPlayers[i].Bankroll == playingPlayers[j].Bankroll && (iExist && jExist) {
			return playerCacheI.JoinAt < playerCacheJ.JoinAt
		}
		return playingPlayers[i].Bankroll > playingPlayers[j].Bankroll
	})

	// build RankingData
	rank := 1
	rankingData := make(map[string]RankData)

	// add playing player ranks
	for _, playingPlayer := range playingPlayers {
		rankingData[playingPlayer.PlayerID] = RankData{
			PlayerID: playingPlayer.PlayerID,
			Rank:     rank,
			Chips:    playingPlayer.Bankroll,
		}
		rank++
	}

	// add not participating player ranks
	for _, player := range tablePlayers {
		if _, exist := rankingData[player.PlayerID]; !exist {
			rankingData[player.PlayerID] = RankData{
				PlayerID: player.PlayerID,
				Rank:     0,
				Chips:    player.Bankroll,
			}
		}
	}

	return rankingData
}

func (ce *competitionEngine) GetSortedTableSettlementKnockoutPlayerRankings(tablePlayers []*pwbtable.TablePlayerState) []string {
	competitionID := ce.competition.ID
	sortedKnockoutPlayers := make([]pwbtable.TablePlayerState, 0)

	for _, p := range tablePlayers {
		if p.Bankroll > 0 {
			continue
		}

		allowToReBuy := false
		isAlreadyKnockout := false
		if playerCache, exist := ce.getPlayerCache(competitionID, p.PlayerID); exist {
			allowToReBuy = playerCache.ReBuyTimes < ce.competition.Meta.ReBuySetting.MaxTime
			isAlreadyKnockout = ce.competition.State.Players[playerCache.PlayerIdx].Status == CompetitionPlayerStatus_Knockout
		}
		if !ce.competition.State.BlindState.IsStopBuyIn() && allowToReBuy {
			continue
		}

		if isAlreadyKnockout {
			continue
		}

		sortedKnockoutPlayers = append(sortedKnockoutPlayers, *p)
	}

	sort.Slice(sortedKnockoutPlayers, func(i int, j int) bool {
		playerCacheI, iExist := ce.getPlayerCache(competitionID, sortedKnockoutPlayers[i].PlayerID)
		playerCacheJ, jExist := ce.getPlayerCache(competitionID, sortedKnockoutPlayers[j].PlayerID)
		if iExist && jExist {
			return playerCacheI.JoinAt > playerCacheJ.JoinAt
		}
		return true
	})

	return funk.Map(sortedKnockoutPlayers, func(p pwbtable.TablePlayerState) string {
		return p.PlayerID
	}).([]string)
}

func (ce *competitionEngine) GetSortedStopBuyInKnockoutPlayerRankings() []string {
	sortedKnockoutPlayers := make([]CompetitionPlayer, 0)

	for _, p := range ce.competition.State.Players {
		if p.Chips == 0 && p.Status == CompetitionPlayerStatus_ReBuyWaiting {
			sortedKnockoutPlayers = append(sortedKnockoutPlayers, *p)
		}
	}

	sort.Slice(sortedKnockoutPlayers, func(i int, j int) bool {
		return sortedKnockoutPlayers[i].JoinAt > sortedKnockoutPlayers[j].JoinAt
	})

	playerIDs := make([]string, 0)
	for _, p := range sortedKnockoutPlayers {
		playerIDs = append(playerIDs, p.PlayerID)
	}
	return playerIDs
}

func (ce *competitionEngine) GetParticipatedPlayerCompetitionRankingData(competitionID string, players []*CompetitionPlayer) []RankData {
	playingPlayers := make([]CompetitionPlayer, 0)
	for _, player := range players {
		if player.Chips > 0 {
			playingPlayers = append(playingPlayers, *player)
		}
	}

	// sort result
	sort.Slice(playingPlayers, func(i int, j int) bool {
		playerCacheI, iExist := ce.getPlayerCache(competitionID, playingPlayers[i].PlayerID)
		playerCacheJ, jExist := ce.getPlayerCache(competitionID, playingPlayers[j].PlayerID)
		if playingPlayers[i].Chips == playingPlayers[j].Chips && (iExist && jExist) {
			return playerCacheI.JoinAt < playerCacheJ.JoinAt
		}
		return playingPlayers[i].Chips > playingPlayers[j].Chips
	})

	// build RankingData
	rank := 1
	rankingData := make([]RankData, 0)

	// add playing player ranks
	for _, playingPlayer := range playingPlayers {
		rankingData = append(rankingData, RankData{
			PlayerID: playingPlayer.PlayerID,
			Rank:     rank,
			Chips:    playingPlayer.Chips,
		})
		rank++
	}

	return rankingData
}
