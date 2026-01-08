# Feature: Notes (Sample Feature)

> This is an example USER_STORIES.md file demonstrating the recommended format for documenting feature requirements.

## Overview

A simple notes feature that allows users to create, view, edit, and delete personal notes. This feature demonstrates all the architectural patterns used in the template.

## User Roles

- **User**: Standard authenticated user who can manage their own notes
- **Admin**: Can view all notes for support purposes

---

## User Stories

### US-001: View My Notes

**As a** user
**I want** to see a list of all my notes
**So that** I can quickly find and access my saved information

**Acceptance Criteria:**
- [ ] Notes are displayed in a card grid layout
- [ ] Each card shows the note title, preview text, and last modified date
- [ ] Notes are sorted by last modified date (newest first)
- [ ] Empty state is shown when user has no notes
- [ ] Loading state is shown while fetching notes

**Technical Notes:**
- Use `useNotes` hook for data fetching
- Implement optimistic UI updates
- Cache notes locally for offline viewing

---

### US-002: Create a Note

**As a** user
**I want** to create a new note
**So that** I can save information for later reference

**Acceptance Criteria:**
- [ ] "New Note" button opens a creation dialog
- [ ] Dialog has fields for title (required) and content
- [ ] Title is limited to 100 characters
- [ ] Content supports basic text (no rich formatting for MVP)
- [ ] Save button creates the note and closes dialog
- [ ] Cancel button discards changes
- [ ] New note appears in list immediately (optimistic update)
- [ ] Toast notification confirms creation

**Technical Notes:**
- Use CobraDialog for the modal
- Use CobraTextField for inputs
- Use CobraSaveButton with loading state
- POST to `/api/notes`

---

### US-003: Edit a Note

**As a** user
**I want** to edit an existing note
**So that** I can update or correct my saved information

**Acceptance Criteria:**
- [ ] Clicking a note card opens edit dialog
- [ ] Dialog is pre-populated with existing note data
- [ ] Changes can be saved or cancelled
- [ ] Updated note appears in list with new modified date
- [ ] Toast notification confirms update

**Technical Notes:**
- Reuse the same dialog component as create
- PUT to `/api/notes/{id}`

---

### US-004: Delete a Note

**As a** user
**I want** to delete a note
**So that** I can remove information I no longer need

**Acceptance Criteria:**
- [ ] Delete button is available on note card (hover state)
- [ ] Confirmation dialog prevents accidental deletion
- [ ] Deleted note is removed from list immediately
- [ ] Toast notification confirms deletion with undo option (5 seconds)
- [ ] Undo restores the note

**Technical Notes:**
- Use soft delete pattern (IsDeleted flag)
- Implement undo with setTimeout
- DELETE to `/api/notes/{id}`

---

### US-005: Real-time Note Updates

**As a** user
**I want** to see notes updated in real-time
**So that** if I have multiple tabs open, they stay in sync

**Acceptance Criteria:**
- [ ] Creating a note in one tab shows it in other tabs
- [ ] Editing a note in one tab updates it in other tabs
- [ ] Deleting a note in one tab removes it from other tabs
- [ ] Connection status indicator shows SignalR state

**Technical Notes:**
- Use SignalR hub for broadcast
- Events: `NoteCreated`, `NoteUpdated`, `NoteDeleted`
- Use `useSignalR` hook

---

## Non-Functional Requirements

### Performance
- Note list should load in < 2 seconds
- Create/Edit operations should feel instant (optimistic UI)

### Security
- Users can only see/edit their own notes
- API endpoints validate user ownership

### Accessibility
- All interactive elements are keyboard accessible
- ARIA labels on buttons and dialogs
- Focus management in dialogs

---

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/notes` | List all notes for current user |
| GET | `/api/notes/{id}` | Get single note |
| POST | `/api/notes` | Create new note |
| PUT | `/api/notes/{id}` | Update note |
| DELETE | `/api/notes/{id}` | Soft delete note |

---

## Data Model

```
Note
├── Id: Guid (PK)
├── UserId: string
├── Title: string (max 100)
├── Content: string (max 10000)
├── CreatedAt: DateTime
├── UpdatedAt: DateTime
├── IsDeleted: bool
└── DeletedAt: DateTime?
```

---

## UI Mockups

*(Add links to Figma, wireframes, or screenshots here)*

---

## Open Questions

1. Should we support rich text formatting in the future?
2. Should notes be shareable with other users?
3. Should we add tags/categories?

---

## Change Log

| Date | Author | Changes |
|------|--------|---------|
| 2024-12-04 | Template | Initial user stories |
