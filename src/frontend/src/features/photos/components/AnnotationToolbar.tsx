/**
 * AnnotationToolbar - Tool selection and actions for photo annotation
 *
 * Bottom-anchored toolbar providing drawing tool selection (circle, arrow, text)
 * and annotation actions (undo, cancel, done). Active tool is visually highlighted.
 *
 * @module features/photos
 */

import type { FC } from 'react'
import { Box, Divider } from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
  faCircle,
  faArrowRight,
  faFont,
  faRotateLeft,
  faXmark,
  faCheck,
} from '@fortawesome/free-solid-svg-icons'
import { CobraIconButton } from '@/theme/styledComponents'
import type { AnnotationTool } from '../types/annotations'

interface AnnotationToolbarProps {
  /** Currently active drawing tool */
  activeTool: AnnotationTool | null
  /** Called when user selects a drawing tool */
  onToolChange: (tool: AnnotationTool | null) => void
  /** Called when user taps undo button */
  onUndo: () => void
  /** Called when user taps done button */
  onDone: () => void
  /** Called when user taps cancel button */
  onCancel: () => void
  /** Whether undo button should be enabled */
  canUndo: boolean
}

/**
 * Renders the annotation toolbar with tool selection and action buttons
 */
export const AnnotationToolbar: FC<AnnotationToolbarProps> = ({
  activeTool,
  onToolChange,
  onUndo,
  onDone,
  onCancel,
  canUndo,
}) => {
  const handleToolClick = (tool: AnnotationTool) => {
    // Toggle tool off if already active, otherwise activate it
    onToolChange(activeTool === tool ? null : tool)
  }

  return (
    <Box
      sx={{
        position: 'fixed',
        bottom: 0,
        left: 0,
        right: 0,
        backgroundColor: 'background.paper',
        borderTop: 1,
        borderColor: 'divider',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        gap: 1,
        p: 2,
        zIndex: 1300, // Above dialog content
      }}
    >
      {/* Drawing tools */}
      <CobraIconButton
        onClick={() => handleToolClick('circle')}
        aria-label="Circle tool"
        sx={{
          backgroundColor: activeTool === 'circle' ? 'primary.main' : 'transparent',
          color: activeTool === 'circle' ? 'primary.contrastText' : 'text.primary',
          '&:hover': {
            backgroundColor: activeTool === 'circle' ? 'primary.dark' : 'action.hover',
          },
        }}
      >
        <FontAwesomeIcon icon={faCircle} />
      </CobraIconButton>

      <CobraIconButton
        onClick={() => handleToolClick('arrow')}
        aria-label="Arrow tool"
        sx={{
          backgroundColor: activeTool === 'arrow' ? 'primary.main' : 'transparent',
          color: activeTool === 'arrow' ? 'primary.contrastText' : 'text.primary',
          '&:hover': {
            backgroundColor: activeTool === 'arrow' ? 'primary.dark' : 'action.hover',
          },
        }}
      >
        <FontAwesomeIcon icon={faArrowRight} />
      </CobraIconButton>

      <CobraIconButton
        onClick={() => handleToolClick('text')}
        aria-label="Text tool"
        sx={{
          backgroundColor: activeTool === 'text' ? 'primary.main' : 'transparent',
          color: activeTool === 'text' ? 'primary.contrastText' : 'text.primary',
          '&:hover': {
            backgroundColor: activeTool === 'text' ? 'primary.dark' : 'action.hover',
          },
        }}
      >
        <FontAwesomeIcon icon={faFont} />
      </CobraIconButton>

      {/* Divider */}
      <Divider orientation="vertical" flexItem sx={{ mx: 1 }} />

      {/* Action buttons */}
      <CobraIconButton
        onClick={onUndo}
        disabled={!canUndo}
        aria-label="Undo annotation"
      >
        <FontAwesomeIcon icon={faRotateLeft} />
      </CobraIconButton>

      <CobraIconButton
        onClick={onCancel}
        aria-label="Cancel annotation"
      >
        <FontAwesomeIcon icon={faXmark} />
      </CobraIconButton>

      <CobraIconButton
        onClick={onDone}
        aria-label="Done annotating"
        sx={{
          color: 'success.main',
          '&:hover': {
            backgroundColor: 'success.light',
            color: 'success.contrastText',
          },
        }}
      >
        <FontAwesomeIcon icon={faCheck} />
      </CobraIconButton>
    </Box>
  )
}
