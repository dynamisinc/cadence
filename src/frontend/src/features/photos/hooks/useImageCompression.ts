/**
 * useImageCompression Hook
 *
 * Provides image compression utilities for photo capture.
 * Compresses photos to optimize storage and bandwidth while maintaining quality.
 * Generates thumbnails for gallery views.
 *
 * @module features/photos/hooks
 */

import { useCallback } from 'react'
import imageCompression from 'browser-image-compression'

/** Compressed photo result with full and thumbnail versions */
export interface CompressedPhoto {
  /** Compressed full-size image (max 1920px, JPEG 80%) */
  compressed: Blob
  /** Small thumbnail (300px, JPEG) */
  thumbnail: Blob
  /** Original file name from user's device */
  originalFileName: string
  /** Size of compressed image in bytes */
  fileSizeBytes: number
}

/**
 * Hook for compressing images and generating thumbnails
 *
 * Uses browser-image-compression with web workers for non-blocking compression.
 *
 * @example
 * ```tsx
 * const { compressImage } = useImageCompression();
 *
 * const handleCapture = async (file: File) => {
 *   const result = await compressImage(file);
 *   // result.compressed - full size (max 1920px)
 *   // result.thumbnail - 300px thumbnail
 * };
 * ```
 */
export const useImageCompression = () => {
  const compressImage = useCallback(async (file: File): Promise<CompressedPhoto> => {
    // Compress to max 1920px, JPEG 80% quality
    const compressed = await imageCompression(file, {
      maxWidthOrHeight: 1920,
      maxSizeMB: 2,
      useWebWorker: true,
      fileType: 'image/jpeg',
    })

    // Generate 300px thumbnail
    const thumbnail = await imageCompression(file, {
      maxWidthOrHeight: 300,
      maxSizeMB: 0.1,
      useWebWorker: true,
      fileType: 'image/jpeg',
    })

    return {
      compressed,
      thumbnail,
      originalFileName: file.name,
      fileSizeBytes: compressed.size,
    }
  }, [])

  return { compressImage }
}

export default useImageCompression
