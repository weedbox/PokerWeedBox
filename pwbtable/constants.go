package pwbtable

const (
	// General
	UnsetValue = -1

	// CompetitionMode
	CompetitionMode_Cash = "cash"

	// CompetitionRule
	CompetitionRule_Default   = "default"
	CompetitionRule_ShortDeck = "short_deck"
	CompetitionRule_Omaha     = "omaha"

	// Position
	Position_Unknown = "unknown"
	Position_Dealer  = "dealer"
	Position_SB      = "sb"
	Position_BB      = "bb"
	Position_UG      = "ug"
	Position_UG2     = "ug2"
	Position_UG3     = "ug3"
	Position_MP      = "mp"
	Position_MP2     = "mp2"
	Position_HJ      = "hj"
	Position_CO      = "co"

	// Action
	Action_Ready = "ready"
	Action_Pay   = "pay"

	// Wager Action
	WagerAction_Fold  = "fold"
	WagerAction_Check = "check"
	WagerAction_Call  = "call"
	WagerAction_AllIn = "allin"
	WagerAction_Bet   = "bet"
	WagerAction_Raise = "raise"

	// Round
	GameRound_Preflop = "preflop"
	GameRound_Flop    = "flop"
	GameRound_Turn    = "turn"
	GameRound_River   = "river"
)
