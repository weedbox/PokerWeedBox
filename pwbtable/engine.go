package pwbtable

type TableEngine interface {
	OnTableUpdated(fn func(table *Table))
	OnTableErrorUpdated(fn func(table *Table, err error))
	OnTableStateUpdated(fn func(string, *Table))
	OnTablePlayerStateUpdated(fn func(string, string, *TablePlayerState))
	OnTablePlayerReserved(fn func(competitionID, tableID string, playerState *TablePlayerState))
	OnGamePlayerActionUpdated(fn func(TablePlayerGameAction))

	GetTable() *Table
	GetGame() Game
	CreateTable(tableSetting TableSetting) (*Table, error)
	PauseTable() error
	CloseTable() error
	StartTableGame() error
	TableGameOpen() error
	UpdateBlind(level int, ante, dealer, sb, bb int64)
	UpdateTablePlayers(joinPlayers []JoinPlayer, leavePlayerIDs []string) (map[string]int, error)

	PlayerReserve(joinPlayer JoinPlayer) error
	PlayerJoin(playerID string) error
	PlayerRedeemChips(joinPlayer JoinPlayer) error
	PlayersLeave(playerIDs []string) error

	PlayerReady(playerID string) error
	PlayerPay(playerID string, chips int64) error
	PlayerBet(playerID string, chips int64) error
	PlayerRaise(playerID string, chipLevel int64) error
	PlayerCall(playerID string) error
	PlayerAllin(playerID string) error
	PlayerCheck(playerID string) error
	PlayerFold(playerID string) error
	PlayerPass(playerID string) error
}
