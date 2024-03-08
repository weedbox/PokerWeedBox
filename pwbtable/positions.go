package pwbtable

import (
	"math/rand"
	"time"
)

func NewDefaultSeatMap(seatCount int) []int {
	seatMap := make([]int, seatCount)
	for seatIdx := 0; seatIdx < seatCount; seatIdx++ {
		seatMap[seatIdx] = UnsetValue
	}
	return seatMap
}

func RandomSeats(seatMap []int, count int) ([]int, error) {
	emptySeats := make([]int, 0)
	for seatIdx, playerIdx := range seatMap {
		if playerIdx == UnsetValue {
			emptySeats = append(emptySeats, seatIdx)
		}
	}

	if len(emptySeats) < count {
		return nil, ErrTableNoEmptySeats
	}

	rand.Seed(time.Now().UnixNano())
	rand.Shuffle(len(emptySeats), func(i, j int) {
		emptySeats[i], emptySeats[j] = emptySeats[j], emptySeats[i]
	})

	return emptySeats[:count], nil
}

func IsBetweenDealerBB(seatIdx, currDealerTableSeatIdx, currBBTableSeatIdx, maxPlayerCount int, rule string) bool {
	if rule == CompetitionRule_ShortDeck {
		return false
	}

	if currBBTableSeatIdx-currDealerTableSeatIdx < 0 {
		for i := currDealerTableSeatIdx + 1; i < (currBBTableSeatIdx + maxPlayerCount); i++ {
			if i%maxPlayerCount == seatIdx {
				return true
			}
		}
	}

	return seatIdx < currBBTableSeatIdx && seatIdx > currDealerTableSeatIdx
}

func FindDealerPlayerIndex(gameCount, prevDealerSeatIdx, minPlayingCount, maxSeatCount int, players []*TablePlayerState, seatMap []int) int {
	newDealerIdx := UnsetValue
	if gameCount == 0 {
		newDealerIdx = rand.Intn(len(players))

		if !players[newDealerIdx].IsParticipated {
			for playerIdx := 0; playerIdx < len(players); playerIdx++ {
				if players[playerIdx].IsParticipated {
					newDealerIdx = playerIdx
					break
				}
			}
		}
	} else {
		for i := prevDealerSeatIdx + 1; i < (maxSeatCount + prevDealerSeatIdx + 1); i++ {
			targetTableSeatIdx := i % maxSeatCount
			targetPlayerIdx := seatMap[targetTableSeatIdx]

			if targetPlayerIdx != UnsetValue && players[targetPlayerIdx].IsParticipated && players[targetPlayerIdx].IsIn {
				newDealerIdx = targetPlayerIdx
				break
			}
		}
	}
	return newDealerIdx
}

func FindGamePlayerIndexes(dealerSeatIdx int, seatMap []int, players []*TablePlayerState) []int {
	dealerPlayerIndex := seatMap[dealerSeatIdx]

	totalPlayersCount := 0
	gamePlayerIndexes := make([]int, 0)

	seatMapDealerPlayerIdx := UnsetValue

	for _, playerIndex := range seatMap {
		if playerIndex == UnsetValue {
			continue
		}
		player := players[playerIndex]
		if player.IsParticipated {
			if playerIndex == dealerPlayerIndex {
				seatMapDealerPlayerIdx = totalPlayersCount
			}

			totalPlayersCount++
			gamePlayerIndexes = append(gamePlayerIndexes, playerIndex)
		}
	}

	if len(gamePlayerIndexes) == 0 {
		return gamePlayerIndexes
	}

	gamePlayerIndexes = rotateIntArray(gamePlayerIndexes, seatMapDealerPlayerIdx)

	return gamePlayerIndexes
}

func GetPlayerPositionMap(rule string, players []*TablePlayerState, gamePlayerIndexes []int) map[int][]string {
	playerPositionMap := make(map[int][]string)
	positions := newPositions(len(gamePlayerIndexes))
	for gamePlayerIdx, playerIdx := range gamePlayerIndexes {
		playerPositionMap[playerIdx] = positions[gamePlayerIdx]
	}
	return playerPositionMap
}

func newPositions(playerCount int) [][]string {
	switch playerCount {
	case 10:
		return [][]string{
			{Position_Dealer},
			{Position_SB},
			{Position_BB},
			{Position_UG},
			{Position_UG2},
			{Position_UG3},
			{Position_MP},
			{Position_MP2},
			{Position_HJ},
			{Position_CO},
		}
	case 9:
		return [][]string{
			{Position_Dealer},
			{Position_SB},
			{Position_BB},
			{Position_UG},
			{Position_UG2},
			{Position_MP},
			{Position_MP2},
			{Position_HJ},
			{Position_CO},
		}
	case 8:
		return [][]string{
			{Position_Dealer},
			{Position_SB},
			{Position_BB},
			{Position_UG},
			{Position_UG2},
			{Position_MP},
			{Position_HJ},
			{Position_CO},
		}
	case 7:
		return [][]string{
			{Position_Dealer},
			{Position_SB},
			{Position_BB},
			{Position_UG},
			{Position_MP},
			{Position_HJ},
			{Position_CO},
		}
	case 6:
		return [][]string{
			{Position_Dealer},
			{Position_SB},
			{Position_BB},
			{Position_UG},
			{Position_HJ},
			{Position_CO},
		}
	case 5:
		return [][]string{
			{Position_Dealer},
			{Position_SB},
			{Position_BB},
			{Position_UG},
			{Position_CO},
		}
	case 4:
		return [][]string{
			{Position_Dealer},
			{Position_SB},
			{Position_BB},
			{Position_UG},
		}
	case 3:
		return [][]string{
			{Position_Dealer},
			{Position_SB},
			{Position_BB},
		}
	case 2:
		return [][]string{
			{Position_Dealer, Position_SB},
			{Position_BB},
		}
	default:
		return make([][]string, 0)
	}
}

func rotateIntArray(source []int, startIndex int) []int {
	if startIndex > len(source) {
		startIndex = startIndex % len(source)
	}
	return append(source[startIndex:], source[:startIndex]...)
}
