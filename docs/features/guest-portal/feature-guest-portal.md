# Feature: External Stakeholder Guest Portal

**Parent Epic:** Collaborative MSEL Review
**Priority:** P0 — Standalone differentiator for multi-agency exercises
**Phase:** J-2 (after Review Mode; shares auth infrastructure)

---

## Description

Partner organization representatives can review the injects in their lane, add comments, and formally approve their portion of the MSEL — without creating a Cadence account. The Exercise Director generates a time-limited, organization-scoped guest link and tracks response status across all invited organizations from a single dashboard. For regulated industries and multi-agency exercises, the formal approval record replaces an email chain with a documented, defensible sign-off.

---

## Domain Context

Multi-agency exercises involve organizations that will never become Cadence subscribers: hospitals, utilities, private sector partners, federal liaisons, National Guard units. Requiring these participants to create accounts is an adoption-killer. But their review matters — often legally. A hospital emergency manager approving their lane in a HEALTHCARE coalition exercise may be satisfying an accreditation requirement. A utility partner approving their lane may be satisfying NERC CIP exercise documentation standards.

The Guest Portal solves this by treating review participation as a transactional event rather than an ongoing relationship. The guest gets exactly what they need for the specific task, and nothing else.

---

## Domain Terms

| Term | Definition |
|------|------------|
| Guest Reviewer | An external stakeholder who reviews the MSEL via a time-limited link without a Cadence account |
| Guest Link | A secure, organization-scoped URL that grants read and comment access to a filtered MSEL view |
| Lane | The subset of injects assigned to a specific player organization |
| Formal Approval | An explicit, recorded acknowledgment from an authorized representative that their lane is reviewed and accepted |
| Review Window | The time period during which a guest link is valid for access and comment |

---

## User Stories

---

### Story GP-1: Generate a Guest Review Link

**As an** Exercise Director or Planner,
**I want** to generate a scoped guest review link for a partner organization,
**So that** their representative can review only their injects without needing a Cadence account.

#### Acceptance Criteria

- [ ] **Given** I am on the MSEL view with Director or Planner role, **when** I click "Invite Guest Reviewer", **then** a dialog opens where I configure the guest link
- [ ] **Given** the link configuration dialog is open, **when** I fill it in, **then** I provide: organization name (free text or from participant list), the reviewer's name and email (for the notification), review window (open date, close date), and whether formal approval is required
- [ ] **Given** I submit the configuration, **when** the link is generated, **then** Cadence creates a unique, signed token URL and sends an invitation email via ACS to the reviewer's address
- [ ] **Given** the link is generated, **when** I view the Guest Reviewers list, **then** I see the organization, reviewer name, status (Pending / Opened / Commented / Approved), and link expiry
- [ ] **Given** I need to revoke access, **when** I click "Revoke Link" on a guest entry, **then** the token is invalidated immediately and any subsequent attempt to use the link shows an "Access expired" message
- [ ] **Given** the review window closes (expiry date passes), **when** the guest attempts to open the link, **then** they see a message that the review window has closed and are directed to contact the exercise planner

#### UI/UX Notes

```
┌─────────────────────────────────────────────────────────────────┐
│ Invite Guest Reviewer                                            │
│ ─────────────────────────────────────────────────────────────── │
│ Organization:  [County Health Department                       ] │
│ Reviewer Name: [Jane Doe                                       ] │
│ Email:         [jdoe@county.gov                                ] │
│                                                                  │
│ Review Window: [03/15/2026     ] to [03/22/2026     ]           │
│                                                                  │
│ Scoped to:     ● Their organization's injects only              │
│                ○ Full MSEL (read-only)                           │
│                                                                  │
│ Require formal approval?   ● Yes  ○ No                          │
│                                                                  │
│ [Cancel]                          [Send Invitation]              │
└─────────────────────────────────────────────────────────────────┘
```

---

### Story GP-2: Access the Guest Review Portal (No Login)

**As a** Guest Reviewer,
**I want** to open my review link and immediately see the injects assigned to my organization,
**So that** I can complete my review without IT involvement, account setup, or training.

#### Context
The guest experience must be frictionless above all else. The link opens directly to a focused, clearly-labeled review view. No login screen. No "create an account" prompt. No Cadence branding confusion. The page should immediately communicate: here is what you are reviewing, here is what you need to do, here is when it is due.

#### Acceptance Criteria

- [ ] **Given** a guest opens their review link, **when** the page loads, **then** they see a welcoming header with: exercise name, their organization name, reviewer name, review window close date, and a clear task description
- [ ] **Given** the guest portal loads, **when** the guest views the inject list, **then** they see only injects assigned to their organization — no other organization's injects are visible
- [ ] **Given** the guest views an inject, **when** they click on it, **then** they see the full inject detail: description, scheduled time, phase, expected actions, evaluation criteria
- [ ] **Given** the guest link is valid, **when** the guest accesses the portal, **then** their first access is recorded with a timestamp (so the Director can see "opened")
- [ ] **Given** the guest is on a mobile browser, **when** they view the portal, **then** the layout is fully responsive and usable on a phone screen
- [ ] **Given** the guest link has expired or been revoked, **when** they open the link, **then** they see a clear, friendly message explaining why and who to contact

#### UI/UX Notes

```
┌─────────────────────────────────────────────────────────────────┐
│  🔵  CADENCE  │  MSEL Review — Hurricane Response FSE 2026       │
│ ─────────────────────────────────────────────────────────────── │
│  Reviewing as:  County Health Department                         │
│  Prepared for:  Jane Doe                                         │
│  Review closes: March 22, 2026 at 5:00 PM EDT                   │
│                                                                  │
│  Please review the injects below and add comments on anything   │
│  that needs clarification or revision. When done, click         │
│  "Approve Lane" to formally confirm your review.                │
│ ─────────────────────────────────────────────────────────────── │
│  Injects for County Health Department  (12 injects)             │
│                                                                  │
│  Phase 1 — Initial Response                                      │
│  H+0:45  Mass casualty notification received        [Comment]   │
│  H+1:00  Request for mobile medical unit issued     [Comment]   │
│  H+1:30  Field triage protocols activated           [Comment]   │
│                                                                  │
│  Phase 2 — Stabilization                                         │
│  H+2:00  Shelter capacity assessment requested      [Comment]   │
│  ...                                                             │
│                                                                  │
│                              [ Approve Lane ✓ ]                  │
└─────────────────────────────────────────────────────────────────┘
```

---

### Story GP-3: Add Comments as a Guest Reviewer

**As a** Guest Reviewer,
**I want** to add comments to specific injects in my lane,
**So that** my feedback is captured against the exact inject it refers to without any technical friction.

#### Acceptance Criteria

- [ ] **Given** I am in the guest portal, **when** I click "Comment" on an inject, **then** an inline comment form opens below that inject
- [ ] **Given** the comment form is open, **when** I type a comment and click "Submit", **then** my comment is saved with my name, organization, and timestamp — no account required
- [ ] **Given** I submit a comment, **when** a Planner or Director views that inject in the main Cadence MSEL view, **then** my comment appears in the inject's comment thread, attributed to "[Name] — [Organization] (Guest)"
- [ ] **Given** I have submitted a comment, **when** the Planner replies, **then** my portal view updates to show the reply with a visual "New" indicator the next time I open the link
- [ ] **Given** I submit a comment, **when** I return to the portal later via the same link, **then** my previous comments are still visible

---

### Story GP-4: Formally Approve a Lane

**As a** Guest Reviewer,
**I want** to formally approve my organization's lane when I have completed my review,
**So that** the Exercise Director has documented confirmation that my organization has reviewed and accepted their role in the exercise.

#### Context
This is the feature that makes the Guest Portal valuable for regulated industries. For healthcare coalition exercises (HPP/ASPR accreditation), utilities (NERC CIP), and nuclear power (NRC exercise requirements), a documented approval from an authorized representative is a compliance artifact, not just a courtesy.

#### Acceptance Criteria

- [ ] **Given** I am in the guest portal and have reviewed all injects, **when** I click "Approve Lane", **then** I am shown a confirmation dialog summarizing: exercise name, my organization, inject count reviewed, my name
- [ ] **Given** the approval dialog is open, **when** I confirm, **then** I enter my full name and title (required) and click "Submit Approval"
- [ ] **Given** I submit my approval, **when** it is recorded, **then** the portal displays a confirmation with a reference number and timestamp, and I receive a confirmation email
- [ ] **Given** I submit my approval, **when** the Exercise Director views the Guest Reviewers dashboard, **then** my organization's status updates to "Approved ✓" with my name, title, and timestamp
- [ ] **Given** I approved but then discover an issue, **when** I click "Withdraw Approval", **then** my approval is revoked and I am returned to comment-only status; the Director is notified
- [ ] **Given** I attempt to approve but I have injects with open unresolved conflicts flagged, **when** I click "Approve Lane", **then** I receive a warning listing flagged injects and must explicitly acknowledge them before approving

---

### Story GP-5: Guest Reviewer Tracking Dashboard

**As an** Exercise Director,
**I want** to see a consolidated view of all guest reviewer activity across all invited organizations,
**So that** I can track who has responded, who has not, and what feedback is outstanding — from one place.

#### Acceptance Criteria

- [ ] **Given** I am on the MSEL view, **when** I click "Guest Reviewers", **then** I see a panel listing all invited organizations with: reviewer name, email, status, last access time, comment count, approval status
- [ ] **Given** I view the dashboard, **when** I identify organizations that have not opened their link within 48 hours of invitation, **then** I can select them and click "Send Reminder" to trigger a reminder email via ACS
- [ ] **Given** all organizations have approved, **when** I view the dashboard, **then** a "All Lanes Approved ✓" indicator appears, and this status is displayed in the MSEL header
- [ ] **Given** the review window closes, **when** I view the dashboard, **then** any non-responding organizations are flagged "No Response" in red
- [ ] **Given** I need a record for the exercise file, **when** I click "Export Approval Record", **then** I receive a PDF listing: all invited organizations, reviewer name and title, approval status, approval timestamp, and comment count

#### UI/UX Notes

```
┌──────────────────────────────────────────────────────────────────────┐
│ Guest Reviewers — Hurricane Response FSE 2026                         │
│ Review window: Mar 15–22, 2026                         [+ Invite]    │
├────────────────────────┬──────────┬──────────┬──────┬────────────────┤
│ Organization           │ Reviewer │ Status   │ Cmts │ Approved       │
├────────────────────────┼──────────┼──────────┼──────┼────────────────┤
│ County Health Dept.    │ J. Doe   │ Opened   │  3   │ —              │
│ Red Cross Chapter      │ M. Green │ Approved │  1   │ ✓ Mar 18 2:14p │
│ County Fire Rescue     │ T. Burns │ Pending  │  0   │ —  [Remind]    │
│ State EOC Liaison      │ K. Ross  │ Approved │  0   │ ✓ Mar 17 9:02a │
│ National Guard J3      │ —        │ No Link  │  —   │ —  [Invite]    │
└────────────────────────┴──────────┴──────────┴──────┴────────────────┘
  2 of 4 lanes approved.   [Export Approval Record PDF]
```

---

## Security Considerations

| Concern | Approach |
|---------|----------|
| Link token security | Signed JWT with exercise ID, org scope, expiry — not guessable |
| Token storage | Stored as hashed value; original token not recoverable after generation |
| Data scope enforcement | Server-side filter on every request — guest token scope validated on API, not just UI |
| PII in guest records | Guest name/email stored for the exercise record; not used for marketing or shared across organizations |
| Rate limiting | Guest endpoints rate-limited per token to prevent enumeration |
| Audit log | Every guest access, comment, and approval is written to the audit trail |

---

## API Endpoints Required

| Method | Route | Description |
|--------|-------|-------------|
| POST | /exercises/{id}/guest-links | Generate a guest review link |
| GET | /exercises/{id}/guest-links | List all guest links for an exercise |
| DELETE | /guest-links/{token} | Revoke a guest link |
| GET | /guest/{token} | Guest portal entry point — returns scoped exercise data |
| GET | /guest/{token}/injects | Get lane-scoped inject list for guest |
| POST | /guest/{token}/comments | Post a comment as a guest |
| POST | /guest/{token}/approve | Submit formal lane approval |
| DELETE | /guest/{token}/approve | Withdraw approval |
| POST | /exercises/{id}/guest-links/remind | Send reminder emails to non-responding guests |
| GET | /exercises/{id}/guest-links/export | Export approval record PDF |

---

## Dependencies

- ACS email integration (for invitation and reminder emails)
- JWT token infrastructure (extend existing auth system — guest tokens are separate from user auth)
- PDF export utility (for approval record export)
- Comment threading (Feature 2 of the Collaborative Review epic)
