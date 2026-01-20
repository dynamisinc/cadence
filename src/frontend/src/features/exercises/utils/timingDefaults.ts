/**
 * Smart defaults for exercise timing configuration
 *
 * Provides default Delivery Mode and Timeline Mode based on Exercise Type
 * per CLK-03 requirements.
 *
 * @module features/exercises/utils
 */

import { ExerciseType, DeliveryMode, TimelineMode } from '../../../types'

/**
 * Get the default delivery mode for an exercise type
 *
 * Discussion-based exercises (TTX) default to Facilitator-paced.
 * Operations-based exercises (FSE, FE, CAX) default to Clock-driven.
 */
export const getDefaultDeliveryMode = (exerciseType: ExerciseType): DeliveryMode => {
  switch (exerciseType) {
    case ExerciseType.TTX:
      return DeliveryMode.FacilitatorPaced
    case ExerciseType.FSE:
    case ExerciseType.FE:
    case ExerciseType.CAX:
    case ExerciseType.Hybrid:
    default:
      return DeliveryMode.ClockDriven
  }
}

/**
 * Get the default timeline mode for an exercise type
 *
 * Currently defaults to Real-time for all exercise types.
 * Can be extended with type-specific logic in the future.
 */
export const getDefaultTimelineMode = (_exerciseType: ExerciseType): TimelineMode => {
  return TimelineMode.RealTime
}
