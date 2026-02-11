/**
 * useCamera Hook
 *
 * Manages camera/gallery access via hidden file input.
 * Handles the platform differences between camera capture and file selection.
 *
 * @module features/photos/hooks
 */

import { useState, useCallback, useRef, type ChangeEvent } from 'react'

/** Return type for useCamera hook */
export interface UseCameraReturn {
  /** Ref to the hidden file input element */
  fileInputRef: React.RefObject<HTMLInputElement | null>
  /** Whether a capture/selection is in progress */
  isCapturing: boolean
  /** Open the device camera (rear-facing) */
  openCamera: () => void
  /** Open the photo gallery/file picker */
  openGallery: () => void
  /** Handle file input change event */
  handleFileChange: (event: ChangeEvent<HTMLInputElement>) => void
  /** Reset capture state */
  resetCapture: () => void
}

/**
 * Hook for managing camera/gallery access
 *
 * Uses a hidden file input with `capture="environment"` attribute for camera mode.
 * Mobile devices will open the camera app, desktop will show file picker.
 *
 * @param onFileSelected - Callback when user selects/captures a file
 *
 * @example
 * ```tsx
 * const { fileInputRef, openCamera, handleFileChange } = useCamera((file) => {
 *   console.log('Captured:', file.name);
 * });
 *
 * return (
 *   <>
 *     <input
 *       ref={fileInputRef}
 *       type="file"
 *       hidden
 *       onChange={handleFileChange}
 *     />
 *     <button onClick={openCamera}>Take Photo</button>
 *   </>
 * );
 * ```
 */
export const useCamera = (onFileSelected: (file: File) => void): UseCameraReturn => {
  const fileInputRef = useRef<HTMLInputElement | null>(null)
  const [isCapturing, setIsCapturing] = useState(false)
  const captureMode = useRef<'camera' | 'gallery'>('camera')

  const openCamera = useCallback(() => {
    if (!fileInputRef.current) return
    captureMode.current = 'camera'
    fileInputRef.current.setAttribute('capture', 'environment')
    fileInputRef.current.accept = 'image/*'
    setIsCapturing(true)
    fileInputRef.current.click()
  }, [])

  const openGallery = useCallback(() => {
    if (!fileInputRef.current) return
    captureMode.current = 'gallery'
    fileInputRef.current.removeAttribute('capture')
    fileInputRef.current.accept = 'image/*'
    setIsCapturing(true)
    fileInputRef.current.click()
  }, [])

  const handleFileChange = useCallback(
    (event: ChangeEvent<HTMLInputElement>) => {
      const file = event.target.files?.[0]
      if (file) {
        onFileSelected(file)
      }
      setIsCapturing(false)
      // Reset input value so same file can be selected again
      if (fileInputRef.current) {
        fileInputRef.current.value = ''
      }
    },
    [onFileSelected],
  )

  const resetCapture = useCallback(() => {
    setIsCapturing(false)
    if (fileInputRef.current) {
      fileInputRef.current.value = ''
    }
  }, [])

  return {
    fileInputRef,
    isCapturing,
    openCamera,
    openGallery,
    handleFileChange,
    resetCapture,
  }
}

export default useCamera
