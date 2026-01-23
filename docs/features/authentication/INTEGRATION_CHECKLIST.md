# Role Component Integration Checklist

## Completed Integrations ✅

### Core Pages
- [x] **ExerciseDetailPage** - Role badge in header
- [x] **ExerciseConductPage** - Role badge + permission checks
- [x] **ProfileMenu** - Exercise assignments display

### Permission Gates
- [x] **Fire Inject** - `can('fire_inject')` in ExerciseConductPage
- [x] **Add Observation** - `can('add_observation')` in ExerciseConductPage
- [x] **Edit Exercise** - Existing `canManage` permission (already working)

### Test Coverage
- [x] ExerciseDetailPage.test.tsx - Role badge test
- [x] ProfileMenu.test.tsx - Updated with mocks

## Components Already Properly Gated (No Changes Needed) ✅

These components already respect permission props passed from parents:

### InjectRow
- **File:** `src/frontend/src/features/injects/components/InjectRow.tsx`
- **Prop:** `canControl` - gates Fire/Skip/Reset buttons
- **Parent:** ExerciseConductPage (now uses role-based `can('fire_inject')`)

### ObservationList
- **File:** `src/frontend/src/features/observations/components/ObservationList.tsx`
- **Prop:** `canEdit` - gates Edit/Delete buttons
- **Parent:** ExerciseConductPage (now uses role-based `can('add_observation')`)

### ObservationForm
- **File:** `src/frontend/src/features/observations/components/ObservationForm.tsx`
- **Parent Control:** Only shown when `canAddObservations` is true in ExerciseConductPage

## Future Integration Opportunities 🔄

### Pages Not Yet Integrated (Optional)
- [ ] **ExerciseListPage** - Could add role filter/badge
- [ ] **CreateExercisePage** - Could gate with `can('create_exercise')`
- [ ] **ExerciseParticipantsPage** - Could gate edit with `can('manage_participants')`

### Navigation (Optional)
- [ ] Add role badge to navigation breadcrumbs
- [ ] Show role-specific menu items in sidebar (if implemented)

### Admin Features (Separate Story)
- [ ] Admin dashboard - Show all user role assignments
- [ ] User management - Assign system roles
- [ ] Exercise setup - Assign exercise roles

## Integration Patterns Used

### Pattern 1: Role Badge Display
```tsx
import { EffectiveRoleBadge } from '@/features/auth';

<ExerciseHeader
  exercise={exercise}
  actions={
    <>
      <EffectiveRoleBadge exerciseId={id ?? null} showOverride />
      {/* other actions */}
    </>
  }
/>
```

### Pattern 2: Permission Checking
```tsx
import { useExerciseRole } from '@/features/auth';

const { effectiveRole, can } = useExerciseRole(exerciseId);

const canControl = useMemo(() => {
  return exercise?.status === ExerciseStatus.Active && can('fire_inject');
}, [exercise, can]);
```

### Pattern 3: Profile Display
```tsx
import { roleResolutionService, getRoleColor, getRoleDisplayName } from '@/features/auth';

const [assignments, setAssignments] = useState<ExerciseAssignmentDto[]>([]);

useEffect(() => {
  const fetch = async () => {
    const data = await roleResolutionService.getUserExerciseAssignments(userId);
    setAssignments(data);
  };
  fetch();
}, [userId]);
```

## Testing Approach

### Unit Tests
- Mock `useExerciseRole` to return specific roles
- Test that components render/hide based on permissions
- Verify permission gates work correctly

### Integration Tests (Future)
- Test with real backend endpoints
- Verify role assignment flow
- Test role precedence (system vs. exercise)

### E2E Tests (Future)
- Login as different roles
- Verify UI elements appear/disappear
- Test complete permission workflow

## Backend Dependencies

### Required Endpoints
- ✅ `GET /api/exercises/{exerciseId}/participants`
- ⚠️ `GET /api/users/{userId}/exercise-assignments` (needs implementation)

### Permission Enforcement
- ✅ Controller role checking in ExerciseHub
- ✅ Permission attributes on API endpoints
- ✅ Role-based authorization in services

## Verification Steps

1. **Visual Check:**
   - [ ] Role badge appears on ExerciseDetailPage
   - [ ] Role badge appears on ExerciseConductPage
   - [ ] Exercise assignments show in ProfileMenu

2. **Permission Check:**
   - [ ] Controller sees Fire button
   - [ ] Observer does NOT see Fire button
   - [ ] Evaluator sees Add Observation button
   - [ ] Director/Admin sees Edit buttons

3. **Role Override Check:**
   - [ ] User with exercise role sees that role (not system role)
   - [ ] Tooltip explains role precedence
   - [ ] Color coding is consistent

## Rollback Instructions

If integration causes issues:

1. Revert commits for role integration
2. Restore previous hardcoded permission checks
3. Remove role badge components from pages
4. Remove exercise assignments from ProfileMenu

All changes are additive and can be safely removed without breaking existing functionality.

## Documentation Updates

- [x] Created ROLE_INTEGRATION_SUMMARY.md
- [x] Created INTEGRATION_CHECKLIST.md (this file)
- [ ] Update user guide with role documentation
- [ ] Update API documentation with new endpoint
- [ ] Create admin guide for role assignment

## Success Metrics

- ✅ Zero TypeScript errors
- ✅ All existing tests passing
- ✅ New test coverage for role badge
- ⏳ Manual QA verification (pending backend)

---

**Status:** Integration Complete (Frontend)
**Blockers:** Backend endpoint for user exercise assignments
**Next:** Backend implementation + E2E testing
