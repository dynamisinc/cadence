/**
 * PhotoCapture Component
 *
 * Provides camera/gallery capture with automatic compression.
 * Captures GPS location and scenario time for each photo.
 * Uses COBRA styled buttons and FontAwesome icons.
 *
 * @module features/photos/components
 */

import { useState } from 'react'
import { Box, Stack } from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faCamera, faImage, faSpinner } from '@fortawesome/free-solid-svg-icons'
import { CobraPrimaryButton, CobraSecondaryButton } from '@/theme/styledComponents'
import { useCamera } from '../hooks/useCamera'
import { useImageCompression } from '../hooks/useImageCompression'

interface PhotoCaptureProps {
  /** Exercise ID for the photo */
  exerciseId: string
  /** Called after photo is compressed and ready for upload */
  onPhotoCaptured: (formData: FormData) => Promise<void>
  /** Current scenario time from exercise clock (if running) */
  scenarioTime?: string | null
  /** Whether the component is in a loading/uploading state */
  isUploading?: boolean
}

export const PhotoCapture = ({
  exerciseId,
  onPhotoCaptured,
  scenarioTime = null,
  isUploading = false,
}: PhotoCaptureProps) => {
  const [isProcessing, setIsProcessing] = useState(false)
  const { compressImage } = useImageCompression()

  /**
   * Handle file selection from camera or gallery
   */
  const handleFileSelected = async (file: File) => {
    setIsProcessing(true)

    try {
      // Compress image and generate thumbnail
      const compressed = await compressImage(file)

      // Get GPS location
      let latitude: number | null = null
      let longitude: number | null = null
      let locationAccuracy: number | null = null

      if ('geolocation' in navigator) {
        try {
          const position = await new Promise<GeolocationPosition>(
            (resolve, reject) => {
              navigator.geolocation.getCurrentPosition(resolve, reject, {
                enableHighAccuracy: true,
                timeout: 10000,
                maximumAge: 0,
              })
            },
          )
          latitude = position.coords.latitude
          longitude = position.coords.longitude
          locationAccuracy = position.coords.accuracy
        } catch (error) {
          // GPS unavailable or denied - continue without location
          console.warn('GPS location unavailable:', error)
        }
      }

      // Build FormData
      const formData = new FormData()
      formData.append('photo', compressed.compressed, compressed.originalFileName)
      formData.append('thumbnail', compressed.thumbnail, `thumb_${compressed.originalFileName}`)
      formData.append('capturedAt', new Date().toISOString())

      if (scenarioTime) {
        formData.append('scenarioTime', scenarioTime)
      }

      if (latitude !== null) {
        formData.append('latitude', latitude.toString())
      }

      if (longitude !== null) {
        formData.append('longitude', longitude.toString())
      }

      if (locationAccuracy !== null) {
        formData.append('locationAccuracy', locationAccuracy.toString())
      }

      // Call parent handler
      await onPhotoCaptured(formData)
    } catch (error) {
      console.error('Photo capture failed:', error)
      throw error
    } finally {
      setIsProcessing(false)
    }
  }

  const { fileInputRef, openCamera, openGallery, handleFileChange } = useCamera(handleFileSelected)

  const isLoading = isProcessing || isUploading

  return (
    <Box>
      {/* Hidden file input */}
      <input
        ref={fileInputRef}
        type="file"
        hidden
        accept="image/*"
        onChange={handleFileChange}
      />

      {/* Capture buttons */}
      <Stack direction="row" spacing={2}>
        <CobraPrimaryButton
          startIcon={
            isLoading ? (
              <FontAwesomeIcon icon={faSpinner} spin />
            ) : (
              <FontAwesomeIcon icon={faCamera} />
            )
          }
          onClick={openCamera}
          disabled={isLoading}
        >
          {isLoading ? 'Processing...' : 'Camera'}
        </CobraPrimaryButton>

        <CobraSecondaryButton
          startIcon={<FontAwesomeIcon icon={faImage} />}
          onClick={openGallery}
          disabled={isLoading}
        >
          Gallery
        </CobraSecondaryButton>
      </Stack>
    </Box>
  )
}

export default PhotoCapture
