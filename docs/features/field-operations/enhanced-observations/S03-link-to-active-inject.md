# Story: Link Observation to Active Inject

## S03-link-to-active-inject

**As an** Evaluator,
**I want** to quickly link my observation to a recently-fired inject without searching through the entire MSEL,
**So that** my observation is connected to the specific exercise event it relates to, making it directly useful for the After-Action Review.

### Context

When an Evaluator captures an observation, it's almost always in response to something that just happened — an inject was fired, players responded, and the Evaluator is documenting what they saw. Currently, linking an observation to an inject requires searching or scrolling through the full MSEL, which in a large exercise might contain 100+ injects. The Evaluator knows what just happened because they saw the response — they shouldn't have to hunt for the inject that caused it. By surfacing recently-fired injects at the top of the linking interface, we reduce inject selection from a 30-second search to a 2-second tap. For exercises with 50+ injects, this saves significant time and reduces linking errors (selecting the wrong inject).

### Acceptance Criteria

- [ ] **Given** I am creating or editing an observation, **when** I tap the inject link field, **then** I see a list of recently-fired injects (fired within the last 60 minutes of exercise time) displayed first, followed by an option to search all injects
- [ ] **Given** I see the recently-fired inject list, **when** I view each inject, **then** I see: inject number, title, fired time (scenario time), and phase name
- [ ] **Given** I see the recently-fired inject list, **when** I tap an inject, **then** it is selected and the inject link field shows the inject number and title
- [ ] **Given** I want to link to an inject that is not recently fired, **when** I tap "Search all injects" or type in the search field, **then** I can search the full MSEL by inject number, title, or description
- [ ] **Given** I have linked an inject to my observation, **when** I save the observation, **then** the inject reference is stored and visible when viewing the observation
- [ ] **Given** I have linked an inject, **when** I want to change or remove the link, **then** I can tap the linked inject to either select a different one or clear the selection
- [ ] **Given** no injects have been fired yet in the exercise, **when** I tap the inject link field, **then** I see the full inject list with search capability (no "recently fired" section)
- [ ] **Given** I am viewing a saved observation that is linked to an inject, **when** I tap the inject reference, **then** I navigate to the inject detail view
- [ ] **Given** multiple injects were fired close together, **when** I see the recently-fired list, **then** they are ordered by fired time (most recent first)
- [ ] **Given** I am offline, **when** I tap the inject link field, **then** I see recently-fired injects from the locally cached MSEL data (same behavior as online)

### Out of Scope

- Linking an observation to multiple injects simultaneously (one inject per observation; create multiple observations if needed)
- Automatic inject suggestion based on evaluator location or assignment
- Inject detail preview within the observation form (just show number + title)
- Reverse navigation: viewing all observations linked to a specific inject (separate story for inject detail view enhancement)

### Dependencies

- Phase D: Exercise Conduct (inject firing and status tracking must be implemented)
- Phase C: Inject CRUD (inject list and search)
- S01-quick-add-observation (inject link field in the observation form)
- Phase H: Offline sync (locally cached inject data for offline linking)

### Open Questions

- [ ] Should the "recently fired" window be configurable, or fixed at 60 minutes? (Recommendation: fixed at 60 minutes of exercise time initially, revisit if users request adjustment)
- [ ] Should Evaluators be able to link observations to injects that haven't been fired yet (status: Pending)? (Recommendation: yes, but surface fired injects first — an Evaluator might observe pre-positioning that relates to an upcoming inject)
- [ ] Should the inject link suggest injects based on the Evaluator's assigned area or capabilities? (Recommendation: defer — this requires evaluator-inject assignment mapping that may not exist yet)
- [ ] When viewing all observations for an inject (reverse lookup), should this be on the inject detail page or a separate report? (Recommendation: inject detail page, as a collapsible "Observations" section — but that's a separate story)

### Domain Terms

| Term | Definition |
|------|------------|
| Recently-Fired Inject | An inject whose status changed to "Fired" within the last 60 minutes of exercise scenario time. Surfaced as quick-select options when linking observations. |
| Inject Link | A reference from an observation to the specific MSEL inject that prompted the observed activity |
| MSEL (Master Scenario Events List) | The chronologically ordered list of all injects for an exercise |

### UI/UX Notes

```
Inject link field — tap to open selector:

┌─────────────────────────────────────┐
│ Inject: [None selected         ▼]   │
└─────────────────────────────────────┘

Inject selector opened:

┌─────────────────────────────────────┐
│ Link to Inject               ✕      │
├─────────────────────────────────────┤
│ 🔍 Search injects...                │
├─────────────────────────────────────┤
│ RECENTLY FIRED                      │
│ ┌─────────────────────────────────┐ │
│ │ INJ-012 · Evacuation Order     │ │
│ │ Fired 10:45 · Phase 2          │ │
│ └─────────────────────────────────┘ │
│ ┌─────────────────────────────────┐ │
│ │ INJ-011 · Road Closure         │ │
│ │ Fired 10:32 · Phase 2          │ │
│ └─────────────────────────────────┘ │
│ ┌─────────────────────────────────┐ │
│ │ INJ-010 · Shelter Activation   │ │
│ │ Fired 10:15 · Phase 1          │ │
│ └─────────────────────────────────┘ │
├─────────────────────────────────────┤
│ ALL INJECTS                         │
│ ┌─────────────────────────────────┐ │
│ │ INJ-001 · Initial Notification │ │
│ │ Fired 09:00 · Phase 1          │ │
│ └─────────────────────────────────┘ │
│ │ ...                             │ │
└─────────────────────────────────────┘

After selection:

┌─────────────────────────────────────┐
│ Inject: [INJ-012 Evacuation Or… ✕]  │
└─────────────────────────────────────┘
```

- Inject selector opens as a bottom sheet or dropdown panel
- "Recently Fired" section highlighted with a subtle background color or divider
- Search filters both recently-fired and all injects as the user types
- Selected inject shows inject number and truncated title with a clear (✕) button
- Fired time shown in scenario time, not wall clock time
- Touch targets at least 48px tall for each inject row

### Technical Notes

- Recently-fired injects query: filter injects where `status = Fired` and `firedAt >= exerciseClock.currentTime - 60 minutes`
- If offline, query the local IndexedDB inject cache with the same filter logic
- Search implementation: client-side filter on the cached inject list (for exercises under 500 injects, no server roundtrip needed)
- For very large MSELs (500+ injects), consider server-side search with debounced API calls
- Inject link stored as `injectId` foreign key on the observation entity
- Consider preloading recently-fired injects when the observation form opens to avoid a visible loading delay
