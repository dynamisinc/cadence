# Field Operations: Mobile Design Addendum

> **Usage:** Reference this document in implementation prompts alongside feature stories. This is not a story — it's implementation guidance for responsive behavior across Field Operations features. Stories describe *what* users need; this document describes *how* layouts and interactions adapt below the tablet breakpoint.

---

## Breakpoint Definitions

| Breakpoint | Range | Label | Primary Context |
|------------|-------|-------|-----------------|
| **Phone** | < 768px | `xs`, `sm` | Field evaluator with personal device. One-handed use common. Quick capture priority. |
| **Tablet** | 768px – 1023px | `md` | Primary field device. Two-handed use. Full observation workflow. |
| **Desktop** | ≥ 1024px | `lg`, `xl` | Exercise Control room. Director dashboard. Multi-panel layouts. |

**Design principle:** Phone is a first-class field capture device, not a degraded tablet experience. Optimize for the two most common phone actions: Quick Photo and Quick-Add Observation. Accept that complex views (Director map, photo gallery with filters, position history) are functional but optimized for tablet+.

---

## Story-Level Mobile Priority

Not every story needs equal phone optimization. This matrix guides where to invest responsive effort:

| Story | Phone Priority | Rationale |
|-------|---------------|-----------|
| **Photo: S01 Capture Photo** | 🔴 Critical | Phone is the most natural camera device |
| **Photo: S02 Attach to Observation** | 🔴 Critical | Core workflow after capture |
| **Photo: S03 Quick Photo (FAB)** | 🔴 Critical | Primary phone interaction — one-tap capture |
| **Photo: S04 Photo Gallery** | 🟡 Functional | Usable but not primary phone task. 2-column grid. |
| **Photo: S05 Annotate Photo** | 🟡 Functional | Finger annotation on small screen is imprecise but acceptable |
| **Photo: S06 Offline Queue** | 🔴 Critical | Phone users most likely to have poor connectivity |
| **Obs: S01 Quick-Add Observation** | 🔴 Critical | Fast field capture is the phone's primary job |
| **Obs: S02 Voice-to-Text** | 🔴 Critical | Even more valuable on phone — typing on small keyboard is slow |
| **Obs: S03 Link to Active Inject** | 🟢 Standard | Same interaction pattern works, just tighter layout |
| **Obs: S04 Safety Flag** | 🔴 Critical | Safety concerns must work from any device instantly |
| **Obs: S05 Director Observation Feed** | 🟡 Functional | Director is typically on tablet/desktop. Phone is a check-in view. |
| **Location: S01 Opt-In Sharing** | 🟢 Standard | Modal prompt works at any size |
| **Location: S02 Stamp on Observations** | 🟢 Standard | Invisible to user — no layout impact |
| **Location: S03 Stamp on Inject Firing** | 🟢 Standard | Invisible to user — no layout impact |
| **Location: S04 Director Map** | ⚪ Tablet+ optimized | Map on phone is usable but not the target form factor |
| **Location: S05 Position History** | ⚪ Tablet+ optimized | Timeline scrubber + map + controls need screen real estate |

**Legend:**
- 🔴 Critical — Must be designed phone-first, tested on 375px viewport
- 🟡 Functional — Works on phone, optimized for tablet
- 🟢 Standard — Same interaction at all sizes, minimal responsive adaptation needed
- ⚪ Tablet+ optimized — Functional on phone but designed for larger screens

---

## Component Behavior by Viewport

### Floating Action Button (FAB)

The FAB is the primary phone interaction for Quick Photo capture.

| Property | Phone (< 768px) | Tablet (≥ 768px) |
|----------|-----------------|-------------------|
| Size | 64px × 64px | 56px × 56px |
| Position | Bottom-right, 16px from edges | Bottom-right, 24px from edges |
| Safe area | Respect iOS safe area inset (`env(safe-area-inset-bottom)`) | Standard positioning |
| Z-index | Above bottom sheet, below modals | Above content, below modals |
| Label | Icon only (📷) | Icon only (📷), tooltip on hover |

```
Phone FAB placement:

┌──────────────────┐
│                  │
│  [content]       │
│                  │
│                  │
│                  │
│          ┌────┐  │
│          │ 📷 │  │  ← 64px, 16px from bottom/right
│          └────┘  │
│   ▓▓▓▓▓▓▓▓▓▓▓▓  │  ← iOS home indicator safe area
└──────────────────┘
```

### Bottom Sheets / Modals

Quick-Add Observation and inject selector use bottom sheets on tablet. On phone, these become full-screen modals to maximize input space.

| Property | Phone (< 768px) | Tablet (≥ 768px) |
|----------|-----------------|-------------------|
| Quick-Add Observation | Full-screen modal | Bottom sheet (60% height) |
| Inject Selector | Full-screen modal with search | Bottom sheet with search |
| Photo Preview | Full-screen overlay | Modal overlay (80% viewport) |
| Safety Confirm | Full-screen modal | Centered modal (480px max-width) |
| Transition | Slide up from bottom | Slide up from bottom |
| Dismiss | Back button or ✕ | Swipe down, tap outside, or ✕ |

```
Phone — Quick-Add as full-screen modal:

┌──────────────────┐
│ New Observation ✕ │
├──────────────────┤
│ ┌──────────────┐ │
│ │ What did you │ │
│ │ observe?  🎤 │ │
│ │              │ │
│ │              │ │
│ └──────────────┘ │
│                  │
│ [P] [S] [M] [U] │
│ [N/A]            │
│                  │
│ Inject: [▼]     │
│ 📷 Photo  ⚠️ Safe│
│                  │
│ [Cancel] [Save ✓]│
└──────────────────┘

Tablet — Quick-Add as bottom sheet:

┌─────────────────────────────────┐
│ Exercise Conduct View           │
│                                 │
│  [exercise content visible]     │
│                                 │
├─────────────────────────────────┤
│ New Observation              ✕  │
│ ┌─────────────────────────────┐ │
│ │ What did you observe?   🎤  │ │
│ └─────────────────────────────┘ │
│ [P] [S] [M] [U] [N/A]         │
│ Inject: [▼]  📷  ⚠️            │
│ [Cancel]  [Save & New] [Save ✓]│
└─────────────────────────────────┘
```

### Rating Buttons (P/S/M/U/N/A)

| Property | Phone (< 768px) | Tablet (≥ 768px) |
|----------|-----------------|-------------------|
| Button size | 48px × 48px minimum | 44px × 44px |
| Layout | Full-width row, equal distribution | Inline row |
| Labels | Single letter (P, S, M, U) | Single letter, tooltip with full name |
| Selected state | Filled background + bold text | Filled background + bold text |
| Spacing | 8px gap | 8px gap |

```
Phone rating layout (full-width distribution):

┌──────────────────────────────┐
│ [ P ] [ S ] [ M ] [ U ] [NA]│  ← each button expands equally
└──────────────────────────────┘
```

### Photo Thumbnails

| Context | Phone (< 768px) | Tablet (≥ 768px) |
|---------|-----------------|-------------------|
| In observation form | 60px × 60px, horizontal scroll | 80px × 80px, horizontal scroll |
| Photo gallery grid | 2 columns | 3 columns |
| Full-size preview | Full-screen overlay with swipe | Modal with swipe (80% viewport) |
| Gallery filter chips | Horizontally scrollable, single row | Horizontally scrollable, single row |

```
Phone photo gallery (2-column):

┌──────────────────┐
│ Photos    Filter ▼│
│ 47 photos        │
├──────────────────┤
│ ┌──────┐┌──────┐ │
│ │ img1 ││ img2 │ │
│ │      ││      │ │
│ ├──────┤├──────┤ │
│ │10:32 ││10:45 │ │
│ └──────┘└──────┘ │
│ ┌──────┐┌──────┐ │
│ │ img3 ││ img4 │ │
│ │      ││      │ │
│ ├──────┤├──────┤ │
│ │11:02 ││11:15 │ │
│ └──────┘└──────┘ │
└──────────────────┘
```

### Director Observation Feed

| Property | Phone (< 768px) | Tablet (≥ 768px) | Desktop (≥ 1024px) |
|----------|-----------------|-------------------|---------------------|
| Layout | Single column, full width | Single column, max-width 720px | Split: feed left, detail right |
| Safety pinned section | Collapsible banner at top | Always visible at top | Always visible at top |
| Observation cards | Full-width, compact text | Full-width, 2-line preview | Full-width, 2-line preview |
| Filter controls | Icon button → full-screen filter panel | Inline dropdowns | Inline dropdowns |
| "New" badge | Floating banner, tappable | Floating banner, tappable | Floating banner, tappable |

### Director Location Map

| Property | Phone (< 768px) | Tablet (≥ 768px) | Desktop (≥ 1024px) |
|----------|-----------------|-------------------|---------------------|
| Map area | Full viewport minus header | Full viewport minus header and legend | Main panel with sidebar for participant list |
| Legend | Collapsed by default, toggle to show | Visible below map | Sidebar |
| Participant popup | Bottom sheet | Map popup | Map popup + sidebar detail |
| Controls (zoom, layers) | Standard Leaflet touch controls | Standard Leaflet touch controls | Standard + mouse wheel zoom |

### Annotation Tools (Photo Markup)

| Property | Phone (< 768px) | Tablet (≥ 768px) |
|----------|-----------------|-------------------|
| Tool bar position | Bottom of screen | Bottom of screen |
| Tool button size | 56px × 56px | 48px × 48px |
| Drawing precision | Finger — less precise, larger default circle/arrow sizes | Finger or stylus — standard sizes |
| Text label input | Full-width text field above keyboard | Inline text field at annotation point |
| Undo button | Prominent, top-right | Top toolbar |

---

## Touch Target Standards

All interactive elements must meet minimum touch target sizes per viewport:

| Element Type | Phone Minimum | Tablet Minimum | Reference |
|-------------|---------------|----------------|-----------|
| Buttons (primary actions) | 48px × 48px | 44px × 44px | WCAG 2.5.8 |
| Icon buttons | 48px × 48px | 44px × 44px | Material Design |
| List items (tappable rows) | 48px height | 44px height | Material Design |
| Rating buttons | 48px × 48px | 44px × 44px | Custom |
| Checkbox / toggle | 48px tap area | 44px tap area | Includes padding |
| FAB | 64px × 64px | 56px × 56px | Material Design (elevated) |

**Spacing between touch targets:** Minimum 8px on phone to prevent mis-taps.

---

## Phone-Specific Interaction Patterns

### Camera Capture Flow (Phone-Optimized)

The phone camera flow should be as close to a native camera app as possible:

```
1. Tap FAB (📷)
   ↓
2. Native camera opens (fullscreen)
   ↓
3. Capture photo
   ↓
4. Brief preview (1 sec) — "Use" / "Retake"
   ↓
5. Toast: "Photo saved as draft"
   ↓
6. Return to previous view
   
Total taps: 3 (FAB → capture → use)
Total time: < 5 seconds
```

On phone, always use `<input type="file" accept="image/*" capture="environment">` as the primary capture method rather than `getUserMedia`. This triggers the native camera app experience that users are familiar with and handles permissions more reliably on mobile browsers.

### Voice Input (Phone-Optimized)

Voice-to-text is proportionally more valuable on phone because the on-screen keyboard consumes ~50% of the viewport. Specific phone considerations:

- Microphone button should be at least 48px and positioned at the trailing edge of the text field
- When voice recognition is active, collapse the on-screen keyboard to reclaim viewport space
- "Listening" indicator should be prominent — on a small screen, the user needs clear feedback
- Consider keeping the screen awake during voice capture (`navigator.wakeLock.request('screen')` where supported)

### Navigation During Active Exercise (Phone)

On phone during active exercise conduct, minimize navigation depth. The most common flows should be ≤ 2 taps from any screen:

```
Any screen in exercise:
  ├── FAB tap → Camera → Draft observation (1-2 taps)
  ├── "+" button → Quick-Add Observation (1 tap)
  └── Pull-down or tab → My Observations list (1 tap)
```

Consider a sticky bottom navigation bar on phone during active exercise:

```
┌──────────────────┐
│                  │
│  [screen content]│
│                  │
├──────────────────┤
│ MSEL  Obs  📷  ☰ │  ← bottom nav, always visible
└──────────────────┘
```

### Offline Indicator (Phone)

On phone, screen space is limited. The offline/sync indicator should be:
- A thin colored bar at the very top of the viewport (above the app header)
- Green when connected, red when offline, yellow when syncing
- Tappable to expand to full sync status detail
- Does not consume significant vertical space

```
Phone offline indicator:

┌──────────────────┐
│▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓│  ← 4px red bar = offline
│ Exercise Conduct │
│ ...              │
└──────────────────┘
```

---

## Viewport-Specific CSS Patterns

For implementation reference, use these MUI breakpoint queries:

```typescript
// Theme breakpoints (already configured in Phase A)
// xs: 0, sm: 600, md: 768, lg: 1024, xl: 1440

// Phone-specific styles
const useStyles = {
  // Full-screen modal on phone, bottom sheet on tablet+
  observationForm: {
    height: '100vh',              // phone default
    '@media (min-width: 768px)': {
      height: '60vh',             // tablet bottom sheet
      borderRadius: '16px 16px 0 0',
    },
  },
  
  // 2-column gallery on phone, 3-column on tablet+
  photoGrid: {
    gridTemplateColumns: 'repeat(2, 1fr)',
    '@media (min-width: 768px)': {
      gridTemplateColumns: 'repeat(3, 1fr)',
    },
  },
  
  // Larger FAB on phone
  fab: {
    width: 64,
    height: 64,
    '@media (min-width: 768px)': {
      width: 56,
      height: 56,
    },
  },
};
```

---

## iOS-Specific Considerations

Field evaluators using iPhones need these accommodations:

| Concern | Solution |
|---------|----------|
| Safe area insets (notch, home indicator) | Use `env(safe-area-inset-*)` CSS for FAB and bottom nav positioning |
| Safari `100vh` bug (includes address bar) | Use `100dvh` (dynamic viewport height) or `window.innerHeight` for full-screen modals |
| Camera access in PWA | iOS Safari supports camera via `<input capture>` in PWA context. `getUserMedia` support varies by iOS version. |
| SpeechRecognition API | iOS Safari uses `webkitSpeechRecognition`. On-device Siri recognition — works offline. |
| Geolocation in PWA | Requires HTTPS. Works in PWA but may prompt separately from browser. |
| Background location | Not supported in PWA on iOS. Location updates pause when app is backgrounded. Document this limitation for users. |
| IndexedDB storage limits | iOS Safari limits to ~1GB but may evict under storage pressure. Monitor with `navigator.storage.estimate()`. |

---

## Android-Specific Considerations

| Concern | Solution |
|---------|----------|
| Camera access in PWA | Full `getUserMedia` support. `<input capture>` also works. |
| SpeechRecognition API | Chrome uses on-device model on Android. Full offline support. |
| Geolocation in PWA | Works well, including background updates if PWA is in foreground. |
| Back button behavior | Full-screen modals must handle Android back button (close modal, not navigate back). Use `popstate` or `beforeunload` handlers. |
| Keyboard resize | Viewport resizes when keyboard opens. Ensure FAB and save buttons remain visible above keyboard. Use `visualViewport` API if needed. |

---

## Testing Checklist (Phone)

For each 🔴 Critical story, verify on these viewport widths:

| Device | Width | Priority |
|--------|-------|----------|
| iPhone SE (smallest common) | 375px | Required |
| iPhone 14/15 | 390px | Required |
| iPhone 14/15 Pro Max | 430px | Recommended |
| Samsung Galaxy S series | 360px | Required |
| Pixel 7 | 412px | Recommended |

**Test scenarios for each critical story on phone:**
- [ ] All touch targets meet 48px minimum
- [ ] Text is readable without zooming (minimum 16px body text)
- [ ] Forms are usable with on-screen keyboard visible
- [ ] Camera capture works via FAB
- [ ] Bottom sheet / modal is not obscured by keyboard
- [ ] Offline indicator is visible but not intrusive
- [ ] iOS safe area insets are respected
- [ ] Android back button closes modals correctly

---

## Removing the Mobile Blocker

Phase A implemented a mobile blocker component that prevents access below 768px. This must be removed or made conditional for Field Operations features:

**Approach:** Replace the hard block with a conditional check:
- Exercise conduct views (MSEL, observations, photo capture) → **Allow phone access**
- Admin/management views (exercise setup, user management, organization settings) → **Keep tablet+ recommendation** (show a dismissible banner, not a blocker)

```typescript
// Replace hard blocker with context-aware behavior
const isMobileAllowed = isExerciseConductView(currentRoute);

if (viewportWidth < 768 && !isMobileAllowed) {
  return <MobileRecommendationBanner />; // Dismissible, not blocking
}
```

This is a prerequisite for all Field Operations phone work and should be its own implementation task.

---

## Summary

This document provides the responsive guidance needed to implement Field Operations stories on phone form factors without modifying the stories themselves. The key principles:

1. **Phone is a first-class field capture device** — Quick Photo and Quick-Add Observation are phone-native workflows
2. **Full-screen modals replace bottom sheets** on phone for maximum input space
3. **Touch targets increase to 48px** on phone for reliable one-handed use
4. **Director views are tablet-optimized** — functional on phone, designed for larger screens
5. **iOS and Android have platform-specific considerations** that must be tested
6. **The Phase A mobile blocker must be removed** for exercise conduct views

Reference this document in implementation prompts as: `docs/features/field-operations/MOBILE-ADDENDUM.md`
