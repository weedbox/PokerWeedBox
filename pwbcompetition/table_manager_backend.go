package pwbcompetition

import (
	"encoding/json"

	"github.com/weedbox/PokerWeedBox/pwbtable"
)

type TableManagerBackend interface {
	// Events
	OnTableUpdated(fn func(table *pwbtable.Table))
	OnTablePlayerReserved(fn func(tableID string, playerState *pwbtable.TablePlayerState))

	// TableManager Table Actions
	CreateTable(options *pwbtable.TableEngineOptions, setting pwbtable.TableSetting) (*pwbtable.Table, error)
	PauseTable(tableID string) error
	CloseTable(tableID string) error
	StartTableGame(tableID string) error
	TableGameOpen(tableID string) error
	UpdateBlind(tableID string, level int, ante, dealer, sb, bb int64) error

	// TableManager Player Table Actions
	PlayerReserve(tableID string, joinPlayer pwbtable.JoinPlayer) error
	PlayerJoin(tableID, playerID string) error
	PlayerRedeemChips(tableID string, joinPlayer pwbtable.JoinPlayer) error
	PlayersLeave(tableID string, playerIDs []string) error

	// Others
	UpdateTable(table *pwbtable.Table)
}

func NewNativeTableManagerBackend(manager pwbtable.Manager) TableManagerBackend {
	backend := nativeTableManagerBackend{
		manager:               manager,
		onTableUpdated:        func(t *pwbtable.Table) {},
		onTablePlayerReserved: func(tableID string, playerState *pwbtable.TablePlayerState) {},
	}
	return &backend
}

type nativeTableManagerBackend struct {
	manager               pwbtable.Manager
	onTableUpdated        func(table *pwbtable.Table)
	onTablePlayerReserved func(tableID string, playerState *pwbtable.TablePlayerState)
}

func (ntmb *nativeTableManagerBackend) OnTableUpdated(fn func(table *pwbtable.Table)) {
	ntmb.onTableUpdated = fn
}

func (ntmb *nativeTableManagerBackend) OnTablePlayerReserved(fn func(tableID string, playerState *pwbtable.TablePlayerState)) {
	ntmb.onTablePlayerReserved = fn
}

func (ntmb *nativeTableManagerBackend) CreateTable(options *pwbtable.TableEngineOptions, setting pwbtable.TableSetting) (*pwbtable.Table, error) {
	callbacks := pwbtable.NewTableEngineCallbacks()
	callbacks.OnTableUpdated = func(t *pwbtable.Table) {
		data, err := json.Marshal(t)
		if err != nil {
			return
		}

		var cloneTable pwbtable.Table
		err = json.Unmarshal([]byte(data), &cloneTable)
		if err != nil {
			return
		}

		ntmb.onTableUpdated(&cloneTable)
	}
	callbacks.OnTablePlayerReserved = func(competitionID, tableID string, playerState *pwbtable.TablePlayerState) {
		data, err := json.Marshal(playerState)
		if err != nil {
			return
		}

		var clonePlayerState pwbtable.TablePlayerState
		err = json.Unmarshal([]byte(data), &clonePlayerState)
		if err != nil {
			return
		}

		ntmb.onTablePlayerReserved(tableID, &clonePlayerState)
	}

	table, err := ntmb.manager.CreateTable(options, callbacks, setting)
	if err != nil {
		return nil, err
	}

	return table, nil
}

func (ntbm *nativeTableManagerBackend) PauseTable(tableID string) error {
	return ntbm.manager.PauseTable(tableID)
}

func (ntmb *nativeTableManagerBackend) CloseTable(tableID string) error {
	return ntmb.manager.CloseTable(tableID)
}

func (ntbm *nativeTableManagerBackend) StartTableGame(tableID string) error {
	return ntbm.manager.StartTableGame(tableID)
}

func (ntbm *nativeTableManagerBackend) TableGameOpen(tableID string) error {
	return ntbm.manager.TableGameOpen(tableID)
}

func (ntbm *nativeTableManagerBackend) UpdateBlind(tableID string, level int, ante, dealer, sb, bb int64) error {
	return ntbm.manager.UpdateBlind(tableID, level, ante, dealer, sb, bb)
}

func (ntbm *nativeTableManagerBackend) PlayerReserve(tableID string, joinPlayer pwbtable.JoinPlayer) error {
	return ntbm.manager.PlayerReserve(tableID, joinPlayer)
}

func (ntbm *nativeTableManagerBackend) PlayerJoin(tableID, playerID string) error {
	return ntbm.manager.PlayerJoin(tableID, playerID)
}

func (ntmb *nativeTableManagerBackend) PlayerRedeemChips(tableID string, joinPlayer pwbtable.JoinPlayer) error {
	return ntmb.manager.PlayerRedeemChips(tableID, joinPlayer)
}

func (ntmb *nativeTableManagerBackend) PlayersLeave(tableID string, playerIDs []string) error {
	return ntmb.manager.PlayersLeave(tableID, playerIDs)
}

func (ntbm *nativeTableManagerBackend) UpdateTable(table *pwbtable.Table) {
	ntbm.onTableUpdated(table)
}
