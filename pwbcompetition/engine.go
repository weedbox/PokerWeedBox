package pwbcompetition

import (
	"sync"

	pwdblind "github.com/weedbox/PokerWeedBox/pwbcompetition/blind"
	"github.com/weedbox/PokerWeedBox/pwbtable"
)

type CompetitionEngineOpt func(*competitionEngine)

type CompetitionEngine interface {
	// Others
	UpdateTable(table *pwbtable.Table)
	UpdateReserveTablePlayerState(tableID string, playerState *pwbtable.TablePlayerState)

	// Events
	OnCompetitionUpdated(fn func(competition *Competition))
	OnCompetitionErrorUpdated(fn func(competition *Competition, err error))
	OnCompetitionPlayerUpdated(fn func(competitionID string, competitionPlayer *CompetitionPlayer))
	OnCompetitionFinalPlayerRankUpdated(fn func(competitionID, playerID string, rank int))
	OnCompetitionPlayerCashOut(fn func(competitionID string, competitionPlayer *CompetitionPlayer))

	// Competition Actions
	GetCompetition() *Competition
	CreateCompetition(competitionSetting CompetitionSetting) (*Competition, error)
	CloseCompetition(endStatus CompetitionStateStatus) error
	StartCompetition() (int64, error)

	// Player Operations
	PlayerBuyIn(joinPlayer JoinPlayer) error
	PlayerRefund(playerID string) error
	PlayerCashOut(tableID, playerID string) error
	PlayerQuit(tableID, playerID string) error

	OnTableCreated(fn func(table *pwbtable.Table))
}

type competitionEngine struct {
	mu                                  sync.RWMutex
	competition                         *Competition
	playerCaches                        sync.Map // key: <competitionID.playerID>, value: PlayerCache
	tableOptions                        *pwbtable.TableEngineOptions
	tableManagerBackend                 TableManagerBackend
	onCompetitionUpdated                func(competition *Competition)
	onCompetitionErrorUpdated           func(competition *Competition, err error)
	onCompetitionPlayerUpdated          func(competitionID string, competitionPlayer *CompetitionPlayer)
	onCompetitionFinalPlayerRankUpdated func(competitionID, playerID string, rank int)
	onCompetitionStateUpdated           func(event string, competition *Competition)
	onAdvancePlayerCountUpdated         func(competitionID string, totalBuyInCount int) int
	onCompetitionPlayerCashOut          func(competitionID string, competitionPlayer *CompetitionPlayer)
	breakingPauseResumeStates           map[string]map[int]bool // key: tableID, value: (k,v): (breaking blind level index, is resume from pause)
	blind                               pwdblind.Blind

	// TODO: Test Only
	onTableCreated func(table *pwbtable.Table)
}

func NewCompetitionEngine(opts ...CompetitionEngineOpt) CompetitionEngine {
	ce := &competitionEngine{
		playerCaches:                        sync.Map{},
		onCompetitionUpdated:                func(competition *Competition) {},
		onCompetitionErrorUpdated:           func(competition *Competition, err error) {},
		onCompetitionPlayerUpdated:          func(competitionID string, competitionPlayer *CompetitionPlayer) {},
		onCompetitionFinalPlayerRankUpdated: func(competitionID, playerID string, rank int) {},
		onCompetitionStateUpdated:           func(event string, competition *Competition) {},
		onAdvancePlayerCountUpdated:         func(competitionID string, totalBuyInCount int) int { return 0 },
		onCompetitionPlayerCashOut:          func(competitionID string, competitionPlayer *CompetitionPlayer) {},
		breakingPauseResumeStates:           make(map[string]map[int]bool),
		blind:                               pwdblind.NewBlind(),

		// TODO: Test Only
		onTableCreated: func(table *pwbtable.Table) {},
	}

	for _, opt := range opts {
		opt(ce)
	}

	return ce
}

func (ce *competitionEngine) UpdateTable(table *pwbtable.Table) {
	// TODO: implement it
}

func (ce *competitionEngine) UpdateReserveTablePlayerState(tableID string, playerState *pwbtable.TablePlayerState) {
	// TODO: implement it
}

func (ce *competitionEngine) OnCompetitionUpdated(fn func(competition *Competition)) {
	// TODO: implement it
}

func (ce *competitionEngine) OnCompetitionErrorUpdated(fn func(competition *Competition, err error)) {
	// TODO: implement it
}

func (ce *competitionEngine) OnCompetitionPlayerUpdated(fn func(competitionID string, competitionPlayer *CompetitionPlayer)) {
	// TODO: implement it
}

func (ce *competitionEngine) OnCompetitionFinalPlayerRankUpdated(fn func(competitionID, playerID string, rank int)) {
	// TODO: implement it
}

func (ce *competitionEngine) OnCompetitionPlayerCashOut(fn func(competitionID string, competitionPlayer *CompetitionPlayer)) {
	// TODO: implement it
}

func (ce *competitionEngine) GetCompetition() *Competition {
	// TODO: implement it
	return nil
}

func (ce *competitionEngine) CreateCompetition(competitionSetting CompetitionSetting) (*Competition, error) {
	// TODO: implement it
	return nil, nil
}

func (ce *competitionEngine) CloseCompetition(endStatus CompetitionStateStatus) error {
	// TODO: implement it
	return nil
}

func (ce *competitionEngine) StartCompetition() (int64, error) {
	// TODO: implement it
	return 0, nil
}

func (ce *competitionEngine) PlayerBuyIn(joinPlayer JoinPlayer) error {
	// TODO: implement it
	return nil
}

func (ce *competitionEngine) PlayerRefund(playerID string) error {
	// TODO: implement it
	return nil
}

func (ce *competitionEngine) PlayerCashOut(tableID, playerID string) error {
	// TODO: implement it
	return nil
}

func (ce *competitionEngine) PlayerQuit(tableID, playerID string) error {
	// TODO: implement it
	return nil
}

func (ce *competitionEngine) OnTableCreated(fn func(table *pwbtable.Table)) {
	// TODO: implement it
}
