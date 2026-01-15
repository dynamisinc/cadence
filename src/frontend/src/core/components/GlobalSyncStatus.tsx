/**
 * GlobalSyncStatus Component
 *
 * Renders the global ConflictDialog for offline sync conflicts.
 * Should be placed at the app level to show conflicts regardless of current page.
 */

import { useOfflineSyncContext } from '../contexts'
import { ConflictDialog } from './ConflictDialog'

export const GlobalSyncStatus = () => {
  const { conflicts, clearConflicts } = useOfflineSyncContext()

  return (
    <ConflictDialog
      open={conflicts.length > 0}
      conflicts={conflicts}
      onClose={clearConflicts}
    />
  )
}

export default GlobalSyncStatus
