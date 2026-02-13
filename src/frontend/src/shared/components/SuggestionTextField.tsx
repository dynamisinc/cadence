import { useState, useEffect, useRef, useCallback } from 'react'
import type { FC } from 'react'
import { Autocomplete, CircularProgress } from '@mui/material'
import { CobraTextField } from '@/theme/styledComponents'

export interface SuggestionTextFieldProps {
  label: string
  value: string
  onChange: (value: string) => void
  suggestions: string[]
  isLoading?: boolean
  onFilterChange?: (filter: string) => void
  error?: boolean
  helperText?: string
  required?: boolean
  fullWidth?: boolean
  placeholder?: string
  onBlur?: () => void
}

export const SuggestionTextField: FC<SuggestionTextFieldProps> = ({
  label,
  value,
  onChange,
  suggestions,
  isLoading = false,
  onFilterChange,
  error = false,
  helperText,
  required = false,
  fullWidth = false,
  placeholder,
  onBlur,
}) => {
  const [inputValue, setInputValue] = useState(value)
  const debounceRef = useRef<ReturnType<typeof setTimeout> | null>(null)

  // Sync inputValue when controlled value changes externally (e.g. edit mode init)
  useEffect(() => {
    setInputValue(value)
  }, [value])

  const handleFilterChange = useCallback(
    (filter: string) => {
      if (!onFilterChange) return
      if (debounceRef.current) clearTimeout(debounceRef.current)
      debounceRef.current = setTimeout(() => {
        onFilterChange(filter)
      }, 300)
    },
    [onFilterChange],
  )

  // Cleanup debounce on unmount
  useEffect(() => {
    return () => {
      if (debounceRef.current) clearTimeout(debounceRef.current)
    }
  }, [])

  return (
    <Autocomplete
      freeSolo
      options={suggestions}
      value={value}
      inputValue={inputValue}
      onInputChange={(_, newInputValue, reason) => {
        setInputValue(newInputValue)
        handleFilterChange(newInputValue)
        // For freeSolo, update the form value on every keystroke so the value
        // is captured even when the user doesn't select from the dropdown
        if (reason === 'input') {
          onChange(newInputValue)
        }
      }}
      onChange={(_, newValue) => {
        // Fired when user selects from dropdown or clears
        const val = typeof newValue === 'string' ? newValue : ''
        onChange(val)
      }}
      loading={isLoading}
      noOptionsText="No suggestions"
      size="small"
      renderInput={params => (
        <CobraTextField
          {...params}
          label={label}
          required={required}
          error={error}
          helperText={helperText}
          fullWidth={fullWidth}
          placeholder={placeholder}
          onBlur={onBlur}
          slotProps={{
            input: {
              ...params.InputProps,
              endAdornment: (
                <>
                  {isLoading ? <CircularProgress size={18} /> : null}
                  {params.InputProps.endAdornment}
                </>
              ),
            },
          }}
        />
      )}
    />
  )
}
