package pwbcompetition

import (
	"errors"
	"sync"

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
	CloseCompetition(competitionID string, endStatus CompetitionStateStatus) error
	StartCompetition(competitionID string) (int64, error)

	// Table Actions
	GetTableEngineOptions() *pwbtable.TableEngineOptions
	SetTableEngineOptions(tableOptions *pwbtable.TableEngineOptions)
	UpdateTable(competitionID string, table *pwbtable.Table) error
	UpdateReserveTablePlayerState(competitionID, tableID string, playerState *pwbtable.TablePlayerState) error

	// Player Operations
	PlayerBuyIn(competitionID string, joinPlayer JoinPlayer) error
	PlayerCashOut(competitionID string, tableID, playerID string) error
}

type manager struct {
	tableOptions        *pwbtable.TableEngineOptions
	competitionEngines  sync.Map
	tableManagerBackend TableManagerBackend
}

func NewManager(tableManagerBackend TableManagerBackend) Manager {
	tableOptions := pwbtable.NewTableEngineOptions()
	tableOptions.Interval = 6

	return &manager{
		tableOptions:        tableOptions,
		competitionEngines:  sync.Map{},
		tableManagerBackend: tableManagerBackend,
	}
}

func (m *manager) Reset() {
	m.competitionEngines.Range(func(key, value interface{}) bool {
		_ = value.(CompetitionEngine).CloseCompetition(CompetitionStateStatus_ForceEnd)
		return true
	})

	m.competitionEngines = sync.Map{}
}

func (m *manager) GetCompetitionEngine(competitionID string) (CompetitionEngine, error) {
	competitionEngine, exist := m.competitionEngines.Load(competitionID)
	if !exist {
		return nil, ErrManagerCompetitionNotFound
	}
	return competitionEngine.(CompetitionEngine), nil
}

func (m *manager) CreateCompetition(competitionSetting CompetitionSetting, options *CompetitionEngineOptions) (*Competition, error) {
	competitionEngine := NewCompetitionEngine(
		WithTableManagerBackend(m.tableManagerBackend),
		WithTableOptions(m.tableOptions),
	)
	competitionEngine.OnCompetitionUpdated(options.OnCompetitionUpdated)
	competitionEngine.OnCompetitionErrorUpdated(options.OnCompetitionErrorUpdated)
	competitionEngine.OnCompetitionPlayerUpdated(options.OnCompetitionPlayerUpdated)
	competitionEngine.OnCompetitionPlayerCashOut(options.OnCompetitionPlayerCashOut)
	competition, err := competitionEngine.CreateCompetition(competitionSetting)
	if err != nil {
		return nil, err
	}

	m.competitionEngines.Store(competition.ID, competitionEngine)
	return competition, nil
}

func (m *manager) CloseCompetition(competitionID string, endStatus CompetitionStateStatus) error {
	competitionEngine, err := m.GetCompetitionEngine(competitionID)
	if err != nil {
		return ErrManagerCompetitionNotFound
	}

	if err := competitionEngine.CloseCompetition(endStatus); err != nil {
		return err
	}

	m.competitionEngines.Delete(competitionID)
	return nil
}

func (m *manager) StartCompetition(competitionID string) (int64, error) {
	competitionEngine, err := m.GetCompetitionEngine(competitionID)
	if err != nil {
		return 0, ErrManagerCompetitionNotFound
	}

	return competitionEngine.StartCompetition()
}

func (m *manager) GetTableEngineOptions() *pwbtable.TableEngineOptions {
	return m.tableOptions
}

func (m *manager) SetTableEngineOptions(tableOptions *pwbtable.TableEngineOptions) {
	m.tableOptions = tableOptions
}

func (m *manager) UpdateTable(competitionID string, table *pwbtable.Table) error {
	competitionEngine, err := m.GetCompetitionEngine(competitionID)
	if err != nil {
		return ErrManagerCompetitionNotFound
	}

	competitionEngine.UpdateTable(table)
	return nil
}

func (m *manager) UpdateReserveTablePlayerState(competitionID, tableID string, playerState *pwbtable.TablePlayerState) error {
	competitionEngine, err := m.GetCompetitionEngine(competitionID)
	if err != nil {
		return ErrManagerCompetitionNotFound
	}

	competitionEngine.UpdateReserveTablePlayerState(tableID, playerState)
	return nil
}

func (m *manager) PlayerBuyIn(competitionID string, joinPlayer JoinPlayer) error {
	competitionEngine, err := m.GetCompetitionEngine(competitionID)
	if err != nil {
		return ErrManagerCompetitionNotFound
	}

	return competitionEngine.PlayerBuyIn(joinPlayer)
}

func (m *manager) PlayerCashOut(competitionID string, tableID, playerID string) error {
	competitionEngine, err := m.GetCompetitionEngine(competitionID)
	if err != nil {
		return ErrManagerCompetitionNotFound
	}

	return competitionEngine.PlayerCashOut(tableID, playerID)
}
