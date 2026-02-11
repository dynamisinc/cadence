/**
 * usePhotos Hook
 *
 * React Query hook for managing photo state and operations.
 * Supports offline-first mutations with optimistic updates and queue sync.
 *
 * @module features/photos/hooks
 */

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useEffect, useState, useRef } from 'react'
import { toast } from 'react-toastify'
import { photoService } from '../services/photoService'
import { useConnectivity } from '../../../core/contexts'
import { addPendingAction } from '../../../core/offline'
import {
  cachePhotoBlob,
  getCachedPhotosByExercise,
  cachedPhotoToDto,
} from '../../../core/offline/photoCacheService'
import type { CachedPhoto } from '../../../core/offline/db'
import type {
  PhotoDto,
  PhotoListQuery,
  PhotoListResponse,
  UpdatePhotoRequest,
  QuickPhotoResponse,
} from '../types'

/** Generate a UUID-like idempotency key */
function generateIdempotencyKey(): string {
  return `${Date.now()}-${crypto.randomUUID()}`
}

/** Query key for photos list by exercise */
export const photosQueryKey = (exerciseId: string) =>
  ['photos', 'exercise', exerciseId] as const

/**
 * Hook for managing photos for an exercise
 *
 * Provides CRUD operations with offline support and optimistic updates.
 * Automatically invalidates related queries (observations for quick photos).
 *
 * @param exerciseId - The exercise to manage photos for
 * @param query - Optional filter/pagination parameters
 *
 * @example
 * ```tsx
 * const { photos, uploadPhoto, isUploading } = usePhotos(exerciseId);
 *
 * const handleUpload = async (formData: FormData) => {
 *   await uploadPhoto(formData);
 * };
 * ```
 */
export const usePhotos = (exerciseId: string, query?: PhotoListQuery) => {
  const queryClient = useQueryClient()
  const { connectivityState, incrementPendingCount } = useConnectivity()

  // Consider offline if not fully connected
  const isEffectivelyOnline = connectivityState === 'online'

  // State for locally cached pending photos
  const [localPhotos, setLocalPhotos] = useState<PhotoDto[]>([])

  // Track object URLs for cleanup
  const objectUrlsRef = useRef<string[]>([])

  // Query for fetching photos
  const {
    data,
    isLoading,
    error,
    refetch,
  } = useQuery({
    queryKey: [...photosQueryKey(exerciseId), query],
    queryFn: () => photoService.getPhotos(exerciseId, query),
    enabled: !!exerciseId,
  })

  // Load pending photos from IndexedDB on mount
  useEffect(() => {
    const loadLocalPhotos = async () => {
      if (!exerciseId) return

      try {
        const cachedPhotos = await getCachedPhotosByExercise(exerciseId)

        // Filter for pending/failed photos only
        const pendingPhotos = cachedPhotos.filter(
          p => p.syncStatus === 'pending' || p.syncStatus === 'failed',
        )

        // Convert to DTOs and track object URLs for cleanup
        const photoDtos = pendingPhotos.map(cached => {
          const dto = cachedPhotoToDto(cached)
          // Track all object URLs created
          objectUrlsRef.current.push(...dto._localObjectUrls)
          return dto
        })

        setLocalPhotos(photoDtos)
      } catch (err) {
        console.error('Failed to load cached photos:', err)
      }
    }

    loadLocalPhotos()

    // Cleanup: revoke all object URLs on unmount
    return () => {
      objectUrlsRef.current.forEach(url => {
        try {
          URL.revokeObjectURL(url)
        } catch {
          // Ignore revoke errors
        }
      })
      objectUrlsRef.current = []
    }
  }, [exerciseId])

  // Mutation for uploading photos
  const uploadMutation = useMutation({
    mutationFn: (formData: FormData) => photoService.uploadPhoto(exerciseId, formData),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: photosQueryKey(exerciseId) })
      toast.success('Photo uploaded')
    },
    onError: err => {
      toast.error(err instanceof Error ? err.message : 'Failed to upload photo')
    },
  })

  // Mutation for quick photo (upload + auto-create observation)
  const quickPhotoMutation = useMutation({
    mutationFn: (formData: FormData) => photoService.quickPhoto(exerciseId, formData),
    onSuccess: () => {
      // Invalidate both photos and observations queries
      queryClient.invalidateQueries({ queryKey: photosQueryKey(exerciseId) })
      queryClient.invalidateQueries({ queryKey: ['observations', 'exercise', exerciseId] })
      toast.success('Quick photo captured')
    },
    onError: err => {
      toast.error(err instanceof Error ? err.message : 'Failed to save quick photo')
    },
  })

  // Mutation for deleting photos with optimistic updates
  const deleteMutation = useMutation({
    mutationFn: (photoId: string) => photoService.deletePhoto(exerciseId, photoId),
    onMutate: async deletedId => {
      // Cancel pending queries to avoid race conditions
      await queryClient.cancelQueries({ queryKey: [...photosQueryKey(exerciseId), query] })

      // Snapshot for rollback
      const previousData = queryClient.getQueryData<PhotoListResponse>(
        [...photosQueryKey(exerciseId), query],
      )

      // Apply optimistic update - immediately remove from list
      queryClient.setQueryData<PhotoListResponse>(
        [...photosQueryKey(exerciseId), query],
        old => {
          if (!old) return old
          return {
            ...old,
            photos: old.photos.filter(photo => photo.id !== deletedId),
            totalCount: old.totalCount - 1,
          }
        },
      )

      return { previousData }
    },
    onSuccess: () => {
      toast.success('Photo deleted')
    },
    onError: (err, _deletedId, context) => {
      // Rollback to previous state
      if (context?.previousData) {
        queryClient.setQueryData(
          [...photosQueryKey(exerciseId), query],
          context.previousData,
        )
      }
      const message =
        err instanceof Error ? err.message : 'Failed to delete photo'
      toast.error(message)
    },
  })

  // Mutation for updating photo metadata with optimistic updates
  const updateMutation = useMutation({
    mutationFn: ({ photoId, request }: { photoId: string; request: UpdatePhotoRequest }) =>
      photoService.updatePhoto(exerciseId, photoId, request),
    onMutate: async ({ photoId, request }) => {
      // Cancel pending queries to avoid race conditions
      await queryClient.cancelQueries({ queryKey: [...photosQueryKey(exerciseId), query] })

      // Snapshot for rollback
      const previousData = queryClient.getQueryData<PhotoListResponse>(
        [...photosQueryKey(exerciseId), query],
      )

      // Apply optimistic update
      queryClient.setQueryData<PhotoListResponse>(
        [...photosQueryKey(exerciseId), query],
        old => {
          if (!old) return old
          return {
            ...old,
            photos: old.photos.map(photo =>
              photo.id === photoId
                ? {
                  ...photo,
                  observationId: request.observationId ?? photo.observationId,
                  displayOrder: request.displayOrder ?? photo.displayOrder,
                  annotationsJson: request.annotationsJson !== undefined ? request.annotationsJson : photo.annotationsJson,
                  updatedAt: new Date().toISOString(),
                }
                : photo,
            ),
          }
        },
      )

      return { previousData }
    },
    onSuccess: () => {
      // Invalidate to refetch fresh data (e.g. after annotation save)
      queryClient.invalidateQueries({ queryKey: photosQueryKey(exerciseId) })
      toast.success('Photo updated')
    },
    onError: (err, _variables, context) => {
      // Rollback to previous state
      if (context?.previousData) {
        queryClient.setQueryData(
          [...photosQueryKey(exerciseId), query],
          context.previousData,
        )
      }
      const message =
        err instanceof Error ? err.message : 'Failed to update photo'
      toast.error(message)
    },
  })

  /**
   * Upload photo with offline support
   * When offline: stores blob in IndexedDB photos table and queues lightweight action
   * When online: sends directly to API
   */
  const uploadPhoto = async (formData: FormData): Promise<PhotoDto> => {
    if (isEffectivelyOnline) {
      return uploadMutation.mutateAsync(formData)
    }

    // Offline: store blob in dedicated photos table, queue lightweight action
    const photoBlob = formData.get('photo') as Blob
    const thumbnailBlob = (formData.get('thumbnail') as Blob) ?? photoBlob
    const capturedAt = formData.get('capturedAt') as string
    const scenarioTime = formData.get('scenarioTime') as string | null
    const latitude = formData.get('latitude') ? parseFloat(formData.get('latitude') as string) : null
    const longitude = formData.get('longitude') ? parseFloat(formData.get('longitude') as string) : null
    const locationAccuracy = formData.get('locationAccuracy') ? parseFloat(formData.get('locationAccuracy') as string) : null
    const observationId = formData.get('observationId') as string | null

    const tempId = `temp-${Date.now()}-${Math.random().toString(36).slice(2)}`
    const idempotencyKey = generateIdempotencyKey()

    // Store blob in dedicated photos table (efficient binary storage)
    const cachedPhoto: CachedPhoto = {
      id: tempId,
      exerciseId,
      blob: photoBlob,
      thumbnailBlob,
      fileName: 'offline-photo.jpg',
      fileSizeBytes: photoBlob.size,
      syncStatus: 'pending',
      idempotencyKey,
      capturedAt,
      scenarioTime,
      latitude,
      longitude,
      locationAccuracy,
      observationId,
      cachedAt: new Date(),
      isQuickPhoto: false,
    }
    await cachePhotoBlob(cachedPhoto)

    // Queue lightweight action (no blob data, just a reference)
    await addPendingAction({
      type: 'UPLOAD_PHOTO',
      exerciseId,
      payload: {
        localPhotoId: tempId,
        idempotencyKey,
      },
    })

    incrementPendingCount()
    toast.info('Photo saved offline. Will sync when connection restores.')

    // Return optimistic photo with object URL for display
    const blobUrl = URL.createObjectURL(photoBlob)
    const thumbnailUrl = URL.createObjectURL(thumbnailBlob)
    return {
      id: tempId,
      exerciseId,
      observationId,
      capturedById: 'offline-user',
      capturedByName: 'You (offline)',
      fileName: 'offline-photo.jpg',
      blobUri: blobUrl,
      thumbnailUri: thumbnailUrl,
      fileSizeBytes: photoBlob.size,
      capturedAt,
      scenarioTime,
      latitude,
      longitude,
      locationAccuracy,
      displayOrder: 0,
      status: 'Draft',
      annotationsJson: null,
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
    }
  }

  /**
   * Quick photo capture with offline support
   * When offline: stores blob in IndexedDB photos table and queues lightweight action
   * When online: sends directly to API
   */
  const quickPhoto = async (formData: FormData): Promise<QuickPhotoResponse> => {
    if (isEffectivelyOnline) {
      return quickPhotoMutation.mutateAsync(formData)
    }

    // Offline: store blob in dedicated photos table, queue lightweight action
    const photoBlob = formData.get('photo') as Blob
    const thumbnailBlob = (formData.get('thumbnail') as Blob) ?? photoBlob
    const capturedAt = formData.get('capturedAt') as string
    const scenarioTime = formData.get('scenarioTime') as string | null
    const latitude = formData.get('latitude') ? parseFloat(formData.get('latitude') as string) : null
    const longitude = formData.get('longitude') ? parseFloat(formData.get('longitude') as string) : null
    const locationAccuracy = formData.get('locationAccuracy') ? parseFloat(formData.get('locationAccuracy') as string) : null

    const tempPhotoId = `temp-photo-${Date.now()}-${Math.random().toString(36).slice(2)}`
    const tempObsId = `temp-obs-${Date.now()}-${Math.random().toString(36).slice(2)}`
    const idempotencyKey = generateIdempotencyKey()

    // Store blob in dedicated photos table
    const cachedPhoto: CachedPhoto = {
      id: tempPhotoId,
      exerciseId,
      blob: photoBlob,
      thumbnailBlob,
      fileName: 'offline-photo.jpg',
      fileSizeBytes: photoBlob.size,
      syncStatus: 'pending',
      idempotencyKey,
      capturedAt,
      scenarioTime,
      latitude,
      longitude,
      locationAccuracy,
      cachedAt: new Date(),
      isQuickPhoto: true,
      tempObservationId: tempObsId,
    }
    await cachePhotoBlob(cachedPhoto)

    // Queue lightweight action (no blob data, just a reference)
    await addPendingAction({
      type: 'QUICK_PHOTO',
      exerciseId,
      payload: {
        localPhotoId: tempPhotoId,
        idempotencyKey,
        tempObsId,
      },
    })

    incrementPendingCount()
    toast.info('Quick photo saved offline. Will sync when connection restores.')

    // Return optimistic response with object URL for display
    const blobUrl = URL.createObjectURL(photoBlob)
    const thumbnailUrl = URL.createObjectURL(thumbnailBlob)
    return {
      photo: {
        id: tempPhotoId,
        exerciseId,
        observationId: tempObsId,
        capturedById: 'offline-user',
        capturedByName: 'You (offline)',
        fileName: 'offline-photo.jpg',
        blobUri: blobUrl,
        thumbnailUri: thumbnailUrl,
        fileSizeBytes: photoBlob.size,
        capturedAt,
        scenarioTime,
        latitude,
        longitude,
        locationAccuracy,
        displayOrder: 0,
        status: 'Draft',
        annotationsJson: null,
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString(),
      },
      observationId: tempObsId,
    }
  }

  /**
   * Delete photo with offline support
   * When offline: queues action and applies optimistic update
   * When online: sends directly to API
   */
  const deletePhoto = async (photoId: string): Promise<void> => {
    if (isEffectivelyOnline) {
      // Online: send directly to API
      await deleteMutation.mutateAsync(photoId)
      return
    }

    // Offline: queue action for later sync
    await addPendingAction({
      type: 'DELETE_PHOTO',
      exerciseId,
      payload: {
        photoId,
      },
    })

    // Apply optimistic update (remove from list)
    queryClient.setQueryData<PhotoListResponse>(
      [...photosQueryKey(exerciseId), query],
      old => {
        if (!old) return old
        return {
          ...old,
          photos: old.photos.filter(photo => photo.id !== photoId),
          totalCount: old.totalCount - 1,
        }
      },
    )

    incrementPendingCount()
    toast.info('Deletion queued. Will sync when connection restores.')
  }

  /**
   * Update photo metadata with offline support
   * When offline: queues action and applies optimistic update
   * When online: sends directly to API
   */
  const updatePhoto = async (
    photoId: string,
    request: UpdatePhotoRequest,
  ): Promise<PhotoDto> => {
    if (isEffectivelyOnline) {
      // Online: send directly to API
      return updateMutation.mutateAsync({ photoId, request })
    }

    // Offline: queue action and apply optimistic update
    const currentData = queryClient.getQueryData<PhotoListResponse>(
      [...photosQueryKey(exerciseId), query],
    )
    const existingPhoto = currentData?.photos.find(p => p.id === photoId)

    if (!existingPhoto) {
      throw new Error('Photo not found')
    }

    const optimisticPhoto: PhotoDto = {
      ...existingPhoto,
      observationId: request.observationId ?? existingPhoto.observationId,
      displayOrder: request.displayOrder ?? existingPhoto.displayOrder,
      updatedAt: new Date().toISOString(),
    }

    // Queue the action for later sync
    await addPendingAction({
      type: 'UPDATE_PHOTO',
      exerciseId,
      payload: {
        photoId,
        changes: request,
      },
    })

    // Apply optimistic update
    queryClient.setQueryData<PhotoListResponse>(
      [...photosQueryKey(exerciseId), query],
      old => {
        if (!old) return old
        return {
          ...old,
          photos: old.photos.map(photo =>
            photo.id === photoId ? optimisticPhoto : photo,
          ),
        }
      },
    )

    incrementPendingCount()
    toast.info('Changes saved offline. Will sync when connection restores.')

    return optimisticPhoto
  }

  // Merge local pending photos with server photos
  // Pending photos appear first (newest first)
  const mergedPhotos = [
    ...localPhotos.sort((a, b) =>
      new Date(b.capturedAt).getTime() - new Date(a.capturedAt).getTime(),
    ),
    ...(data?.photos ?? []),
  ]

  return {
    photos: mergedPhotos,
    totalCount: (data?.totalCount ?? 0) + localPhotos.length,
    page: data?.page ?? 1,
    pageSize: data?.pageSize ?? 20,
    isLoading,
    error: error ? (error instanceof Error ? error.message : 'Failed to load photos') : null,
    refetch,
    uploadPhoto,
    quickPhoto,
    deletePhoto,
    updatePhoto,
    isUploading: uploadMutation.isPending,
    isDeleting: deleteMutation.isPending,
  }
}

export default usePhotos
