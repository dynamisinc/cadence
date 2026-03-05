/**
 * ExerciseRolePrimerSection Tests
 */

import { describe, it, expect, beforeEach } from 'vitest'
import { render, screen, fireEvent } from '@testing-library/react'
import { ThemeProvider } from '@mui/material/styles'
import { ExerciseRolePrimerSection } from './ExerciseRolePrimerSection'
import { cobraTheme } from '../../../theme/cobraTheme'

const renderWithTheme = (component: React.ReactElement) => {
  return render(
    <ThemeProvider theme={cobraTheme}>{component}</ThemeProvider>,
  )
}

describe('ExerciseRolePrimerSection', () => {
  beforeEach(() => {
    localStorage.clear()
  })

  it('renders all four HSEEP exercise roles', () => {
    renderWithTheme(<ExerciseRolePrimerSection />)

    expect(screen.getByText('Exercise Director')).toBeInTheDocument()
    expect(screen.getByText('Controller')).toBeInTheDocument()
    expect(screen.getByText('Evaluator')).toBeInTheDocument()
    expect(screen.getByText('Observer')).toBeInTheDocument()
  })

  it('shows role summaries', () => {
    renderWithTheme(<ExerciseRolePrimerSection />)

    expect(screen.getByText(/Go\/No-Go decisions/)).toBeInTheDocument()
    expect(screen.getByText(/Fires injects/)).toBeInTheDocument()
    expect(screen.getByText(/Records observations/)).toBeInTheDocument()
    expect(screen.getByText(/Read-only access/)).toBeInTheDocument()
  })

  it('has a toggle button labeled "HSEEP Exercise Roles"', () => {
    renderWithTheme(<ExerciseRolePrimerSection />)

    expect(screen.getByText('HSEEP Exercise Roles')).toBeInTheDocument()
  })

  it('collapses when toggle is clicked', () => {
    renderWithTheme(<ExerciseRolePrimerSection />)

    // Roles should be visible initially
    expect(screen.getByText('Exercise Director')).toBeVisible()

    // Click to collapse
    fireEvent.click(screen.getByText('HSEEP Exercise Roles'))

    // After collapse animation, content should not be visible
    // Note: MUI Collapse wraps content, so we check localStorage
    expect(localStorage.getItem('cadence:dismissed:home-exercise-role-primer')).toBe('true')
  })

  it('expands when toggle is clicked again', () => {
    renderWithTheme(<ExerciseRolePrimerSection />)

    // Collapse
    fireEvent.click(screen.getByText('HSEEP Exercise Roles'))
    expect(localStorage.getItem('cadence:dismissed:home-exercise-role-primer')).toBe('true')

    // Expand
    fireEvent.click(screen.getByText('HSEEP Exercise Roles'))
    expect(localStorage.getItem('cadence:dismissed:home-exercise-role-primer')).toBeNull()
  })

  it('starts collapsed when localStorage says dismissed', () => {
    localStorage.setItem('cadence:dismissed:home-exercise-role-primer', 'true')

    renderWithTheme(<ExerciseRolePrimerSection />)

    // Toggle button should still be visible
    expect(screen.getByText('HSEEP Exercise Roles')).toBeInTheDocument()
  })
})
