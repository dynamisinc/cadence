# Story: EM-02-S05 - Welcome Email

**As a** new User,  
**I want** to receive a welcome email after joining an organization,  
**So that** I have confirmation of my account and know how to get started.

## Context

Welcome emails confirm successful registration and provide helpful next steps. This email is sent automatically after a user accepts an organization invitation and creates their account.

## Acceptance Criteria

### Email Trigger

- [ ] **Given** user accepts invitation, **when** account created successfully, **then** welcome email is sent
- [ ] **Given** welcome email, **when** sent, **then** it's sent within 30 seconds of account creation
- [ ] **Given** user already exists (joining second org), **when** accepting invitation, **then** send "Welcome to [Org]" variant

### Email Content

- [ ] **Given** welcome email, **when** received, **then** it addresses user by name
- [ ] **Given** welcome email, **when** received, **then** it confirms organization name they joined
- [ ] **Given** welcome email, **when** received, **then** it includes link to sign in
- [ ] **Given** welcome email, **when** received, **then** it includes brief "getting started" guidance
- [ ] **Given** welcome email, **when** received, **then** it includes link to email preferences

### Organization Context

- [ ] **Given** user joins org with active exercises, **when** welcome email sent, **then** mention exercises available
- [ ] **Given** user assigned role, **when** welcome email sent, **then** mention their role

## Out of Scope

- Onboarding email sequence (multiple follow-up emails)
- Getting started tutorial embedded in email

## Dependencies

- EM-01-S01: ACS Email Configuration
- EM-01-S02: Email Template System
- User registration flow

## Domain Terms

| Term | Definition |
|------|------------|
| Welcome Email | Confirmation email sent after successful registration |

## Effort Estimate

**2 story points** - Template creation, trigger implementation

---

*Feature: EM-02 Organization Invitations*  
*Priority: P0*
