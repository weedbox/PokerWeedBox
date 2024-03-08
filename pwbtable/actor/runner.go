package actor

import (
	"github.com/weedbox/PokerWeedBox/pwbtable"
)

type Runner interface {
	SetActor(a Actor)
	UpdateTableState(t *pwbtable.Table) error
}
