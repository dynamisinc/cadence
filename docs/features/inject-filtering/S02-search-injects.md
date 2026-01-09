# Story: S02 - Search Injects

## User Story

**As a** Controller, Evaluator, or Observer,
**I want** to search for injects by text content,
**So that** I can quickly find specific injects without scrolling through the entire MSEL.

## Context

Text search complements filtering by allowing users to find injects containing specific words or phrases. During a busy exercise, a Controller might need to quickly find the "hospital evacuation" inject. Search should be fast and search across multiple text fields.

## Acceptance Criteria

### Search Interface
- [ ] **Given** I am viewing the MSEL, **when** I look at the toolbar, **then** I see a search input field with placeholder "Search injects..."
- [ ] **Given** I click the search field, **when** it receives focus, **then** the cursor is ready for input
- [ ] **Given** I have a keyboard shortcut preference, **when** I press Ctrl+F or Cmd+F, **then** focus moves to the search field

### Search Behavior
- [ ] **Given** I type in the search field, **when** I enter 2+ characters, **then** results filter in real-time (debounced 300ms)
- [ ] **Given** I type a search term, **when** results display, **then** matching injects are shown
- [ ] **Given** my search matches no injects, **when** results display, **then** I see "No injects found" message

### Search Fields
Search should match text in these inject fields:
- [ ] Title
- [ ] Description
- [ ] From
- [ ] To
- [ ] Expected Action
- [ ] Controller Notes
- [ ] Inject Number (exact match)

### Result Highlighting
- [ ] **Given** search results are displayed, **when** I view matching injects, **then** the matching text is highlighted
- [ ] **Given** match is in Title, **when** I view the list, **then** I see the highlight in the Title column
- [ ] **Given** match is in Description (not shown in list), **when** I view the inject, **then** I see "Match in description" indicator

### Search + Filter Combination
- [ ] **Given** I have filters active, **when** I also search, **then** results match BOTH filter criteria AND search term
- [ ] **Given** I search with filters, **when** I view results count, **then** it shows injects matching both
- [ ] **Given** I clear search, **when** filters are still active, **then** only filters remain applied

### Clear Search
- [ ] **Given** I have entered a search term, **when** I click the X button in search field, **then** search is cleared
- [ ] **Given** I clear search, **when** the field empties, **then** all injects (subject to filters) are shown again

### Search State
- [ ] **Given** I search for a term, **when** I navigate to inject detail and back, **then** my search term is preserved
- [ ] **Given** I refresh the page, **when** it reloads, **then** search is cleared

## Out of Scope

- Advanced search syntax (AND, OR, quotes)
- Search history
- Saved searches
- Fuzzy/approximate matching
- Regular expression search

## Dependencies

- inject-crud/S01: Create Inject (injects to search)
- inject-filtering/S01: Filter Injects (search combines with filters)

## Open Questions

- [ ] Should search be case-sensitive? (Recommend: case-insensitive)
- [ ] Should we support phrase search with quotes?
- [ ] Should search include objective/phase names?
- [ ] Minimum characters before search triggers?

## Domain Terms

| Term | Definition |
|------|------------|
| Search | Text-based matching across inject content fields |
| Highlight | Visual emphasis of matching text in results |
| Debounce | Delay before executing search to avoid excessive calls |

## UI/UX Notes

### Search Input

```
┌─────────────────────────────────────────────────────────────────────────┐
│  [Status ▼] [Phase ▼] [Objective ▼]  │  🔍 Search injects...    [✕]  │
└─────────────────────────────────────────────────────────────────────────┘

With search active:
┌─────────────────────────────────────────────────────────────────────────┐
│  [Status ▼] [Phase ▼] [Objective ▼]  │  🔍 evacuation            [✕]  │
└─────────────────────────────────────────────────────────────────────────┘
│                                                                         │
│  Found 5 injects matching "evacuation"                                 │
└─────────────────────────────────────────────────────────────────────────┘
```

### Search Results with Highlighting

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│  Search: "evacuation"                                        5 results         │
├─────────────────────────────────────────────────────────────────────────────────┤
│  #  │ Time     │ Title                                      │ Status │ Match  │
│ ────┼──────────┼────────────────────────────────────────────┼────────┼─────── │
│  3  │ 09:30 AM │ [Evacuation] order issued                  │ Pending│ Title  │
│  7  │ 10:15 AM │ [Evacuation] routes closed                 │ Pending│ Title  │
│ 12  │ 11:00 AM │ Hospital requests [evacuation] assistance  │ Pending│ Title  │
│ 18  │ 12:30 PM │ Shelter capacity report                    │ Pending│ Descr. │
│ 23  │ 14:00 PM │ Zone B [evacuation] complete              │ Pending│ Title  │
└─────────────────────────────────────────────────────────────────────────────────┘

[text] = highlighted match
"Descr." = match found in Description field (not shown in list)
```

### No Results

```
┌─────────────────────────────────────────────────────────────────────────┐
│  Search: "tsunami"                                        0 results    │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│                          🔍                                            │
│                                                                         │
│              No injects found matching "tsunami"                       │
│                                                                         │
│              Try different search terms or [clear filters]             │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### Keyboard Shortcut Hint

```
┌─────────────────────────────────────────────────────────────────────────┐
│  🔍 Search injects...                                        ⌘F       │
└─────────────────────────────────────────────────────────────────────────┘

Tooltip on hover: "Press Ctrl+F or ⌘F to search"
```

## Technical Notes

- Implement debounced search (300ms delay) to avoid excessive filtering
- Search should be case-insensitive
- Consider search indexing for large MSELs (100+ injects)
- Client-side search for MVP; consider server-side for scale
- Highlight matching using mark.js or similar library
- Store search term in URL query parameter for shareability (optional)
