/**
 * ExpectedOutcomesEvaluator Component
 *
 * Displays expected outcomes for evaluation during AAR phase.
 * Allows evaluators to mark outcomes as achieved/not achieved with notes.
 */

import { useState } from 'react'
import {
  Box,
  Stack,
  Typography,
  Chip,
  IconButton,
  List,
  ListItem,
  ListItemText,
  Collapse,
  Paper,
  Divider,
  ToggleButton,
  ToggleButtonGroup,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
  faChevronDown,
  faChevronUp,
  faCheck,
  faXmark,
  faQuestion,
  faPen,
  faSpinner,
} from '@fortawesome/free-solid-svg-icons'

import {
  CobraPrimaryButton,
  CobraSecondaryButton,
  CobraTextField,
} from '@/theme/styledComponents'
import {
  useExpectedOutcomes,
  useEvaluateExpectedOutcome,
} from '../hooks/useExpectedOutcomes'
import type { ExpectedOutcomeDto } from '../types'
import { getAchievementStatusLabel, getAchievementStatusColor } from '../types'

interface ExpectedOutcomesEvaluatorProps {
  /** Inject ID to manage outcomes for */
  injectId: string
  /** Default expanded state */
  defaultExpanded?: boolean
}

export const ExpectedOutcomesEvaluator = ({
  injectId,
  defaultExpanded = true,
}: ExpectedOutcomesEvaluatorProps) => {
  const [expanded, setExpanded] = useState(defaultExpanded)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [editNotes, setEditNotes] = useState('')
  const [editAchieved, setEditAchieved] = useState<boolean | null>(null)

  const { data: outcomes = [], isLoading } = useExpectedOutcomes(injectId)
  const evaluateMutation = useEvaluateExpectedOutcome(injectId)

  const handleStartEdit = (outcome: ExpectedOutcomeDto) => {
    setEditingId(outcome.id)
    setEditNotes(outcome.evaluatorNotes ?? '')
    setEditAchieved(outcome.wasAchieved)
  }

  const handleCancelEdit = () => {
    setEditingId(null)
    setEditNotes('')
    setEditAchieved(null)
  }

  const handleSaveEvaluation = async () => {
    if (!editingId) return

    try {
      await evaluateMutation.mutateAsync({
        id: editingId,
        request: {
          wasAchieved: editAchieved,
          evaluatorNotes: editNotes.trim() || undefined,
        },
      })
      setEditingId(null)
      setEditNotes('')
      setEditAchieved(null)
    } catch {
      // Error handled by mutation
    }
  }

  const handleQuickEvaluate = async (outcome: ExpectedOutcomeDto, wasAchieved: boolean) => {
    try {
      await evaluateMutation.mutateAsync({
        id: outcome.id,
        request: {
          wasAchieved,
          evaluatorNotes: outcome.evaluatorNotes,
        },
      })
    } catch {
      // Error handled by mutation
    }
  }

  const getAchievementIcon = (wasAchieved: boolean | null) => {
    if (wasAchieved === null) return faQuestion
    return wasAchieved ? faCheck : faXmark
  }

  const getAchievementColor = (wasAchieved: boolean | null) => {
    if (wasAchieved === null) return 'text.secondary'
    return wasAchieved ? 'success.main' : 'error.main'
  }

  // Calculate summary stats
  const achieved = outcomes.filter(o => o.wasAchieved === true).length
  const notAchieved = outcomes.filter(o => o.wasAchieved === false).length
  const notEvaluated = outcomes.filter(o => o.wasAchieved === null).length

  if (isLoading) {
    return (
      <Box sx={{ py: 2 }}>
        <Typography variant="body2" color="text.secondary">
          <FontAwesomeIcon icon={faSpinner} spin /> Loading outcomes...
        </Typography>
      </Box>
    )
  }

  if (outcomes.length === 0) {
    return null // Don't show evaluator if no outcomes defined
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
        <Stack direction="row" spacing={1} alignItems="center">
          <Typography variant="subtitle2">
            Expected Outcomes
          </Typography>
          {/* Summary chips */}
          {achieved > 0 && (
            <Chip
              size="small"
              label={achieved}
              color="success"
              sx={{ height: 20 }}
            />
          )}
          {notAchieved > 0 && (
            <Chip
              size="small"
              label={notAchieved}
              color="error"
              sx={{ height: 20 }}
            />
          )}
          {notEvaluated > 0 && (
            <Chip
              size="small"
              label={notEvaluated}
              variant="outlined"
              sx={{ height: 20 }}
            />
          )}
        </Stack>
        <IconButton size="small">
          <FontAwesomeIcon icon={expanded ? faChevronUp : faChevronDown} size="sm" />
        </IconButton>
      </Box>

      <Collapse in={expanded}>
        <Divider />

        {/* Outcomes List */}
        <List dense disablePadding>
          {outcomes.map((outcome, index) => (
            <ListItem
              key={outcome.id}
              sx={{
                borderBottom: index < outcomes.length - 1 ? '1px solid' : undefined,
                borderColor: 'divider',
                py: 1.5,
                flexDirection: 'column',
                alignItems: 'stretch',
              }}
            >
              {editingId === outcome.id ? (
                /* Edit Mode */
                <Stack spacing={1.5}>
                  <Typography variant="body2">{outcome.description}</Typography>

                  <ToggleButtonGroup
                    value={editAchieved}
                    exclusive
                    onChange={(_, value) => setEditAchieved(value)}
                    size="small"
                    fullWidth
                  >
                    <ToggleButton value={true} color="success">
                      <FontAwesomeIcon icon={faCheck} style={{ marginRight: 4 }} />
                      Achieved
                    </ToggleButton>
                    <ToggleButton value={null}>
                      <FontAwesomeIcon icon={faQuestion} style={{ marginRight: 4 }} />
                      Not Evaluated
                    </ToggleButton>
                    <ToggleButton value={false} color="error">
                      <FontAwesomeIcon icon={faXmark} style={{ marginRight: 4 }} />
                      Not Achieved
                    </ToggleButton>
                  </ToggleButtonGroup>

                  <CobraTextField
                    size="small"
                    value={editNotes}
                    onChange={e => setEditNotes(e.target.value)}
                    fullWidth
                    placeholder="Evaluator notes (optional)..."
                    multiline
                    rows={2}
                  />

                  <Stack direction="row" spacing={1} justifyContent="flex-end">
                    <CobraSecondaryButton
                      size="small"
                      onClick={handleCancelEdit}
                      disabled={evaluateMutation.isPending}
                    >
                      Cancel
                    </CobraSecondaryButton>
                    <CobraPrimaryButton
                      size="small"
                      onClick={handleSaveEvaluation}
                      disabled={evaluateMutation.isPending}
                      startIcon={
                        evaluateMutation.isPending ? (
                          <FontAwesomeIcon icon={faSpinner} spin />
                        ) : (
                          <FontAwesomeIcon icon={faCheck} />
                        )
                      }
                    >
                      Save
                    </CobraPrimaryButton>
                  </Stack>
                </Stack>
              ) : (
                /* View Mode */
                <Stack direction="row" spacing={1} alignItems="flex-start">
                  <Box
                    sx={{
                      color: getAchievementColor(outcome.wasAchieved),
                      pt: 0.25,
                    }}
                  >
                    <FontAwesomeIcon icon={getAchievementIcon(outcome.wasAchieved)} />
                  </Box>

                  <Box sx={{ flex: 1 }}>
                    <Typography variant="body2">{outcome.description}</Typography>
                    {outcome.evaluatorNotes && (
                      <Typography
                        variant="caption"
                        color="text.secondary"
                        sx={{ display: 'block', mt: 0.5, fontStyle: 'italic' }}
                      >
                        {outcome.evaluatorNotes}
                      </Typography>
                    )}
                  </Box>

                  <Stack direction="row" spacing={0.5}>
                    {/* Quick evaluation buttons */}
                    <IconButton
                      size="small"
                      color={outcome.wasAchieved === true ? 'success' : 'default'}
                      onClick={() => handleQuickEvaluate(outcome, true)}
                      disabled={evaluateMutation.isPending}
                      title="Mark as Achieved"
                    >
                      <FontAwesomeIcon icon={faCheck} size="xs" />
                    </IconButton>
                    <IconButton
                      size="small"
                      color={outcome.wasAchieved === false ? 'error' : 'default'}
                      onClick={() => handleQuickEvaluate(outcome, false)}
                      disabled={evaluateMutation.isPending}
                      title="Mark as Not Achieved"
                    >
                      <FontAwesomeIcon icon={faXmark} size="xs" />
                    </IconButton>
                    <IconButton
                      size="small"
                      onClick={() => handleStartEdit(outcome)}
                      title="Add notes"
                    >
                      <FontAwesomeIcon icon={faPen} size="xs" />
                    </IconButton>
                  </Stack>
                </Stack>
              )}
            </ListItem>
          ))}
        </List>
      </Collapse>
    </Paper>
  )
}

export default ExpectedOutcomesEvaluator
