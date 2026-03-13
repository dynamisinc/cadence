import { describe, it, expect } from 'vitest'
import { screen } from '@testing-library/react'
import { render } from '../../../test/test-utils'
import { InjectStatusChip } from './InjectStatusChip'
import { InjectStatus } from '../../../types'

describe('InjectStatusChip', () => {
  describe('HSEEP Status Labels', () => {
    it.each([
      [InjectStatus.Draft, 'Draft'],
      [InjectStatus.Submitted, 'Submitted'],
      [InjectStatus.Approved, 'Approved'],
      [InjectStatus.Synchronized, 'Synchronized'],
      [InjectStatus.Released, 'Released'],
      [InjectStatus.Complete, 'Complete'],
      [InjectStatus.Deferred, 'Deferred'],
      [InjectStatus.Obsolete, 'Obsolete'],
    ])('renders %s status with correct label "%s"', (status, label) => {
      render(<InjectStatusChip status={status} />)
      expect(screen.getByText(label)).toBeInTheDocument()
    })
  })

  describe('HSEEP Status Colors', () => {
    it('applies gray styling for Draft status', () => {
      const { container } = render(<InjectStatusChip status={InjectStatus.Draft} />)
      const chip = container.querySelector('.MuiChip-root')
      expect(chip).toBeInTheDocument()
      // Gray background per HSEEP spec
      expect(chip).toHaveStyle({ backgroundColor: 'rgb(224, 224, 224)' })
    })

    it('applies amber/yellow styling for Submitted status', () => {
      const { container } = render(<InjectStatusChip status={InjectStatus.Submitted} />)
      const chip = container.querySelector('.MuiChip-root')
      expect(chip).toBeInTheDocument()
      // Amber background per HSEEP spec
      expect(chip).toHaveStyle({ backgroundColor: 'rgb(255, 224, 178)' })
    })

    it('applies green styling for Approved status', () => {
      const { container } = render(<InjectStatusChip status={InjectStatus.Approved} />)
      const chip = container.querySelector('.MuiChip-root')
      expect(chip).toBeInTheDocument()
      // Green background per HSEEP spec
      expect(chip).toHaveStyle({ backgroundColor: 'rgb(200, 230, 201)' })
    })

    it('applies blue styling for Synchronized status', () => {
      const { container } = render(<InjectStatusChip status={InjectStatus.Synchronized} />)
      const chip = container.querySelector('.MuiChip-root')
      expect(chip).toBeInTheDocument()
      // Blue background per HSEEP spec
      expect(chip).toHaveStyle({ backgroundColor: 'rgb(187, 222, 251)' })
    })

    it('applies purple styling for Released status', () => {
      const { container } = render(<InjectStatusChip status={InjectStatus.Released} />)
      const chip = container.querySelector('.MuiChip-root')
      expect(chip).toBeInTheDocument()
      // Purple background per HSEEP spec
      expect(chip).toHaveStyle({ backgroundColor: 'rgb(225, 190, 231)' })
    })

    it('applies dark green styling for Complete status', () => {
      const { container } = render(<InjectStatusChip status={InjectStatus.Complete} />)
      const chip = container.querySelector('.MuiChip-root')
      expect(chip).toBeInTheDocument()
      // Dark green background per HSEEP spec
      expect(chip).toHaveStyle({ backgroundColor: 'rgb(165, 214, 167)' })
    })

    it('applies orange styling for Deferred status', () => {
      const { container } = render(<InjectStatusChip status={InjectStatus.Deferred} />)
      const chip = container.querySelector('.MuiChip-root')
      expect(chip).toBeInTheDocument()
      // Orange background per HSEEP spec
      expect(chip).toHaveStyle({ backgroundColor: 'rgb(255, 204, 128)' })
    })

    it('applies light gray styling for Obsolete status', () => {
      const { container } = render(<InjectStatusChip status={InjectStatus.Obsolete} />)
      const chip = container.querySelector('.MuiChip-root')
      expect(chip).toBeInTheDocument()
      // Light gray background per HSEEP spec
      expect(chip).toHaveStyle({ backgroundColor: 'rgb(245, 245, 245)' })
    })
  })

  describe('Icons', () => {
    it('renders icon for each status', () => {
      const statuses = [
        InjectStatus.Draft,
        InjectStatus.Submitted,
        InjectStatus.Approved,
        InjectStatus.Synchronized,
        InjectStatus.Released,
        InjectStatus.Complete,
        InjectStatus.Deferred,
        InjectStatus.Obsolete,
      ]

      statuses.forEach(status => {
        const { container } = render(<InjectStatusChip status={status} />)
        // FontAwesome icons render as SVG elements
        const icon = container.querySelector('svg')
        expect(icon).toBeInTheDocument()
      })
    })
  })

  describe('Size prop', () => {
    it('renders small size by default', () => {
      const { container } = render(<InjectStatusChip status={InjectStatus.Draft} />)
      const chip = container.querySelector('.MuiChip-sizeSmall')
      expect(chip).toBeInTheDocument()
    })

    it('renders medium size when specified', () => {
      const { container } = render(<InjectStatusChip status={InjectStatus.Draft} size="medium" />)
      const chip = container.querySelector('.MuiChip-sizeMedium')
      expect(chip).toBeInTheDocument()
    })
  })
})
