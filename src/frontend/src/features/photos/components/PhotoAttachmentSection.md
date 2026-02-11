# PhotoAttachmentSection Component

## Overview

A horizontal scrolling strip of photo thumbnails designed for embedding in observation forms. Allows Evaluators to attach photos to observations during exercise conduct.

## Features

- **Horizontal scrollable row** - Thumbnails display in a row with smooth scrolling
- **Add Photo button** - Opens device camera/gallery to capture/select photos
- **Auto-compression** - Compresses images before upload using `useImageCompression`
- **Automatic upload** - Uploads photos linked to the observation
- **Upload progress** - Shows loading state while photo is being processed
- **Photo preview** - Clicking thumbnails will open PhotoPreview (future enhancement)
- **Conditional rendering** - Only shown when editing existing observations (has `observationId`)

## Props

```typescript
interface PhotoAttachmentSectionProps {
  exerciseId: string              // The exercise this observation belongs to
  observationId?: string          // The observation ID (required to show section)
  photos: PhotoTagDto[]           // Current photos attached to the observation
  onPhotoAdded?: () => void       // Called after photo successfully added
  scenarioTime?: string | null    // Scenario time to stamp photo with
}
```

## Usage

### In ObservationForm (Edit Mode)

```tsx
<ObservationForm
  exerciseId={exerciseId}
  observation={editingObservation}  // Must have ID
  scenarioTime={currentScenarioTime}
  onPhotoAdded={() => {
    // Refresh observation to show new photo
    queryClient.invalidateQueries({ queryKey: observationsQueryKey(exerciseId) })
  }}
  // ... other props
/>
```

### Hidden in Create Mode

When creating a new observation (no `observationId` yet), the section is hidden:

```tsx
<ObservationForm
  exerciseId={exerciseId}
  // No observation prop = section hidden
  onSubmit={handleCreate}
  // ... other props
/>
```

## Behavior

1. **Only renders when `observationId` is provided** - New observations don't show photo section
2. **Photos sorted by `displayOrder`** - Maintains consistent ordering
3. **Clicking "Add Photo"** - Opens device camera/gallery via `useCamera` hook
4. **Auto-compresses** - Uses `useImageCompression` to optimize file size
5. **Uploads to observation** - Sets `observationId` in upload request
6. **Callback on success** - Calls `onPhotoAdded()` to refresh parent

## Hooks Used

- `useCamera` - Manages camera/gallery access
- `useImageCompression` - Compresses images before upload
- `usePhotos` - Handles photo upload with offline support

## Styling

- **Thumbnails**: 60px height, rounded corners, bordered
- **Add Photo Button**: CobraSecondaryButton with compact size
- **Horizontal scroll**: `overflowX: auto` with `gap: 1`

## Future Enhancements

- Open PhotoPreview modal when thumbnail clicked
- Delete photo option
- Reorder photos via drag-and-drop
- Show photo metadata (captured time, location)

## Related Components

- `ObservationForm` - Parent component
- `PhotoPreview` - Full-size photo viewer (future)
- `QuickPhotoFab` - Quick photo capture during conduct

## Test Coverage

See `PhotoAttachmentSection.test.tsx`:

- ✅ Hidden when no observationId
- ✅ Shows section with Add Photo button when observationId provided
- ✅ Renders thumbnails sorted by displayOrder
- ✅ Thumbnails are clickable
- ✅ Maintains horizontal scroll layout
