/**
 * Photo Annotation Types
 *
 * Defines the annotation data model for photo markup.
 * All coordinates use a relative system (0-1 range) to ensure
 * annotations render correctly at any display size.
 */

/** Circle/ellipse annotation drawn by finger drag */
export interface CircleAnnotation {
  type: 'circle'
  id: string
  /** Center X position (0-1 relative to photo width) */
  cx: number
  /** Center Y position (0-1 relative to photo height) */
  cy: number
  /** Radius X (0-1 relative to photo width) - allows ellipse */
  rx: number
  /** Radius Y (0-1 relative to photo height) - allows ellipse */
  ry: number
}

/** Arrow annotation drawn from start to end point */
export interface ArrowAnnotation {
  type: 'arrow'
  id: string
  /** Start X position (0-1) */
  x1: number
  /** Start Y position (0-1) */
  y1: number
  /** End X position (0-1) */
  x2: number
  /** End Y position (0-1) */
  y2: number
}

/** Text annotation placed at a point */
export interface TextAnnotation {
  type: 'text'
  id: string
  /** Position X (0-1) */
  x: number
  /** Position Y (0-1) */
  y: number
  /** Text content (max 100 characters) */
  content: string
}

/** Union of all annotation types */
export type Annotation = CircleAnnotation | ArrowAnnotation | TextAnnotation

/** Available annotation tools */
export type AnnotationTool = 'circle' | 'arrow' | 'text'

/** Annotation rendering constants */
export const ANNOTATION_STYLE = {
  /** Stroke color for all annotations */
  strokeColor: '#FF0000',
  /** Stroke width in pixels (at native resolution) */
  strokeWidth: 3,
  /** Fill color for text background */
  textBgColor: 'rgba(255, 255, 255, 0.75)',
  /** Text font size in pixels (at native resolution) */
  textFontSize: 16,
  /** Minimum touch target size in pixels */
  minTouchTarget: 44,
} as const
