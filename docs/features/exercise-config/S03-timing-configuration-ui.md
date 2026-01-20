# Story CLK-03: Exercise Timing Configuration in Create/Edit Form

> **Story ID:** CLK-03
> **Feature:** Exercise Clock Modes
> **Phase:** D - Exercise Conduct
> **Status:** Ready for Development
> **Estimate:** Medium (1-2 days)

---

## User Story

**As an** Exercise Director,
**I want** to configure Delivery Mode and Timeline Mode when creating an exercise,
**So that** the exercise runs with the appropriate timing behavior.

---

## Scope

### In Scope
- Add Delivery Mode radio buttons to exercise create/edit form
- Add Timeline Mode radio buttons with conditional TimeScale input
- Implement smart defaults based on ExerciseType
- Make fields read-only when exercise status is Active
- Validation feedback for invalid configurations

### Out of Scope
- Auto-Ready logic implementation (CLK-05)
- Conduct view changes (CLK-06, CLK-07)
- Backend field additions (CLK-01 - prerequisite)

---

## Acceptance Criteria

- [ ] **Given** I am creating a new exercise, **when** I reach the configuration step, **then** I see "Delivery Mode" selection with "Clock-driven" and "Facilitator-paced" options
- [ ] **Given** I am creating a new exercise, **when** I select ExerciseType = TTX, **then** Delivery Mode defaults to "Facilitator-paced"
- [ ] **Given** I am creating a new exercise, **when** I select ExerciseType = FSE or FE, **then** Delivery Mode defaults to "Clock-driven"
- [ ] **Given** I am creating a new exercise, **when** I reach the configuration step, **then** I see "Timeline Mode" selection with "Real-time", "Compressed", and "Story-only" options
- [ ] **Given** I select "Compressed" timeline mode, **when** the option is selected, **then** a TimeScale input appears with placeholder "e.g., 4"
- [ ] **Given** I enter TimeScale = 4, **when** displayed, **then** helper text shows "1 real minute = 4 story minutes"
- [ ] **Given** I enter TimeScale > 60, **when** I try to save, **then** validation error appears
- [ ] **Given** an exercise is Active, **when** I view the settings, **then** timing fields are disabled with "locked" indicator
- [ ] **Given** an exercise is Draft, **when** I change Delivery Mode, **then** the change is saved successfully

---

## UI Design

### Delivery Mode Section

```
┌─────────────────────────────────────────────────────────────┐
│ How will injects be delivered?                    [?]       │
│                                                              │
│ ● Clock-driven                                               │
│   Injects automatically become Ready at their Delivery Time  │
│                                                              │
│ ○ Facilitator-paced                                          │
│   You control when each inject is delivered                  │
└─────────────────────────────────────────────────────────────┘
```

### Timeline Mode Section

```
┌─────────────────────────────────────────────────────────────┐
│ What timeline will the exercise use?              [?]       │
│                                                              │
│ ● Real-time                                                  │
│   Exercise clock matches wall clock (1:1)                    │
│                                                              │
│ ○ Compressed                                                 │
│   Simulate longer scenarios in less time                     │
│   Time scale: [____] (1 real minute = X story minutes)       │
│                                                              │
│ ○ Story-only                                                 │
│   No real-time clock, just narrative timestamps              │
└─────────────────────────────────────────────────────────────┘
```

### Locked State (Active Exercise)

```
┌─────────────────────────────────────────────────────────────┐
│ 🔒 Timing Configuration (locked during active exercise)     │
│                                                              │
│ Delivery Mode: Clock-driven                                  │
│ Timeline Mode: Compressed (4x)                               │
│                                                              │
│ To change these settings, stop the exercise first.           │
└─────────────────────────────────────────────────────────────┘
```

---

## Technical Design

### Component Structure

```
src/frontend/src/features/exercises/components/
├── ExerciseForm.tsx              # Existing - add timing section
├── TimingConfigurationSection.tsx # New component
└── TimingConfigurationSection.test.tsx
```

### TimingConfigurationSection Component

```typescript
interface TimingConfigurationSectionProps {
  deliveryMode: DeliveryMode;
  timelineMode: TimelineMode;
  timeScale: number | null;
  exerciseType: ExerciseType;
  isLocked: boolean;
  onChange: (field: string, value: any) => void;
  errors?: {
    deliveryMode?: string;
    timelineMode?: string;
    timeScale?: string;
  };
}
```

### Smart Defaults Logic

```typescript
const getDefaultDeliveryMode = (exerciseType: ExerciseType): DeliveryMode => {
  switch (exerciseType) {
    case ExerciseType.TTX:
    case ExerciseType.Workshop:
    case ExerciseType.Seminar:
      return DeliveryMode.FacilitatorPaced;
    case ExerciseType.FSE:
    case ExerciseType.FE:
    case ExerciseType.Drill:
    case ExerciseType.CAX:
    default:
      return DeliveryMode.ClockDriven;
  }
};
```

### Form Integration

In ExerciseForm.tsx, add the section after exercise type selection:

```tsx
<TimingConfigurationSection
  deliveryMode={formData.deliveryMode}
  timelineMode={formData.timelineMode}
  timeScale={formData.timeScale}
  exerciseType={formData.exerciseType}
  isLocked={exercise?.status === ExerciseStatus.Active}
  onChange={handleFieldChange}
  errors={errors}
/>
```

### Validation

```typescript
const validateTimingConfiguration = (data: ExerciseFormData): ValidationErrors => {
  const errors: ValidationErrors = {};

  if (data.timelineMode === TimelineMode.Compressed) {
    if (!data.timeScale) {
      errors.timeScale = 'Time scale is required for compressed mode';
    } else if (data.timeScale <= 0) {
      errors.timeScale = 'Time scale must be greater than 0';
    } else if (data.timeScale > 60) {
      errors.timeScale = 'Time scale cannot exceed 60x';
    }
  }

  return errors;
};
```

### API Integration

Update exercise create/update request:

```typescript
interface CreateExerciseRequest {
  // ... existing fields ...
  deliveryMode: DeliveryMode;
  timelineMode: TimelineMode;
  timeScale?: number;
}
```

---

## Test Cases

### Component Unit Tests

```typescript
describe('TimingConfigurationSection', () => {
  it('renders delivery mode options');
  it('renders timeline mode options');
  it('shows time scale input when Compressed selected');
  it('hides time scale input when Real-time selected');
  it('displays helper text with calculated story time');
  it('shows validation error for invalid time scale');
  it('disables all fields when isLocked=true');
  it('shows locked indicator when isLocked=true');
});
```

### Integration Tests

```typescript
describe('ExerciseForm timing configuration', () => {
  it('defaults to Clock-driven when FSE selected');
  it('defaults to Facilitator-paced when TTX selected');
  it('saves timing configuration on create');
  it('saves timing configuration on update');
  it('prevents changes when exercise is Active');
});
```

---

## Accessibility

- Radio groups have proper `role="radiogroup"` and `aria-labelledby`
- Help tooltips accessible via keyboard (? button)
- Locked state announced to screen readers
- Time scale input has associated label and helper text
- Error messages linked to inputs via `aria-describedby`

---

## Dependencies

| Dependency | Status | Notes |
|------------|--------|-------|
| CLK-01: Timing fields on Exercise | 🔲 Required | Backend must have fields first |
| Exercise create/edit form exists | ✅ Complete | Adding section to existing form |
| COBRA styled components | ✅ Complete | Use CobraTextField, etc. |

---

## Blocked By

- CLK-01: Add timing configuration fields to Exercise entity

---

## Blocks

- CLK-05: Auto-Ready logic (needs mode to determine behavior)
- CLK-06: Clock-driven conduct view (needs mode check)
- CLK-07: Facilitator-paced conduct view (needs mode check)

---

## Notes

- Consider adding info tooltips explaining each mode
- Time scale helper text updates dynamically as user types
- Future: Could add "recommended" badge on default option per exercise type
