/**
 * Photo Hooks
 *
 * Barrel export for all photo-related React hooks.
 *
 * @module features/photos/hooks
 */

export { useCamera, type UseCameraReturn } from './useCamera'
export { useImageCompression, type CompressedPhoto } from './useImageCompression'
export { usePhotos, photosQueryKey } from './usePhotos'
export { usePhotoAdmin, deletedPhotosQueryKey } from './usePhotoAdmin'
