package pwbtable

type Manager interface {
	Reset()

	// Table Actions
	GetTableEngine(tableID string) (TableEngine, error)
	CreateTable(options *TableEngineOptions, callbacks *TableEngineCallbacks, setting TableSetting) (*Table, error)
	PauseTable(tableID string) error
	CloseTable(tableID string) error
	StartTableGame(tableID string) error
	TableGameOpen(tableID string) error
	UpdateBlind(tableID string, level int, ante, dealer, sb, bb int64) error
	UpdateTablePlayers(tableID string, joinPlayers []JoinPlayer, leavePlayerIDs []string) (map[string]int, error)

	// Player Table Actions
	PlayerReserve(tableID string, joinPlayer JoinPlayer) error
	PlayerJoin(tableID, playerID string) error
	PlayerRedeemChips(tableID string, joinPlayer JoinPlayer) error
	PlayersLeave(tableID string, playerIDs []string) error

	// Player Game Actions
	PlayerReady(tableID, playerID string) error
	PlayerPay(tableID, playerID string, chips int64) error
	PlayerBet(tableID, playerID string, chips int64) error
	PlayerRaise(tableID, playerID string, chipLevel int64) error
	PlayerCall(tableID, playerID string) error
	PlayerAllin(tableID, playerID string) error
	PlayerCheck(tableID, playerID string) error
	PlayerFold(tableID, playerID string) error
	PlayerPass(tableID, playerID string) error
}
