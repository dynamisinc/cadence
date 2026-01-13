import { describe, it, expect } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { ThemeProvider } from '@mui/material/styles'
import { cobraTheme } from '../../../theme/cobraTheme'
import { ExerciseTypeChip } from './ExerciseTypeChip'
import { ExerciseType } from '../../../types'

const renderWithTheme = (ui: React.ReactElement) => {
  return render(<ThemeProvider theme={cobraTheme}>{ui}</ThemeProvider>)
}

describe('ExerciseTypeChip', () => {
  it('renders TTX abbreviation', () => {
    renderWithTheme(<ExerciseTypeChip type={ExerciseType.TTX} />)
    expect(screen.getByText('TTX')).toBeInTheDocument()
  })

  it('renders FSE abbreviation', () => {
    renderWithTheme(<ExerciseTypeChip type={ExerciseType.FSE} />)
    expect(screen.getByText('FSE')).toBeInTheDocument()
  })

  it('renders FE abbreviation', () => {
    renderWithTheme(<ExerciseTypeChip type={ExerciseType.FE} />)
    expect(screen.getByText('FE')).toBeInTheDocument()
  })

  it('renders CAX abbreviation', () => {
    renderWithTheme(<ExerciseTypeChip type={ExerciseType.CAX} />)
    expect(screen.getByText('CAX')).toBeInTheDocument()
  })

  it('renders Hybrid label', () => {
    renderWithTheme(<ExerciseTypeChip type={ExerciseType.Hybrid} />)
    expect(screen.getByText('Hybrid')).toBeInTheDocument()
  })

  it('shows full name tooltip on hover', async () => {
    const user = userEvent.setup()
    renderWithTheme(<ExerciseTypeChip type={ExerciseType.TTX} />)

    const chip = screen.getByText('TTX')
    await user.hover(chip)

    await waitFor(() => {
      expect(screen.getByRole('tooltip')).toHaveTextContent(
        'Tabletop Exercise',
      )
    })
  })

  it('shows FSE full name tooltip', async () => {
    const user = userEvent.setup()
    renderWithTheme(<ExerciseTypeChip type={ExerciseType.FSE} />)

    const chip = screen.getByText('FSE')
    await user.hover(chip)

    await waitFor(() => {
      expect(screen.getByRole('tooltip')).toHaveTextContent(
        'Full-Scale Exercise',
      )
    })
  })
})
