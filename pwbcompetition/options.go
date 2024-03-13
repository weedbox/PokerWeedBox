package pwbcompetition

type CompetitionEngineOptions struct {
	OnCompetitionUpdated       func(competition *Competition)
	OnCompetitionErrorUpdated  func(competition *Competition, err error)
	OnCompetitionPlayerUpdated func(competitionID string, competitionPlayer *CompetitionPlayer)
	OnCompetitionPlayerCashOut func(competitionID string, competitionPlayer *CompetitionPlayer)
}

func NewDefaultCompetitionEngineOptions() *CompetitionEngineOptions {
	return &CompetitionEngineOptions{
		OnCompetitionUpdated:       func(competition *Competition) {},
		OnCompetitionErrorUpdated:  func(competition *Competition, err error) {},
		OnCompetitionPlayerUpdated: func(competitionID string, competitionPlayer *CompetitionPlayer) {},
		OnCompetitionPlayerCashOut: func(competitionID string, competitionPlayer *CompetitionPlayer) {},
	}
}
