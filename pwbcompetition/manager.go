package pwbcompetition

import (
	"errors"

	"github.com/weedbox/PokerWeedBox/pwbtable"
)

var (
	ErrManagerCompetitionNotFound = errors.New("manager: competition not found")
)

type Manager interface {
	Reset()

	// CompetitionEngine Actions
	GetCompetitionEngine(competitionID string) (CompetitionEngine, error)

	// Competition Actions
	CreateCompetition(competitionSetting CompetitionSetting, options *CompetitionEngineOptions) (*Competition, error)
	UpdateCompetitionBlindInitialLevel(competitionID string, level int) error
	CloseCompetition(competitionID string, endStatus CompetitionStateStatus) error
	StartCompetition(competitionID string) (int64, error)

	// Table Actions
	GetTableEngineOptions() *pwbtable.TableEngineOptions
	SetTableEngineOptions(tableOptions *pwbtable.TableEngineOptions)
	UpdateTable(competitionID string, table *pwbtable.Table) error
	UpdateReserveTablePlayerState(competitionID, tableID string, playerState *pwbtable.TablePlayerState) error

	// Player Operations
	PlayerBuyIn(competitionID string, joinPlayer JoinPlayer) error
	PlayerAddon(competitionID string, tableID string, joinPlayer JoinPlayer) error
	PlayerRefund(competitionID string, playerID string) error
	PlayerCashOut(competitionID string, tableID, playerID string) error
	PlayerQuit(competitionID string, tableID, playerID string) error
}
