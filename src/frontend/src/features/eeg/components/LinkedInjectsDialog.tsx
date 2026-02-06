/**
 * LinkedInjectsDialog Component (S05)
 *
 * Dialog for viewing and managing injects linked to a Critical Task.
 * Shows currently linked injects with unlink option,
 * and available injects with link option.
 */

import { useState, useMemo, useEffect } from 'react'
import type { FC } from 'react'
import {
  Box,
  Typography,
  Stack,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  IconButton,
  List,
  ListItem,
  ListItemText,
  Chip,
  Divider,
  CircularProgress,
  Alert,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
  faXmark,
  faLink,
  faLinkSlash,
  faMagnifyingGlass,
} from '@fortawesome/free-solid-svg-icons'
import {
  CobraPrimaryButton,
  CobraSecondaryButton,
  CobraTextField,
} from '@/theme/styledComponents'
import { useLinkedInjects } from '../hooks/useCriticalTasks'
import { useInjects } from '@/features/injects/hooks'
import type { CriticalTaskDto } from '../types'
import type { InjectDto } from '@/features/injects/types'

interface LinkedInjectsDialogProps {
  /** Whether the dialog is open */
  open: boolean
  /** Exercise ID */
  exerciseId: string
  /** The critical task to manage inject links for */
  task: CriticalTaskDto | null
  /** Called when the dialog is closed */
  onClose: () => void
}

/**
 * Dialog for linking/unlinking injects to a critical task
 */
export const LinkedInjectsDialog: FC<LinkedInjectsDialogProps> = ({
  open,
  exerciseId,
  task,
  onClose,
}) => {
  const [searchQuery, setSearchQuery] = useState('')

  // Only fetch when dialog is open and we have a task
  const {
    linkedInjectIds,
    loading: linkedLoading,
    setLinkedInjects,
    isUpdating,
  } = useLinkedInjects(exerciseId, task?.id ?? '')

  const { injects = [], loading: injectsLoading } = useInjects(exerciseId)

  // Local state for optimistic updates
  const [localLinkedIds, setLocalLinkedIds] = useState<string[]>([])

  // Sync local state when linked data loads
  useEffect(() => {
    setLocalLinkedIds(linkedInjectIds)
  }, [linkedInjectIds])

  // Reset search on dialog open/close
  useEffect(() => {
    if (!open) {
      setSearchQuery('')
    }
  }, [open])

  // Split injects into linked and available
  const linkedInjects = useMemo(() => {
    return injects.filter((inj: InjectDto) => localLinkedIds.includes(inj.id))
  }, [injects, localLinkedIds])

  const availableInjects = useMemo(() => {
    const available = injects.filter((inj: InjectDto) => !localLinkedIds.includes(inj.id))
    if (!searchQuery.trim()) return available

    const query = searchQuery.toLowerCase()
    return available.filter(
      (inj: InjectDto) =>
        inj.title.toLowerCase().includes(query) ||
        String(inj.injectNumber).includes(query),
    )
  }, [injects, localLinkedIds, searchQuery])

  const handleLink = (injectId: string) => {
    setLocalLinkedIds(prev => [...prev, injectId])
  }

  const handleUnlink = (injectId: string) => {
    setLocalLinkedIds(prev => prev.filter(id => id !== injectId))
  }

  const handleSave = async () => {
    await setLinkedInjects(localLinkedIds)
    onClose()
  }

  const handleCancel = () => {
    setLocalLinkedIds(linkedInjectIds)
    onClose()
  }

  const hasChanges = useMemo(() => {
    if (localLinkedIds.length !== linkedInjectIds.length) return true
    return !localLinkedIds.every(id => linkedInjectIds.includes(id))
  }, [localLinkedIds, linkedInjectIds])

  const isLoading = linkedLoading || injectsLoading

  const formatInjectNumber = (num: number) =>
    `INJ-${String(num).padStart(3, '0')}`

  return (
    <Dialog open={open} onClose={handleCancel} maxWidth="sm" fullWidth>
      <DialogTitle>
        <Stack direction="row" justifyContent="space-between" alignItems="center">
          <Box>
            <Typography variant="h6">Linked Injects</Typography>
            {task && (
              <Typography variant="body2" color="text.secondary">
                Task: {task.taskDescription}
              </Typography>
            )}
          </Box>
          <IconButton onClick={handleCancel} size="small">
            <FontAwesomeIcon icon={faXmark} />
          </IconButton>
        </Stack>
      </DialogTitle>
      <DialogContent dividers>
        {isLoading ? (
          <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
            <CircularProgress />
          </Box>
        ) : (
          <Stack spacing={2}>
            {/* Currently Linked */}
            <Box>
              <Typography variant="subtitle2" gutterBottom>
                Injects testing this task
                <Chip
                  label={linkedInjects.length}
                  size="small"
                  sx={{ ml: 1, height: 20 }}
                />
              </Typography>
              {linkedInjects.length === 0 ? (
                <Alert severity="warning" sx={{ py: 0.5 }}>
                  No injects are linked to this task. Evaluators won&apos;t know
                  which injects test this capability.
                </Alert>
              ) : (
                <List dense disablePadding>
                  {linkedInjects.map((inject: InjectDto) => (
                    <ListItem
                      key={inject.id}
                      sx={{ px: 1, py: 0.5, bgcolor: 'grey.50', mb: 0.5, borderRadius: 1 }}
                      secondaryAction={
                        <IconButton
                          edge="end"
                          size="small"
                          onClick={() => handleUnlink(inject.id)}
                          color="error"
                          aria-label={`Unlink ${inject.title}`}
                        >
                          <FontAwesomeIcon icon={faLinkSlash} size="xs" />
                        </IconButton>
                      }
                    >
                      <ListItemText
                        primary={
                          <Stack direction="row" spacing={1} alignItems="center">
                            <Chip
                              label={formatInjectNumber(inject.injectNumber)}
                              size="small"
                              sx={{ height: 20, fontSize: '0.7rem', fontFamily: 'monospace' }}
                            />
                            <Typography variant="body2" noWrap>
                              {inject.title}
                            </Typography>
                          </Stack>
                        }
                      />
                    </ListItem>
                  ))}
                </List>
              )}
            </Box>

            <Divider />

            {/* Available Injects */}
            <Box>
              <Typography variant="subtitle2" gutterBottom>
                Available injects to link
              </Typography>
              <CobraTextField
                fullWidth
                size="small"
                placeholder="Search injects..."
                value={searchQuery}
                onChange={e => setSearchQuery(e.target.value)}
                slotProps={{
                  input: {
                    startAdornment: (
                      <FontAwesomeIcon
                        icon={faMagnifyingGlass}
                        style={{ marginRight: 8, color: '#999' }}
                      />
                    ),
                  },
                }}
                sx={{ mb: 1 }}
              />
              {availableInjects.length === 0 ? (
                <Typography variant="body2" color="text.secondary" sx={{ py: 1, textAlign: 'center' }}>
                  {searchQuery ? 'No matching injects found.' : 'All injects are already linked.'}
                </Typography>
              ) : (
                <List
                  dense
                  disablePadding
                  sx={{ maxHeight: 250, overflow: 'auto' }}
                >
                  {availableInjects.map((inject: InjectDto) => (
                    <ListItem
                      key={inject.id}
                      sx={{ px: 1, py: 0.5, mb: 0.5, borderRadius: 1 }}
                      secondaryAction={
                        <IconButton
                          edge="end"
                          size="small"
                          onClick={() => handleLink(inject.id)}
                          color="primary"
                          aria-label={`Link ${inject.title}`}
                        >
                          <FontAwesomeIcon icon={faLink} size="xs" />
                        </IconButton>
                      }
                    >
                      <ListItemText
                        primary={
                          <Stack direction="row" spacing={1} alignItems="center">
                            <Chip
                              label={formatInjectNumber(inject.injectNumber)}
                              size="small"
                              variant="outlined"
                              sx={{ height: 20, fontSize: '0.7rem', fontFamily: 'monospace' }}
                            />
                            <Typography variant="body2" noWrap>
                              {inject.title}
                            </Typography>
                          </Stack>
                        }
                      />
                    </ListItem>
                  ))}
                </List>
              )}
            </Box>
          </Stack>
        )}
      </DialogContent>
      <DialogActions>
        <CobraSecondaryButton onClick={handleCancel} disabled={isUpdating}>
          Cancel
        </CobraSecondaryButton>
        <CobraPrimaryButton
          onClick={handleSave}
          disabled={!hasChanges || isUpdating}
        >
          {isUpdating ? 'Saving...' : 'Save Changes'}
        </CobraPrimaryButton>
      </DialogActions>
    </Dialog>
  )
}

export default LinkedInjectsDialog
