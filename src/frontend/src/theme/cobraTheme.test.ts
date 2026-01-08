import { describe, it, expect } from 'vitest'
import {
  cobraTheme,
  getProgressColor,
  getStatusChipColor,
} from './cobraTheme'

describe('cobraTheme', () => {
  describe('theme configuration', () => {
    it('has correct cssStyling values', () => {
      expect(cobraTheme.cssStyling.headerHeight).toBe(54)
      expect(cobraTheme.cssStyling.drawerClosedWidth).toBe(64)
      expect(cobraTheme.cssStyling.drawerOpenWidth).toBe(288)
    })

    it('has correct primary colors', () => {
      expect(cobraTheme.palette.primary.main).toBe('#c0c0c0')
      expect(cobraTheme.palette.primary.dark).toBe('#696969')
      expect(cobraTheme.palette.primary.light).toBe('#dadbdd')
    })

    it('has correct buttonPrimary colors (cobalt blue)', () => {
      expect(cobraTheme.palette.buttonPrimary.main).toBe('#0020c2')
      expect(cobraTheme.palette.buttonPrimary.contrastText).toBe('#ffffff')
      expect(cobraTheme.palette.buttonPrimary.dark).toBe('#00008b')
    })

    it('has correct buttonDelete colors (lava red)', () => {
      expect(cobraTheme.palette.buttonDelete.main).toBe('#e42217')
      expect(cobraTheme.palette.buttonDelete.contrastText).toBe('#ffffff')
    })

    it('has correct notification colors', () => {
      expect(cobraTheme.palette.notifications.success).toBe('#AEFBB8')
      expect(cobraTheme.palette.notifications.error).toBe('#EFB6B6')
      expect(cobraTheme.palette.notifications.warning).toBe('#F9F9BE')
    })

    it('has small size as default for form controls', () => {
      expect(
        cobraTheme.components?.MuiTextField?.defaultProps?.size,
      ).toBe('small')
      expect(
        cobraTheme.components?.MuiButton?.defaultProps?.size,
      ).toBe('small')
      expect(
        cobraTheme.components?.MuiSelect?.defaultProps?.size,
      ).toBe('small')
    })

    it('has Roboto as the font family', () => {
      expect(cobraTheme.typography.fontFamily).toContain('Roboto')
    })

    it('disables button text transform', () => {
      expect(cobraTheme.typography.button?.textTransform).toBe('none')
    })
  })

  describe('getProgressColor', () => {
    it('returns green for 100%', () => {
      expect(getProgressColor(100)).toBe(cobraTheme.palette.success.main)
    })

    it('returns cobalt blue for 67-99%', () => {
      expect(getProgressColor(67)).toBe(cobraTheme.palette.info.main)
      expect(getProgressColor(85)).toBe(cobraTheme.palette.info.main)
      expect(getProgressColor(99)).toBe(cobraTheme.palette.info.main)
    })

    it('returns yellow for 34-66%', () => {
      expect(getProgressColor(34)).toBe(cobraTheme.palette.statusChart.yellow)
      expect(getProgressColor(50)).toBe(cobraTheme.palette.statusChart.yellow)
      expect(getProgressColor(66)).toBe(cobraTheme.palette.statusChart.yellow)
    })

    it('returns red for 0-33%', () => {
      expect(getProgressColor(0)).toBe(cobraTheme.palette.error.main)
      expect(getProgressColor(15)).toBe(cobraTheme.palette.error.main)
      expect(getProgressColor(33)).toBe(cobraTheme.palette.error.main)
    })
  })

  describe('getStatusChipColor', () => {
    it('returns success colors for completed status', () => {
      const result = getStatusChipColor('completed')
      expect(result.bg).toBe(cobraTheme.palette.notifications.success)
      expect(result.text).toBe(cobraTheme.palette.text.primary)
    })

    it('returns success colors for complete status (case insensitive)', () => {
      expect(getStatusChipColor('Complete').bg).toBe(
        cobraTheme.palette.notifications.success,
      )
      expect(getStatusChipColor('COMPLETED').bg).toBe(
        cobraTheme.palette.notifications.success,
      )
    })

    it('returns grid colors for in progress status', () => {
      const result = getStatusChipColor('in progress')
      expect(result.bg).toBe(cobraTheme.palette.grid.main)
    })

    it('handles hyphenated status formats', () => {
      expect(getStatusChipColor('in-progress').bg).toBe(
        cobraTheme.palette.grid.main,
      )
      expect(getStatusChipColor('not-started').bg).toBe(
        cobraTheme.palette.primary.light,
      )
    })

    it('returns error colors for blocked status', () => {
      const result = getStatusChipColor('blocked')
      expect(result.bg).toBe(cobraTheme.palette.error.main)
      expect(result.text).toBe('#ffffff')
    })

    it('returns default colors for unknown status', () => {
      const result = getStatusChipColor('unknown-status')
      expect(result.bg).toBe(cobraTheme.palette.primary.main)
    })
  })
})
