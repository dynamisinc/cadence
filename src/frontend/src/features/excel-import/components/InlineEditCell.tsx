/**
 * InlineEditCell Component
 *
 * Displays a cell value that can be clicked to edit inline.
 * Used in the validation table for fixing individual cell errors.
 */

import { useState, useRef, useEffect } from 'react'
import { Box } from '@mui/material'
import { CobraTextField } from '@/theme/styledComponents'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faPen } from '@fortawesome/free-solid-svg-icons'

interface InlineEditCellProps {
  /** Current cell value */
  value: string
  /** Cadence field name */
  field: string
  /** Row number (1-based) */
  rowNumber: number
  /** Whether this cell has a validation issue */
  hasIssue: boolean
  /** Severity of the issue */
  issueSeverity?: 'Error' | 'Warning'
  /** Called when user saves an edit */
  onSave: (rowNumber: number, field: string, newValue: string) => void
}

export const InlineEditCell = ({
  value,
  field,
  rowNumber,
  hasIssue,
  issueSeverity,
  onSave,
}: InlineEditCellProps) => {
  const [isEditing, setIsEditing] = useState(false)
  const [editValue, setEditValue] = useState(value)
  const inputRef = useRef<HTMLInputElement>(null)

  useEffect(() => {
    if (isEditing && inputRef.current) {
      inputRef.current.focus()
      inputRef.current.select()
    }
  }, [isEditing])

  // Sync value prop changes (e.g., after server re-validation)
  useEffect(() => {
    if (!isEditing) {
      setEditValue(value)
    }
  }, [value, isEditing])

  const handleClick = () => {
    if (hasIssue) {
      setIsEditing(true)
      setEditValue(value)
    }
  }

  const handleSave = () => {
    setIsEditing(false)
    if (editValue !== value) {
      onSave(rowNumber, field, editValue)
    }
  }

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter') {
      handleSave()
    } else if (e.key === 'Escape') {
      setIsEditing(false)
      setEditValue(value)
    }
  }

  const borderColor =
    issueSeverity === 'Error' ? 'error.main' : issueSeverity === 'Warning' ? 'warning.main' : undefined

  if (isEditing) {
    return (
      <CobraTextField
        inputRef={inputRef}
        value={editValue}
        onChange={e => setEditValue(e.target.value)}
        onBlur={handleSave}
        onKeyDown={handleKeyDown}
        size="small"
        variant="outlined"
        sx={{
          width: '100%',
          '& .MuiInputBase-root': {
            fontSize: '0.875rem',
            borderRadius: 1,
          },
          '& .MuiOutlinedInput-root': {
            '& fieldset': {
              borderColor: borderColor || 'primary.main',
            },
          },
          '& .MuiInputBase-input': {
            px: 1,
            py: 0.25,
          },
        }}
      />
    )
  }

  return (
    <Box
      onClick={handleClick}
      sx={{
        display: 'flex',
        alignItems: 'center',
        gap: 0.5,
        cursor: hasIssue ? 'pointer' : 'default',
        borderBottom: hasIssue ? 2 : 0,
        borderColor: borderColor,
        '&:hover .edit-icon': {
          opacity: hasIssue ? 1 : 0,
        },
      }}
    >
      <Box
        component="span"
        sx={{
          overflow: 'hidden',
          textOverflow: 'ellipsis',
          whiteSpace: 'nowrap',
        }}
      >
        {value || <em style={{ color: '#999' }}>empty</em>}
      </Box>
      {hasIssue && (
        <FontAwesomeIcon
          icon={faPen}
          className="edit-icon"
          style={{ fontSize: '0.7rem', opacity: 0, transition: 'opacity 0.15s' }}
        />
      )}
    </Box>
  )
}

export default InlineEditCell
