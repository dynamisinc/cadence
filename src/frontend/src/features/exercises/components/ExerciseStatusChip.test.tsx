import { describe, it, expect } from 'vitest'
import { render, screen } from '@testing-library/react'
import { ThemeProvider } from '@mui/material/styles'
import { cobraTheme } from '../../../theme/cobraTheme'
import { ExerciseStatusChip } from './ExerciseStatusChip'
import { ExerciseStatus } from '../../../types'

const renderWithTheme = (ui: React.ReactElement) => {
  return render(<ThemeProvider theme={cobraTheme}>{ui}</ThemeProvider>)
}

describe('ExerciseStatusChip', () => {
  it('renders Draft status with correct label', () => {
    renderWithTheme(<ExerciseStatusChip status={ExerciseStatus.Draft} />)
    expect(screen.getByText('Draft')).toBeInTheDocument()
  })

  it('renders Active status with correct label', () => {
    renderWithTheme(<ExerciseStatusChip status={ExerciseStatus.Active} />)
    expect(screen.getByText('Active')).toBeInTheDocument()
  })

  it('renders Completed status with correct label', () => {
    renderWithTheme(<ExerciseStatusChip status={ExerciseStatus.Completed} />)
    expect(screen.getByText('Completed')).toBeInTheDocument()
  })

  it('renders Archived status with correct label', () => {
    renderWithTheme(<ExerciseStatusChip status={ExerciseStatus.Archived} />)
    expect(screen.getByText('Archived')).toBeInTheDocument()
  })

  it('applies correct COBRA styling for Active status (green)', () => {
    renderWithTheme(<ExerciseStatusChip status={ExerciseStatus.Active} />)
    const chip = screen.getByText('Active').closest('.MuiChip-root')
    expect(chip).toHaveStyle({
      backgroundColor: cobraTheme.palette.notifications.success,
    })
  })

  it('applies correct COBRA styling for Draft status (neutral)', () => {
    renderWithTheme(<ExerciseStatusChip status={ExerciseStatus.Draft} />)
    const chip = screen.getByText('Draft').closest('.MuiChip-root')
    expect(chip).toHaveStyle({
      backgroundColor: cobraTheme.palette.grid.main,
    })
  })
})
