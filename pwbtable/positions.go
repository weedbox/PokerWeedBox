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
