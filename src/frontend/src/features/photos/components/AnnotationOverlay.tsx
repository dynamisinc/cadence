/**
 * AnnotationOverlay Component
 *
 * Renders annotations over a photo as an SVG overlay.
 * Displays circles, arrows, and text in a coordinate-independent way.
 * Read-only - no interactivity (for gallery/preview views).
 *
 * Uses a normalized coordinate system (0-1) that scales to any display size.
 * All annotations are rendered in red (#FF0000) with drop shadow for visibility.
 *
 * @module features/photos/components
 */

import { memo } from 'react'
import type { Annotation, CircleAnnotation, ArrowAnnotation, TextAnnotation } from '../types'
import { ANNOTATION_STYLE } from '../types'

interface AnnotationOverlayProps {
  /** List of annotations to render */
  annotations: Annotation[]
  /** Width of the container in pixels */
  width: number
  /** Height of the container in pixels */
  height: number
}

/**
 * Render a circle annotation as an SVG ellipse
 */
const renderCircle = (annotation: CircleAnnotation) => {
  return (
    <ellipse
      key={annotation.id}
      cx={annotation.cx}
      cy={annotation.cy}
      rx={annotation.rx}
      ry={annotation.ry}
      stroke={ANNOTATION_STYLE.strokeColor}
      strokeWidth={0.003} // Scaled for viewBox coordinates
      fill="none"
      filter="url(#annotation-shadow)"
    />
  )
}

/**
 * Render an arrow annotation as an SVG line with arrowhead marker
 */
const renderArrow = (annotation: ArrowAnnotation) => {
  return (
    <line
      key={annotation.id}
      x1={annotation.x1}
      y1={annotation.y1}
      x2={annotation.x2}
      y2={annotation.y2}
      stroke={ANNOTATION_STYLE.strokeColor}
      strokeWidth={0.003} // Scaled for viewBox coordinates
      markerEnd="url(#arrowhead)"
      filter="url(#annotation-shadow)"
    />
  )
}

/**
 * Render a text annotation with white background
 */
const renderText = (annotation: TextAnnotation) => {
  // Calculate approximate text dimensions for background
  const charWidth = 0.008 // Approximate width per character in viewBox units
  const textWidth = annotation.content.length * charWidth
  const textHeight = 0.02 // Approximate height in viewBox units
  const padding = 0.003

  return (
    <g key={annotation.id}>
      {/* Background rectangle */}
      <rect
        x={annotation.x - padding}
        y={annotation.y - textHeight}
        width={textWidth + padding * 2}
        height={textHeight + padding}
        fill={ANNOTATION_STYLE.textBgColor}
        rx={0.002}
        filter="url(#annotation-shadow)"
      />

      {/* Text */}
      <text
        x={annotation.x}
        y={annotation.y}
        fill={ANNOTATION_STYLE.strokeColor}
        fontSize="0.016" // Scaled for viewBox coordinates
        fontWeight="bold"
        fontFamily="sans-serif"
        dominantBaseline="alphabetic"
      >
        {annotation.content}
      </text>
    </g>
  )
}

/**
 * AnnotationOverlay - SVG overlay for rendering read-only annotations
 */
export const AnnotationOverlay = memo((
  { annotations, width: _width, height: _height }: AnnotationOverlayProps,
) => {
  // Don't render if no annotations
  if (!annotations || annotations.length === 0) {
    return null
  }

  return (
    <svg
      viewBox="0 0 1 1"
      preserveAspectRatio="none"
      style={{
        position: 'absolute',
        top: 0,
        left: 0,
        width: '100%',
        height: '100%',
        pointerEvents: 'none',
      }}
      aria-hidden="true"
    >
      {/* SVG definitions for shared elements */}
      <defs>
        {/* Drop shadow filter for better visibility */}
        <filter id="annotation-shadow">
          <feDropShadow
            dx="0"
            dy="0"
            stdDeviation="0.002"
            floodColor="rgba(0, 0, 0, 0.5)"
          />
        </filter>

        {/* Arrowhead marker */}
        <marker
          id="arrowhead"
          markerWidth="10"
          markerHeight="10"
          refX="8"
          refY="3"
          orient="auto"
          markerUnits="strokeWidth"
        >
          <path
            d="M0,0 L0,6 L9,3 z"
            fill={ANNOTATION_STYLE.strokeColor}
          />
        </marker>
      </defs>

      {/* Render all annotations */}
      {annotations.map(annotation => {
        switch (annotation.type) {
          case 'circle':
            return renderCircle(annotation)
          case 'arrow':
            return renderArrow(annotation)
          case 'text':
            return renderText(annotation)
          default:
            return null
        }
      })}
    </svg>
  )
})

AnnotationOverlay.displayName = 'AnnotationOverlay'

export default AnnotationOverlay
