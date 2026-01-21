# Test Fixtures Update Required

## Overview

The addition of timing configuration fields (CLK-01, CLK-02, CLK-03) requires updating test fixtures throughout the codebase. The following fields have been added as **required** (non-nullable) to their respective DTOs:

### ExerciseDto (CLK-01)
- `deliveryMode: DeliveryMode` - Required enum
- `timelineMode: TimelineMode` - Required enum
- `timeScale: number | null` - Nullable number

### InjectDto (CLK-02, CLK-04)
- `deliveryTime: string | null` - Calculated delivery time in wall clock
- `readyAt: string | null` - Timestamp when inject became Ready

---

## Fix Strategy

### Option 1: Use Test Fixtures (RECOMMENDED)

We've created centralized test fixtures that include all required fields with sensible defaults:

```typescript
// Import the fixture helper
import { createMockExercise } from '@/test/fixtures'

// Use in tests
const mockExercise = createMockExercise({
  // Override only the fields you need for your test
  name: 'My Test Exercise',
  status: ExerciseStatus.Active,
})
```

Similarly for injects:

```typescript
import { createMockInject } from '@/test/fixtures'

const mockInject = createMockInject({
  title: 'Critical Alert',
  status: InjectStatus.Fired,
  firedAt: '2026-03-15T10:30:00Z',
})
```

### Option 2: Manual Updates

If you prefer to keep inline fixtures, add the following fields:

**For ExerciseDto:**
```typescript
{
  // ... existing fields ...
  deliveryMode: DeliveryMode.ClockDriven,
  timelineMode: TimelineMode.RealTime,
  timeScale: null,
}
```

**For InjectDto:**
```typescript
{
  // ... existing fields ...
  deliveryTime: null, // or '2026-03-15T10:00:00Z' if fired
  readyAt: null, // or '2026-03-15T09:55:00Z' if ready
}
```

---

## Files Requiring Updates

### Exercises Feature

**Exercise test files:**
- [ ] `src/core/offline/cacheService.test.ts` - Line 32, 51
- [ ] `src/core/offline/cacheService.ts` - Line 51 (mock data)
- [ ] `src/features/exercises/components/ArchiveExerciseDialog.test.tsx` - Line 16
- [ ] `src/features/exercises/components/DeleteExerciseDialog.test.tsx` - Line 34
- [ ] `src/features/exercises/components/ExerciseHeader.test.tsx` - Line 20
- [ ] `src/features/exercises/components/NarrativeView.test.tsx` - Line 80
- [ ] `src/features/exercises/hooks/useExercises.test.tsx` - Lines 27, 124, 151
- [ ] `src/features/exercises/hooks/useExercises.ts` - Line 48 (optimistic update)
- [ ] `src/features/exercises/services/exerciseService.test.ts` - Lines 16, 81, 100

### Injects Feature

**Inject test files:**
- [ ] `src/core/offline/cacheService.test.ts` - Line 61
- [ ] `src/core/offline/cacheService.ts` - Line 163 (mock data)
- [ ] `src/features/exercise-clock/components/ExerciseProgress.test.tsx` - Line 16
- [ ] `src/features/exercises/components/FloatingClockChip.test.tsx` - Line 41
- [ ] `src/features/exercises/components/NarrativeView.test.tsx` - Line 17
- [ ] `src/features/exercises/components/StickyClockHeader.test.tsx` - Line 40
- [ ] `src/features/exercises/utils/narrativeGenerator.test.ts` - Line 17
- [ ] `src/features/injects/components/InjectForm.tsx` - Lines 51, 109 (form defaults)
- [ ] `src/features/injects/components/InjectRow.test.tsx` - Line 31
- [ ] `src/features/injects/services/injectService.test.ts` - Line 17
- [ ] `src/features/injects/utils/filterUtils.test.ts` - Line 25
- [ ] `src/features/injects/utils/groupUtils.test.ts` - Line 25
- [ ] `src/features/injects/utils/searchUtils.test.ts` - Line 22
- [ ] `src/features/injects/utils/sortUtils.test.ts` - Line 17
- [ ] `src/features/observations/components/ObservationForm.test.tsx` - Line 17

---

## Quick Fix Script

For bulk updates, you can use the fixtures by importing them:

```bash
# Add import to each test file
import { createMockExercise, createMockInject } from '@/test/fixtures'

# Then replace inline object literals with fixture calls
```

---

## Example Migration

### Before (OLD):
```typescript
const mockExercise: ExerciseDto = {
  id: 'ex-1',
  name: 'Test Exercise',
  description: 'Test',
  exerciseType: ExerciseType.TTX,
  status: ExerciseStatus.Draft,
  // ... 20+ more fields ...
}
```

### After (NEW):
```typescript
import { createMockExercise } from '@/test/fixtures'

const mockExercise = createMockExercise({
  name: 'Test Exercise',
  description: 'Test',
  exerciseType: ExerciseType.TTX,
  status: ExerciseStatus.Draft,
})
```

---

## Priority

**High Priority (Blocking):**
- Core service mocks (cacheService.ts, useExercises.ts)
- API service tests (exerciseService.test.ts, injectService.test.ts)

**Medium Priority:**
- Component tests
- Utility tests

**Low Priority:**
- Tests that don't directly use these DTOs

---

## Testing After Fix

After updating a file, run its tests:

```bash
npm test -- path/to/file.test.ts
```

To run all tests:

```bash
npm run test
```

To type-check:

```bash
npm run type-check
```

---

## Notes

- The test fixtures are located in `src/frontend/src/test/fixtures/`
- Fixtures use sensible defaults for all required fields
- You can override any field by passing it in the overrides object
- The fixtures follow the same patterns as the backend DTOs

---

**Status:** Test fixtures created ✅
**Remaining Work:** Update test files to use fixtures or add fields manually

