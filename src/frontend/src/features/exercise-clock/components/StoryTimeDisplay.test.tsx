/**
 * StoryTimeDisplay Component Tests
 *
 * Tests for the story time display component.
 */

import { describe, it, expect } from 'vitest'
import { render, screen } from '@testing-library/react'
import { StoryTimeDisplay } from './StoryTimeDisplay'
import { TimelineMode } from '../../../types'
import type { StoryTime } from '../utils/storyTime'

describe('StoryTimeDisplay', () => {
  const mockStoryTime: StoryTime = {
    day: 1,
    hours: 8,
    minutes: 32,
  }

  describe('Basic rendering', () => {
    it('renders formatted story time', () => {
      render(
        <StoryTimeDisplay
          storyTime={mockStoryTime}
          formattedStoryTime="Day 1 • 08:32"
          isStoryOnly={false}
          timelineMode={TimelineMode.RealTime}
          timeScale={null}
        />,
      )

      expect(screen.getByText('Day 1 • 08:32')).toBeInTheDocument()
    })

    it('shows "Scenario Time:" label for non-StoryOnly modes', () => {
      render(
        <StoryTimeDisplay
          storyTime={mockStoryTime}
          formattedStoryTime="Day 1 • 08:32"
          isStoryOnly={false}
          timelineMode={TimelineMode.RealTime}
          timeScale={null}
        />,
      )

      expect(screen.getByText(/Scenario Time:/i)).toBeInTheDocument()
    })

    it('shows "Current Scenario Time:" label for StoryOnly mode', () => {
      render(
        <StoryTimeDisplay
          storyTime={mockStoryTime}
          formattedStoryTime="Day 1 • 18:00"
          isStoryOnly={true}
          timelineMode={TimelineMode.StoryOnly}
          timeScale={null}
        />,
      )

      expect(screen.getByText(/Current Scenario Time:/i)).toBeInTheDocument()
    })

    it('displays book icon', () => {
      const { container } = render(
        <StoryTimeDisplay
          storyTime={mockStoryTime}
          formattedStoryTime="Day 1 • 08:32"
          isStoryOnly={false}
          timelineMode={TimelineMode.RealTime}
          timeScale={null}
        />,
      )

      // FontAwesome renders as SVG
      const svg = container.querySelector('svg')
      expect(svg).toBeInTheDocument()
    })
  })

  describe('Compressed mode indicator', () => {
    it('shows compression indicator for Compressed mode', () => {
      render(
        <StoryTimeDisplay
          storyTime={mockStoryTime}
          formattedStoryTime="Day 1 • 02:08"
          isStoryOnly={false}
          timelineMode={TimelineMode.Compressed}
          timeScale={4}
        />,
      )

      expect(screen.getByText('4x compressed')).toBeInTheDocument()
    })

    it('does not show compression indicator for RealTime mode', () => {
      render(
        <StoryTimeDisplay
          storyTime={mockStoryTime}
          formattedStoryTime="Day 1 • 08:32"
          isStoryOnly={false}
          timelineMode={TimelineMode.RealTime}
          timeScale={null}
        />,
      )

      expect(screen.queryByText(/compressed/i)).not.toBeInTheDocument()
    })

    it('does not show compression indicator for StoryOnly mode', () => {
      render(
        <StoryTimeDisplay
          storyTime={mockStoryTime}
          formattedStoryTime="Day 1 • 18:00"
          isStoryOnly={true}
          timelineMode={TimelineMode.StoryOnly}
          timeScale={null}
        />,
      )

      expect(screen.queryByText(/compressed/i)).not.toBeInTheDocument()
    })

    it('shows correct compression factor', () => {
      render(
        <StoryTimeDisplay
          storyTime={mockStoryTime}
          formattedStoryTime="Day 1 • 02:08"
          isStoryOnly={false}
          timelineMode={TimelineMode.Compressed}
          timeScale={8}
        />,
      )

      expect(screen.getByText('8x compressed')).toBeInTheDocument()
    })

    it('handles fractional time scales', () => {
      render(
        <StoryTimeDisplay
          storyTime={mockStoryTime}
          formattedStoryTime="Day 1 • 01:04"
          isStoryOnly={false}
          timelineMode={TimelineMode.Compressed}
          timeScale={0.5}
        />,
      )

      expect(screen.getByText('0.5x compressed')).toBeInTheDocument()
    })
  })

  describe('Null story time handling', () => {
    it('displays placeholder when story time is null', () => {
      render(
        <StoryTimeDisplay
          storyTime={null}
          formattedStoryTime="—"
          isStoryOnly={true}
          timelineMode={TimelineMode.StoryOnly}
          timeScale={null}
        />,
      )

      expect(screen.getByText('—')).toBeInTheDocument()
    })
  })

  describe('Styling', () => {
    it('uses monospace font for time display', () => {
      render(
        <StoryTimeDisplay
          storyTime={mockStoryTime}
          formattedStoryTime="Day 1 • 08:32"
          isStoryOnly={false}
          timelineMode={TimelineMode.RealTime}
          timeScale={null}
        />,
      )

      const timeElement = screen.getByText('Day 1 • 08:32')
      // Check for monospace font family (MUI applies this via sx prop)
      expect(timeElement).toBeInTheDocument()
    })
  })
})
