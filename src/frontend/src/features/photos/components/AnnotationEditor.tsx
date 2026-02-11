/**
 * AnnotationEditor - Full-screen photo annotation interface
 *
 * Provides interactive drawing tools (circle, arrow, text) on top of a photo.
 * Uses Konva.js for canvas-based annotation rendering with touch support.
 * All coordinates stored in relative (0-1) format for resolution independence.
 *
 * @module features/photos
 */

import { useState, useRef, useEffect, useCallback } from 'react'
import type { FC, KeyboardEvent } from 'react'
import { Dialog, Box } from '@mui/material'
import { Stage, Layer, Ellipse, Arrow, Text, Rect } from 'react-konva'
import type { KonvaEventObject } from 'konva/lib/Node'
import { AnnotationToolbar } from './AnnotationToolbar'
import type {
  Annotation,
  AnnotationTool,
  CircleAnnotation,
  ArrowAnnotation,
  TextAnnotation,
} from '../types/annotations'
import { ANNOTATION_STYLE } from '../types/annotations'

interface AnnotationEditorProps {
  /** Whether the editor dialog is open */
  open: boolean
  /** URL of the photo to annotate */
  photoUrl: string
  /** Existing annotations to display */
  existingAnnotations: Annotation[]
  /** Called when user saves annotations */
  onSave: (annotations: Annotation[]) => void
  /** Called when user cancels editing */
  onCancel: () => void
}

interface Point {
  x: number
  y: number
}

interface TextInputState {
  visible: boolean
  x: number
  y: number
  value: string
}

/**
 * Converts pixel coordinates to relative (0-1) coordinates
 */
const pixelToRelative = (
  px: number,
  py: number,
  stageWidth: number,
  stageHeight: number,
): [number, number] => {
  return [px / stageWidth, py / stageHeight]
}

/**
 * Converts relative (0-1) coordinates to pixel coordinates
 */
const relativeToPixel = (
  rx: number,
  ry: number,
  stageWidth: number,
  stageHeight: number,
): [number, number] => {
  return [rx * stageWidth, ry * stageHeight]
}

/**
 * Full-screen annotation editor with drawing tools
 */
export const AnnotationEditor: FC<AnnotationEditorProps> = ({
  open,
  photoUrl,
  existingAnnotations,
  onSave,
  onCancel,
}) => {
  const [activeTool, setActiveTool] = useState<AnnotationTool | null>(null)
  const [annotations, setAnnotations] = useState<Annotation[]>(existingAnnotations)
  const [undoStack, setUndoStack] = useState<Annotation[][]>([])
  const [stageDimensions, setStageDimensions] = useState({ width: 0, height: 0 })
  const [stagePosition, setStagePosition] = useState({ top: 0, left: 0 })
  const [textInput, setTextInput] = useState<TextInputState>({
    visible: false,
    x: 0,
    y: 0,
    value: '',
  })

  // Drawing state
  const isDrawing = useRef(false)
  const drawStart = useRef<Point | null>(null)
  const [previewShape, setPreviewShape] = useState<{
    type: 'circle' | 'arrow'
    start: Point
    current: Point
  } | null>(null)

  // Refs for DOM elements
  const imageContainerRef = useRef<HTMLDivElement>(null)
  // Callback ref: triggers re-render when the Dialog portal mounts the img
  const [imageNode, setImageNode] = useState<HTMLImageElement | null>(null)
  const imageRef = useCallback((node: HTMLImageElement | null) => {
    setImageNode(node)
  }, [])

  // Initialize annotations from props
  useEffect(() => {
    setAnnotations(existingAnnotations)
    setUndoStack([])
  }, [existingAnnotations])

  // Update stage dimensions when image loads or resizes.
  // Uses ResizeObserver to reliably detect when the image has layout
  // dimensions inside the Dialog portal, avoiding race conditions with
  // img.complete and the load event.
  useEffect(() => {
    if (!open || !imageNode) return

    const updateDimensions = () => {
      const { offsetWidth, offsetHeight, offsetTop, offsetLeft } = imageNode
      if (offsetWidth > 0 && offsetHeight > 0) {
        setStageDimensions({ width: offsetWidth, height: offsetHeight })
        setStagePosition({ top: offsetTop, left: offsetLeft })
      }
    }

    // ResizeObserver fires reliably when the element first gets layout
    // dimensions, and again on any resize (window or container).
    const observer = new ResizeObserver(updateDimensions)
    observer.observe(imageNode)

    // Window resize can change the image position without changing its size
    window.addEventListener('resize', updateDimensions)

    return () => {
      observer.disconnect()
      window.removeEventListener('resize', updateDimensions)
    }
  }, [open, photoUrl, imageNode])

  /**
   * Pushes current state to undo stack before making changes
   */
  const pushUndo = () => {
    setUndoStack(prev => [...prev, annotations])
  }

  /**
   * Handles undo action - restores previous annotation state
   */
  const handleUndo = () => {
    if (undoStack.length === 0) return

    const previousState = undoStack[undoStack.length - 1]
    setAnnotations(previousState)
    setUndoStack(prev => prev.slice(0, -1))
  }

  /**
   * Handles pointer down - starts drawing shape
   */
  const handlePointerDown = (e: KonvaEventObject<PointerEvent>) => {
    if (!activeTool || activeTool === 'text') return

    const stage = e.target.getStage()
    if (!stage) return

    const pos = stage.getPointerPosition()
    if (!pos) return

    isDrawing.current = true
    drawStart.current = pos

    setPreviewShape({
      type: activeTool,
      start: pos,
      current: pos,
    })
  }

  /**
   * Handles pointer move - updates preview shape
   */
  const handlePointerMove = (e: KonvaEventObject<PointerEvent>) => {
    if (!isDrawing.current || !drawStart.current || !activeTool || activeTool === 'text') return

    const stage = e.target.getStage()
    if (!stage) return

    const pos = stage.getPointerPosition()
    if (!pos) return

    setPreviewShape({
      type: activeTool,
      start: drawStart.current,
      current: pos,
    })
  }

  /**
   * Handles pointer up - finalizes annotation
   */
  const handlePointerUp = (e: KonvaEventObject<PointerEvent>) => {
    if (!isDrawing.current || !drawStart.current || !activeTool || activeTool === 'text') {
      isDrawing.current = false
      return
    }

    const stage = e.target.getStage()
    if (!stage) return

    const pos = stage.getPointerPosition()
    if (!pos) return

    pushUndo()

    if (activeTool === 'circle') {
      // Calculate center and radii for ellipse
      const centerX = (drawStart.current.x + pos.x) / 2
      const centerY = (drawStart.current.y + pos.y) / 2
      const radiusX = Math.abs(pos.x - drawStart.current.x) / 2
      const radiusY = Math.abs(pos.y - drawStart.current.y) / 2

      const [cx, cy] = pixelToRelative(centerX, centerY, stageDimensions.width, stageDimensions.height)
      const [rx, ry] = pixelToRelative(radiusX, radiusY, stageDimensions.width, stageDimensions.height)

      const newAnnotation: CircleAnnotation = {
        type: 'circle',
        id: crypto.randomUUID(),
        cx,
        cy,
        rx,
        ry,
      }

      setAnnotations(prev => [...prev, newAnnotation])
    } else if (activeTool === 'arrow') {
      const [x1, y1] = pixelToRelative(
        drawStart.current.x,
        drawStart.current.y,
        stageDimensions.width,
        stageDimensions.height,
      )
      const [x2, y2] = pixelToRelative(pos.x, pos.y, stageDimensions.width, stageDimensions.height)

      const newAnnotation: ArrowAnnotation = {
        type: 'arrow',
        id: crypto.randomUUID(),
        x1,
        y1,
        x2,
        y2,
      }

      setAnnotations(prev => [...prev, newAnnotation])
    }

    // Reset drawing state
    isDrawing.current = false
    drawStart.current = null
    setPreviewShape(null)
  }

  /**
   * Handles stage click for text tool - shows text input
   */
  const handleStageClick = (e: KonvaEventObject<MouseEvent>) => {
    if (activeTool !== 'text') return

    const stage = e.target.getStage()
    if (!stage) return

    // Only trigger on stage background, not on existing annotations
    if (e.target !== stage) return

    const pos = stage.getPointerPosition()
    if (!pos) return

    setTextInput({
      visible: true,
      x: pos.x,
      y: pos.y,
      value: '',
    })
  }

  /**
   * Handles text input submission
   */
  const handleTextSubmit = () => {
    if (!textInput.value.trim()) {
      setTextInput({ visible: false, x: 0, y: 0, value: '' })
      return
    }

    pushUndo()

    const [x, y] = pixelToRelative(textInput.x, textInput.y, stageDimensions.width, stageDimensions.height)

    const newAnnotation: TextAnnotation = {
      type: 'text',
      id: crypto.randomUUID(),
      x,
      y,
      content: textInput.value.slice(0, 100), // Max 100 characters
    }

    setAnnotations(prev => [...prev, newAnnotation])
    setTextInput({ visible: false, x: 0, y: 0, value: '' })
  }

  /**
   * Handles text input key events
   */
  const handleTextKeyDown = (e: KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'Enter') {
      e.preventDefault()
      handleTextSubmit()
    } else if (e.key === 'Escape') {
      setTextInput({ visible: false, x: 0, y: 0, value: '' })
    }
  }

  /**
   * Handles done button - saves annotations
   */
  const handleDone = () => {
    onSave(annotations)
  }

  /**
   * Handles cancel button - discards changes
   */
  const handleCancel = () => {
    setAnnotations(existingAnnotations)
    setUndoStack([])
    setActiveTool(null)
    setTextInput({ visible: false, x: 0, y: 0, value: '' })
    onCancel()
  }

  return (
    <Dialog open={open} fullScreen>
      <Box
        ref={imageContainerRef}
        sx={{
          position: 'relative',
          width: '100%',
          height: '100%',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          backgroundColor: 'rgba(0, 0, 0, 0.9)',
          pb: 8, // Space for toolbar
        }}
      >
        {/* Photo background */}
        <img
          ref={imageRef}
          src={photoUrl}
          alt="Photo to annotate"
          draggable={false}
          style={{
            maxWidth: '100%',
            maxHeight: '100%',
            objectFit: 'contain',
            userSelect: 'none',
            pointerEvents: 'none',
          }}
        />

        {/* Konva canvas overlay - positioned to match the image */}
        {stageDimensions.width > 0 && stageDimensions.height > 0 && (
          <Box
            sx={{
              position: 'absolute',
              top: stagePosition.top,
              left: stagePosition.left,
              pointerEvents: 'all',
            }}
          >
            <Stage
              width={stageDimensions.width}
              height={stageDimensions.height}
              onPointerDown={handlePointerDown}
              onPointerMove={handlePointerMove}
              onPointerUp={handlePointerUp}
              onClick={handleStageClick}
            >
              <Layer>
                {/* Render existing annotations */}
                {annotations.map(annotation => {
                  if (annotation.type === 'circle') {
                    const [cx, cy] = relativeToPixel(
                      annotation.cx,
                      annotation.cy,
                      stageDimensions.width,
                      stageDimensions.height,
                    )
                    const [rx, ry] = relativeToPixel(
                      annotation.rx,
                      annotation.ry,
                      stageDimensions.width,
                      stageDimensions.height,
                    )

                    return (
                      <Ellipse
                        key={annotation.id}
                        x={cx}
                        y={cy}
                        radiusX={rx}
                        radiusY={ry}
                        stroke={ANNOTATION_STYLE.strokeColor}
                        strokeWidth={ANNOTATION_STYLE.strokeWidth}
                      />
                    )
                  } else if (annotation.type === 'arrow') {
                    const [x1, y1] = relativeToPixel(
                      annotation.x1,
                      annotation.y1,
                      stageDimensions.width,
                      stageDimensions.height,
                    )
                    const [x2, y2] = relativeToPixel(
                      annotation.x2,
                      annotation.y2,
                      stageDimensions.width,
                      stageDimensions.height,
                    )

                    return (
                      <Arrow
                        key={annotation.id}
                        points={[x1, y1, x2, y2]}
                        stroke={ANNOTATION_STYLE.strokeColor}
                        fill={ANNOTATION_STYLE.strokeColor}
                        strokeWidth={ANNOTATION_STYLE.strokeWidth}
                        pointerLength={10}
                        pointerWidth={10}
                      />
                    )
                  } else if (annotation.type === 'text') {
                    const [x, y] = relativeToPixel(
                      annotation.x,
                      annotation.y,
                      stageDimensions.width,
                      stageDimensions.height,
                    )

                    return (
                      <>
                        <Rect
                          key={`${annotation.id}-bg`}
                          x={x}
                          y={y - ANNOTATION_STYLE.textFontSize}
                          width={annotation.content.length * ANNOTATION_STYLE.textFontSize * 0.6}
                          height={ANNOTATION_STYLE.textFontSize + 8}
                          fill={ANNOTATION_STYLE.textBgColor}
                        />
                        <Text
                          key={annotation.id}
                          x={x}
                          y={y - ANNOTATION_STYLE.textFontSize + 4}
                          text={annotation.content}
                          fontSize={ANNOTATION_STYLE.textFontSize}
                          fill={ANNOTATION_STYLE.strokeColor}
                          fontFamily="Arial"
                        />
                      </>
                    )
                  }
                  return null
                })}

                {/* Render preview shape while drawing */}
                {previewShape && (
                  <>
                    {previewShape.type === 'circle' && (
                      <Ellipse
                        x={(previewShape.start.x + previewShape.current.x) / 2}
                        y={(previewShape.start.y + previewShape.current.y) / 2}
                        radiusX={Math.abs(previewShape.current.x - previewShape.start.x) / 2}
                        radiusY={Math.abs(previewShape.current.y - previewShape.start.y) / 2}
                        stroke={ANNOTATION_STYLE.strokeColor}
                        strokeWidth={ANNOTATION_STYLE.strokeWidth}
                        opacity={0.6}
                      />
                    )}
                    {previewShape.type === 'arrow' && (
                      <Arrow
                        points={[
                          previewShape.start.x,
                          previewShape.start.y,
                          previewShape.current.x,
                          previewShape.current.y,
                        ]}
                        stroke={ANNOTATION_STYLE.strokeColor}
                        fill={ANNOTATION_STYLE.strokeColor}
                        strokeWidth={ANNOTATION_STYLE.strokeWidth}
                        pointerLength={10}
                        pointerWidth={10}
                        opacity={0.6}
                      />
                    )}
                  </>
                )}
              </Layer>
            </Stage>
          </Box>
        )}

        {/* Text input overlay */}
        {textInput.visible && (
          <input
            autoFocus
            type="text"
            value={textInput.value}
            onChange={e => setTextInput(prev => ({ ...prev, value: e.target.value }))}
            onKeyDown={handleTextKeyDown}
            onBlur={handleTextSubmit}
            maxLength={100}
            style={{
              position: 'absolute',
              left: `${stagePosition.left + textInput.x}px`,
              top: `${stagePosition.top + textInput.y}px`,
              padding: '4px 8px',
              fontSize: `${ANNOTATION_STYLE.textFontSize}px`,
              border: `2px solid ${ANNOTATION_STYLE.strokeColor}`,
              borderRadius: '4px',
              backgroundColor: ANNOTATION_STYLE.textBgColor,
              color: ANNOTATION_STYLE.strokeColor,
              outline: 'none',
              fontFamily: 'Arial',
              minWidth: '100px',
            }}
          />
        )}
      </Box>

      {/* Toolbar */}
      <AnnotationToolbar
        activeTool={activeTool}
        onToolChange={setActiveTool}
        onUndo={handleUndo}
        onDone={handleDone}
        onCancel={handleCancel}
        canUndo={undoStack.length > 0}
      />
    </Dialog>
  )
}
