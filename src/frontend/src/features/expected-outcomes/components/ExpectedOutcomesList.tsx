/**
 * ExpectedOutcomesList Component
 *
 * Displays and manages expected outcomes for an inject during planning.
 * Allows CRUD operations and reordering via drag-and-drop.
 */

import { useState } from 'react'
import {
  Box,
  Stack,
  Typography,
  IconButton,
  List,
  ListItem,
  ListItemText,
  Collapse,
  Paper,
  Divider,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
  faPlus,
  faGripVertical,
  faPen,
  faTrash,
  faChevronDown,
  faChevronUp,
  faCheck,
  faXmark,
  faSpinner,
} from '@fortawesome/free-solid-svg-icons'

import {
  CobraPrimaryButton,
  CobraSecondaryButton,
  CobraTextField,
  CobraDeleteButton,
} from '@/theme/styledComponents'
import {
  useExpectedOutcomes,
  useCreateExpectedOutcome,
  useUpdateExpectedOutcome,
  useDeleteExpectedOutcome,
} from '../hooks/useExpectedOutcomes'
import type { ExpectedOutcomeDto } from '../types'
import { EXPECTED_OUTCOME_FIELD_LIMITS } from '../types'

interface ExpectedOutcomesListProps {
  /** Inject ID to manage outcomes for */
  injectId: string
  /** Is the inject/exercise editable? */
  isEditable?: boolean
  /** Default expanded state */
  defaultExpanded?: boolean
}

export const ExpectedOutcomesList = ({
  injectId,
  isEditable = true,
  defaultExpanded = true,
}: ExpectedOutcomesListProps) => {
  const [expanded, setExpanded] = useState(defaultExpanded)
  const [isAdding, setIsAdding] = useState(false)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [newDescription, setNewDescription] = useState('')
  const [editDescription, setEditDescription] = useState('')

  const { data: outcomes = [], isLoading } = useExpectedOutcomes(injectId)
  const createMutation = useCreateExpectedOutcome(injectId)
  const updateMutation = useUpdateExpectedOutcome(injectId)
  const deleteMutation = useDeleteExpectedOutcome(injectId)

  const handleAdd = async () => {
    if (!newDescription.trim()) return

    try {
      await createMutation.mutateAsync({
        description: newDescription.trim(),
      })
      setNewDescription('')
      setIsAdding(false)
    } catch {
      // Error handled by mutation
    }
  }

  const handleStartEdit = (outcome: ExpectedOutcomeDto) => {
    setEditingId(outcome.id)
    setEditDescription(outcome.description)
  }

  const handleCancelEdit = () => {
    setEditingId(null)
    setEditDescription('')
  }

  const handleSaveEdit = async () => {
    if (!editingId || !editDescription.trim()) return

    try {
      await updateMutation.mutateAsync({
        id: editingId,
        request: { description: editDescription.trim() },
      })
      setEditingId(null)
      setEditDescription('')
    } catch {
      // Error handled by mutation
    }
  }

  const handleDelete = async (id: string) => {
    try {
      await deleteMutation.mutateAsync(id)
    } catch {
      // Error handled by mutation
    }
  }

  const isDescriptionValid = (desc: string) => {
    const trimmed = desc.trim()
    return (
      trimmed.length >= EXPECTED_OUTCOME_FIELD_LIMITS.description.min &&
      trimmed.length <= EXPECTED_OUTCOME_FIELD_LIMITS.description.max
    )
  }

  if (isLoading) {
    return (
      <Box sx={{ py: 2 }}>
        <Typography variant="body2" color="text.secondary">
          <FontAwesomeIcon icon={faSpinner} spin /> Loading outcomes...
        </Typography>
      </Box>
    )
  }

  return (
    <Paper variant="outlined" sx={{ mt: 2 }}>
      {/* Header */}
      <Box
        sx={{
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          px: 2,
          py: 1,
          cursor: 'pointer',
          '&:hover': { bgcolor: 'action.hover' },
        }}
        onClick={() => setExpanded(!expanded)}
      >
        <Typography variant="subtitle2">
          Expected Outcomes ({outcomes.length})
        </Typography>
        <IconButton size="small">
          <FontAwesomeIcon icon={expanded ? faChevronUp : faChevronDown} size="sm" />
        </IconButton>
      </Box>

      <Collapse in={expanded}>
        <Divider />

        {/* Outcomes List */}
        {outcomes.length > 0 && (
          <List dense disablePadding>
            {outcomes.map((outcome, index) => (
              <ListItem
                key={outcome.id}
                sx={{
                  borderBottom: index < outcomes.length - 1 ? '1px solid' : undefined,
                  borderColor: 'divider',
                  py: 1,
                }}
                secondaryAction={
                  isEditable && editingId !== outcome.id ? (
                    <Stack direction="row" spacing={0.5}>
                      <IconButton
                        size="small"
                        onClick={() => handleStartEdit(outcome)}
                        title="Edit"
                      >
                        <FontAwesomeIcon icon={faPen} size="xs" />
                      </IconButton>
                      <IconButton
                        size="small"
                        onClick={() => handleDelete(outcome.id)}
                        disabled={deleteMutation.isPending}
                        title="Delete"
                        color="error"
                      >
                        <FontAwesomeIcon icon={faTrash} size="xs" />
                      </IconButton>
                    </Stack>
                  ) : undefined
                }
              >
                {isEditable && (
                  <IconButton
                    size="small"
                    sx={{ mr: 1, cursor: 'grab', color: 'text.disabled' }}
                    disabled
                    title="Drag to reorder (coming soon)"
                  >
                    <FontAwesomeIcon icon={faGripVertical} size="xs" />
                  </IconButton>
                )}

                {editingId === outcome.id ? (
                  <Box sx={{ flex: 1, display: 'flex', alignItems: 'center', gap: 1 }}>
                    <CobraTextField
                      size="small"
                      value={editDescription}
                      onChange={e => setEditDescription(e.target.value)}
                      fullWidth
                      autoFocus
                      placeholder="Expected outcome description"
                      error={!isDescriptionValid(editDescription) && editDescription.length > 0}
                      helperText={
                        editDescription.length > EXPECTED_OUTCOME_FIELD_LIMITS.description.max
                          ? `${editDescription.length}/${EXPECTED_OUTCOME_FIELD_LIMITS.description.max}`
                          : undefined
                      }
                    />
                    <IconButton
                      size="small"
                      color="primary"
                      onClick={handleSaveEdit}
                      disabled={
                        !isDescriptionValid(editDescription) || updateMutation.isPending
                      }
                    >
                      <FontAwesomeIcon icon={faCheck} />
                    </IconButton>
                    <IconButton size="small" onClick={handleCancelEdit}>
                      <FontAwesomeIcon icon={faXmark} />
                    </IconButton>
                  </Box>
                ) : (
                  <ListItemText
                    primary={outcome.description}
                    primaryTypographyProps={{ variant: 'body2' }}
                  />
                )}
              </ListItem>
            ))}
          </List>
        )}

        {/* Empty State */}
        {outcomes.length === 0 && !isAdding && (
          <Box sx={{ py: 3, px: 2, textAlign: 'center' }}>
            <Typography variant="body2" color="text.secondary">
              No expected outcomes defined
            </Typography>
            {isEditable && (
              <Typography variant="caption" color="text.secondary">
                Add outcomes to track what should happen when this inject is delivered
              </Typography>
            )}
          </Box>
        )}

        {/* Add Form */}
        {isAdding && (
          <Box sx={{ p: 2, bgcolor: 'action.hover' }}>
            <Stack spacing={1}>
              <CobraTextField
                size="small"
                value={newDescription}
                onChange={e => setNewDescription(e.target.value)}
                fullWidth
                autoFocus
                placeholder="Describe an expected outcome..."
                multiline
                rows={2}
                error={!isDescriptionValid(newDescription) && newDescription.length > 0}
                helperText={
                  newDescription.length > EXPECTED_OUTCOME_FIELD_LIMITS.description.max
                    ? `${newDescription.length}/${EXPECTED_OUTCOME_FIELD_LIMITS.description.max}`
                    : undefined
                }
              />
              <Stack direction="row" spacing={1} justifyContent="flex-end">
                <CobraSecondaryButton
                  size="small"
                  onClick={() => {
                    setIsAdding(false)
                    setNewDescription('')
                  }}
                  disabled={createMutation.isPending}
                >
                  Cancel
                </CobraSecondaryButton>
                <CobraPrimaryButton
                  size="small"
                  onClick={handleAdd}
                  disabled={!isDescriptionValid(newDescription) || createMutation.isPending}
                  startIcon={
                    createMutation.isPending ? (
                      <FontAwesomeIcon icon={faSpinner} spin />
                    ) : (
                      <FontAwesomeIcon icon={faCheck} />
                    )
                  }
                >
                  Add
                </CobraPrimaryButton>
              </Stack>
            </Stack>
          </Box>
        )}

        {/* Add Button */}
        {isEditable && !isAdding && (
          <Box sx={{ p: 1 }}>
            <CobraSecondaryButton
              size="small"
              startIcon={<FontAwesomeIcon icon={faPlus} />}
              onClick={() => setIsAdding(true)}
              fullWidth
            >
              Add Expected Outcome
            </CobraSecondaryButton>
          </Box>
        )}
      </Collapse>
    </Paper>
  )
}

export default ExpectedOutcomesList
