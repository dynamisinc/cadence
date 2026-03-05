/**
 * RoleOrientationPanel Tests
 */

import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, fireEvent } from '@testing-library/react'
import { ThemeProvider } from '@mui/material/styles'
import { RoleOrientationPanel } from './RoleOrientationPanel'
import { cobraTheme } from '../../../theme/cobraTheme'

const mockNavigate = vi.fn()
vi.mock('react-router-dom', () => ({
  useNavigate: () => mockNavigate,
}))

const renderWithTheme = (component: React.ReactElement) => {
  return render(
    <ThemeProvider theme={cobraTheme}>{component}</ThemeProvider>,
  )
}

describe('RoleOrientationPanel', () => {
  beforeEach(() => {
    localStorage.clear()
    mockNavigate.mockClear()
  })

  it('renders nothing when orgRole is undefined', () => {
    const { container } = renderWithTheme(
      <RoleOrientationPanel orgRole={undefined} orgName={undefined} />,
    )
    expect(container.firstChild).toBeNull()
  })

  it('renders OrgAdmin orientation with correct headline and cards', () => {
    renderWithTheme(
      <RoleOrientationPanel orgRole="OrgAdmin" orgName="Test Org" />,
    )

    expect(screen.getByText('You manage this organization')).toBeInTheDocument()
    expect(screen.getByText('Test Org')).toBeInTheDocument()
    expect(screen.getByText('Manage Members')).toBeInTheDocument()
    expect(screen.getByText('Create Exercise')).toBeInTheDocument()
    expect(screen.getByText('Organization Settings')).toBeInTheDocument()
    expect(screen.getByText('View Exercises')).toBeInTheDocument()
  })

  it('renders OrgManager orientation with correct cards', () => {
    renderWithTheme(
      <RoleOrientationPanel orgRole="OrgManager" orgName="My Org" />,
    )

    expect(screen.getByText('You coordinate exercises')).toBeInTheDocument()
    expect(screen.getByText('Create Exercise')).toBeInTheDocument()
    expect(screen.getByText('My Assignments')).toBeInTheDocument()
    expect(screen.getByText('View Exercises')).toBeInTheDocument()
    // OrgManager should not see Manage Members
    expect(screen.queryByText('Manage Members')).not.toBeInTheDocument()
  })

  it('renders OrgUser orientation with correct cards', () => {
    renderWithTheme(
      <RoleOrientationPanel orgRole="OrgUser" orgName="My Org" />,
    )

    expect(screen.getByText('You participate in exercises')).toBeInTheDocument()
    expect(screen.getByText('My Assignments')).toBeInTheDocument()
    expect(screen.getByText('View Exercises')).toBeInTheDocument()
    // OrgUser should not see Create Exercise
    expect(screen.queryByText('Create Exercise')).not.toBeInTheDocument()
  })

  it('navigates when a card is clicked', () => {
    renderWithTheme(
      <RoleOrientationPanel orgRole="OrgAdmin" orgName="Test" />,
    )

    fireEvent.click(screen.getByText('Manage Members'))
    expect(mockNavigate).toHaveBeenCalledWith('/organization/members')
  })

  it('can be dismissed and shows restore button', () => {
    renderWithTheme(
      <RoleOrientationPanel orgRole="OrgAdmin" orgName="Test" />,
    )

    expect(screen.getByText('You manage this organization')).toBeInTheDocument()

    fireEvent.click(screen.getByText('Dismiss'))

    expect(screen.queryByText('You manage this organization')).not.toBeInTheDocument()
    expect(screen.getByText('Show orientation guide')).toBeInTheDocument()
  })

  it('can be restored after dismissal', () => {
    renderWithTheme(
      <RoleOrientationPanel orgRole="OrgAdmin" orgName="Test" />,
    )

    fireEvent.click(screen.getByText('Dismiss'))
    fireEvent.click(screen.getByText('Show orientation guide'))

    expect(screen.getByText('You manage this organization')).toBeInTheDocument()
  })

  it('remembers dismissed state across renders', () => {
    localStorage.setItem('cadence:dismissed:home-orientation', 'true')

    renderWithTheme(
      <RoleOrientationPanel orgRole="OrgAdmin" orgName="Test" />,
    )

    expect(screen.queryByText('You manage this organization')).not.toBeInTheDocument()
    expect(screen.getByText('Show orientation guide')).toBeInTheDocument()
  })
})
