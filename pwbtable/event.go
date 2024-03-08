package pwbtable

import (
	"fmt"
	"time"
)

func (te *tableEngine) emitEvent(eventName string, playerID string) {
	// refresh table
	te.table.UpdateAt = time.Now().Unix()
	te.table.UpdateSerial++

	// emit event
	fmt.Printf("->[Table %s][#%d][%d][%s] emit Event: %s\n", te.table.ID, te.table.UpdateSerial, te.table.State.GameCount, playerID, eventName)
	te.onTableUpdated(te.table)
}

func (te *tableEngine) emitErrorEvent(eventName string, playerID string, err error) {
	// emit event
	fmt.Printf("->[Table %s][#%d][%d][%s] emit ERROR Event: %s, Error: %v\n", te.table.ID, te.table.UpdateSerial, te.table.State.GameCount, playerID, eventName, err)
	te.onTableErrorUpdated(te.table, err)
}
