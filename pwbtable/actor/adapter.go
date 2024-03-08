package actor

import (
	"time"

	"github.com/weedbox/PokerWeedBox/pwbtable"
	"github.com/weedbox/pokerface"
)

type Adapter interface {
	SetActor(a Actor)
	UpdateTableState(t *pwbtable.Table) error
	GetGamePlayerIndex(playerID string) int
	GetGameState() *pokerface.GameState

	// Player actions
	Pass(playerID string) error
	Ready(playerID string) error
	Pay(playerID string, chips int64) error
	Check(playerID string) error
	Bet(playerID string, chips int64) error
	Call(playerID string) error
	Fold(playerID string) error
	Allin(playerID string) error
	Raise(playerID string, chipLevel int64) error
	ExtendTime(playerID string, duration time.Duration) error
}
