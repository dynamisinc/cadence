/**
 * CriticalTaskSelector Component
 *
 * Multi-select field for linking critical tasks to an inject.
 * Shows tasks grouped by Capability Target for better organization.
 */

import { useMemo } from 'react'
import {
  Autocomplete,
  Chip,
  ListSubheader,
  Typography,
  Box,
  CircularProgress,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faLightbulb } from '@fortawesome/free-solid-svg-icons'
import { CobraTextField } from '@/theme/styledComponents'
import { useCapabilityTargets } from '../../eeg/hooks/useCapabilityTargets'
import { useCriticalTasksByExercise } from '../../eeg/hooks/useCriticalTasks'
import type { CriticalTaskDto, CapabilityTargetDto } from '../../eeg/types'

interface CriticalTaskOption {
  id: string
  taskDescription: string
  capabilityTargetId: string
  capabilityName: string
  targetDescription: string
  type: 'task'
}

interface GroupHeaderOption {
  id: string
  capabilityName: string
  targetDescription: string
  type: 'header'
}

type SelectorOption = CriticalTaskOption | GroupHeaderOption

interface CriticalTaskSelectorProps {
  /** Exercise ID for loading capability targets and tasks */
  exerciseId: string
  /** Currently selected critical task IDs */
  selectedTaskIds: string[]
  /** Called when selection changes */
  onChange: (taskIds: string[]) => void
  /** Whether the selector is disabled */
  disabled?: boolean
  /** Helper text to display below the field */
  helperText?: string
}

/**
 * Multi-select component for selecting critical tasks grouped by capability target.
 *
 * Per S05 story:
 * - Shows tasks grouped by Capability Target in selector
 * - Supports multi-select
 * - Displays selected tasks with capability context
 */
export const CriticalTaskSelector = ({
  exerciseId,
  selectedTaskIds,
  onChange,
  disabled = false,
  helperText = 'Critical tasks this inject is designed to test',
}: CriticalTaskSelectorProps) => {
  // Fetch capability targets
  const { capabilityTargets, loading: loadingTargets } = useCapabilityTargets(exerciseId)

  // Create a map to hold tasks by target
  const { tasksByTarget, allTasks, loading: loadingTasks } = useTasksByTarget(
    capabilityTargets,
    exerciseId,
  )

  const loading = loadingTargets || loadingTasks

  // Build flat list of options with group headers
  const options = useMemo<SelectorOption[]>(() => {
    const result: SelectorOption[] = []

    for (const target of capabilityTargets) {
      const tasks = tasksByTarget.get(target.id) ?? []
      if (tasks.length === 0) continue

      // Add group header
      result.push({
        id: `header-${target.id}`,
        capabilityName: target.capability.name,
        targetDescription: target.targetDescription,
        type: 'header',
      })

      // Add tasks
      for (const task of tasks) {
        result.push({
          id: task.id,
          taskDescription: task.taskDescription,
          capabilityTargetId: task.capabilityTargetId,
          capabilityName: target.capability.name,
          targetDescription: target.targetDescription,
          type: 'task',
        })
      }
    }

    return result
  }, [capabilityTargets, tasksByTarget])

  // Get selected task options
  const selectedOptions = useMemo(() => {
    return options.filter(
      (opt): opt is CriticalTaskOption =>
        opt.type === 'task' && selectedTaskIds.includes(opt.id),
    )
  }, [options, selectedTaskIds])

  // Only return task options (not headers) for filtering
  const taskOptions = useMemo(
    () => options.filter((opt): opt is CriticalTaskOption => opt.type === 'task'),
    [options],
  )

  const handleChange = (
    _: React.SyntheticEvent,
    newValue: CriticalTaskOption[],
  ) => {
    onChange(newValue.map(opt => opt.id))
  }

  if (allTasks.length === 0 && !loading) {
    return (
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, color: 'text.secondary' }}>
        <FontAwesomeIcon icon={faLightbulb} />
        <Typography variant="body2">
          No Critical Tasks defined. Set up the EEG in the EEG Setup tab first.
        </Typography>
      </Box>
    )
  }

  return (
    <Autocomplete
      multiple
      size="small"
      options={taskOptions}
      value={selectedOptions}
      onChange={handleChange}
      getOptionLabel={(option: CriticalTaskOption) => option.taskDescription}
      groupBy={(option: CriticalTaskOption) =>
        `${option.capabilityName}: ${option.targetDescription}`
      }
      isOptionEqualToValue={(option, value) => option.id === value.id}
      disabled={disabled}
      loading={loading}
      renderInput={params => (
        <CobraTextField
          {...params}
          label="Linked Critical Tasks"
          placeholder={selectedTaskIds.length === 0 ? 'Select tasks this inject tests...' : ''}
          helperText={helperText}
          slotProps={{
            input: {
              ...params.InputProps,
              endAdornment: (
                <>
                  {loading ? <CircularProgress color="inherit" size={20} /> : null}
                  {params.InputProps.endAdornment}
                </>
              ),
            },
          }}
        />
      )}
      renderGroup={params => (
        <li key={params.key}>
          <ListSubheader
            component="div"
            sx={{
              backgroundColor: 'background.paper',
              fontWeight: 600,
              fontSize: '0.75rem',
              color: 'text.secondary',
              lineHeight: '32px',
              borderBottom: 1,
              borderColor: 'divider',
            }}
          >
            {params.group}
          </ListSubheader>
          <ul style={{ padding: 0, margin: 0 }}>{params.children}</ul>
        </li>
      )}
      renderOption={(props, option) => {
        const { key, ...rest } = props
        return (
          <li key={key} {...rest}>
            <Box sx={{ display: 'flex', flexDirection: 'column', gap: 0.25 }}>
              <Typography variant="body2">{option.taskDescription}</Typography>
            </Box>
          </li>
        )
      }}
      renderTags={(value, getTagProps) =>
        value.map((option, index) => {
          const { key, ...tagProps } = getTagProps({ index })
          return (
            <Chip
              key={key}
              label={option.taskDescription}
              size="small"
              title={`${option.capabilityName}: ${option.targetDescription}`}
              {...tagProps}
            />
          )
        })
      }
      noOptionsText="No critical tasks defined for this exercise"
    />
  )
}

/**
 * Custom hook to fetch and organize tasks by capability target
 */
function useTasksByTarget(_capabilityTargets: CapabilityTargetDto[], exerciseId: string) {
  // Fetch all critical tasks for the exercise
  const { criticalTasks, loading } = useCriticalTasksByExercise(exerciseId)

  // Organize tasks by target
  const tasksByTarget = new Map<string, CriticalTaskDto[]>()
  const allTasks: CriticalTaskDto[] = []

  for (const task of criticalTasks) {
    allTasks.push(task)
    const targetTasks = tasksByTarget.get(task.capabilityTargetId) ?? []
    targetTasks.push(task)
    tasksByTarget.set(task.capabilityTargetId, targetTasks)
  }

  return { tasksByTarget, allTasks, loading }
}

export default CriticalTaskSelector
