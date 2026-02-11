/**
 * QuickPhotoFab - Floating Action Button for quick photo capture during conduct
 *
 * A persistent FAB that appears during active exercises, allowing Controllers
 * and Evaluators to quickly capture photos with auto-GPS location and scenario time.
 *
 * Features:
 * - Only visible when exercise status is Active
 * - Opens camera directly (environment-facing)
 * - Auto-compresses images before upload
 * - Captures GPS coordinates automatically
 * - Shows loading state during upload
 * - Brief success toast on completion
 *
 * @module features/photos
 * @see docs/features/field-operations/photo-capture/S01-quick-photo-capture.md
 */

import { type FC, useState, useCallback } from 'react'
import { Fab, CircularProgress } from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faCamera } from '@fortawesome/free-solid-svg-icons'
import { useCamera } from '../hooks/useCamera'
import { useImageCompression } from '../hooks/useImageCompression'
import { usePhotos } from '../hooks/usePhotos'
import { useExerciseNavigation } from '@/shared/contexts'
import { ExerciseStatus } from '@/types'

interface QuickPhotoFabProps {
  /** Exercise ID for photo upload */
  exerciseId: string
  /** Current scenario time from exercise clock */
  scenarioTime?: string | null
}

/**
 * Floating Action Button for quick photo capture
 *
 * Positioned in bottom-right corner. Responsive sizing:
 * - Mobile (≤600px): 64px
 * - Tablet+: 56px
 *
 * @example
 * ```tsx
 * <QuickPhotoFab exerciseId={exerciseId} scenarioTime={displayTime} />
 * ```
 */
export const QuickPhotoFab: FC<QuickPhotoFabProps> = ({ exerciseId, scenarioTime }) => {
  const [isProcessing, setIsProcessing] = useState(false)
  const { currentExercise } = useExerciseNavigation()
  const { compressImage } = useImageCompression()
  const { quickPhoto } = usePhotos(exerciseId)

  // Get GPS location
  const getLocation = useCallback(
    (): Promise<{ latitude: number; longitude: number; accuracy: number } | null> => {
      return new Promise(resolve => {
        if (!navigator.geolocation) {
          resolve(null)
          return
        }

        navigator.geolocation.getCurrentPosition(
          position => {
            resolve({
              latitude: position.coords.latitude,
              longitude: position.coords.longitude,
              accuracy: position.coords.accuracy,
            })
          },
          _error => {
            // Silently fail - location is optional
            resolve(null)
          },
          {
            enableHighAccuracy: true,
            timeout: 5000,
            maximumAge: 0,
          },
        )
      })
    },
    [],
  )

  // Handle file capture
  const handleFileSelected = useCallback(
    async (file: File) => {
      setIsProcessing(true)
      try {
        // Compress image and generate thumbnail
        const { compressed, thumbnail } = await compressImage(file)

        // Get GPS location
        const location = await getLocation()

        // Build form data
        const formData = new FormData()
        const fileName = file.name || 'photo.jpg'
        const thumbName = `thumb_${fileName}`
        formData.append('photo', compressed, fileName)
        formData.append('thumbnail', thumbnail, thumbName)
        formData.append('capturedAt', new Date().toISOString())

        if (scenarioTime) {
          formData.append('scenarioTime', scenarioTime)
        }

        if (location) {
          formData.append('latitude', location.latitude.toString())
          formData.append('longitude', location.longitude.toString())
          formData.append('locationAccuracy', location.accuracy.toString())
        }

        // Upload photo (creates observation automatically)
        await quickPhoto(formData)
      } catch (error) {
        console.error('Failed to capture quick photo:', error)
      } finally {
        setIsProcessing(false)
      }
    },
    [compressImage, getLocation, scenarioTime, quickPhoto],
  )

  // Camera hook
  const { fileInputRef, openCamera, handleFileChange } = useCamera(handleFileSelected)

  // Handle FAB click
  const handleClick = useCallback(() => {
    if (isProcessing) return
    openCamera()
  }, [isProcessing, openCamera])

  // Only show for Active exercises
  if (currentExercise?.status !== ExerciseStatus.Active) {
    return null
  }

  return (
    <>
      {/* Hidden file input for camera */}
      <input
        ref={fileInputRef}
        type="file"
        accept="image/*"
        capture="environment"
        hidden
        onChange={handleFileChange}
        aria-label="Camera input"
      />

      {/* Floating Action Button */}
      <Fab
        color="primary"
        onClick={handleClick}
        disabled={isProcessing}
        aria-label="Quick photo capture"
        sx={{
          position: 'fixed',
          bottom: 24,
          right: 24,
          zIndex: 1200,
          width: { xs: 64, sm: 56 },
          height: { xs: 64, sm: 56 },
          borderRadius: '50%',
          boxShadow: 3,
        }}
      >
        {isProcessing ? (
          <CircularProgress size={24} sx={{ color: 'white' }} />
        ) : (
          <FontAwesomeIcon icon={faCamera} size="lg" style={{ color: 'white' }} />
        )}
      </Fab>
    </>
  )
}

export default QuickPhotoFab
