package pwbtable

import "github.com/weedbox/pokerface"

type Game interface {
	// Events
	OnGameStateUpdated(func(*pokerface.GameState))
	OnGameErrorUpdated(func(*pokerface.GameState, error))

	// Others
	GetGameState() *pokerface.GameState
	Start() (*pokerface.GameState, error)
	Next() (*pokerface.GameState, error)

	// Group Actions
	ReadyForAll() (*pokerface.GameState, error)
	PayAnte() (*pokerface.GameState, error)
	PayBlinds() (*pokerface.GameState, error)

	// Single Actions
	Ready(playerIdx int) (*pokerface.GameState, error)
	Pay(playerIdx int, chips int64) (*pokerface.GameState, error)
	Pass(playerIdx int) (*pokerface.GameState, error)
	Fold(playerIdx int) (*pokerface.GameState, error)
	Check(playerIdx int) (*pokerface.GameState, error)
	Call(playerIdx int) (*pokerface.GameState, error)
	Allin(playerIdx int) (*pokerface.GameState, error)
	Bet(playerIdx int, chips int64) (*pokerface.GameState, error)
	Raise(playerIdx int, chipLevel int64) (*pokerface.GameState, error)
}
