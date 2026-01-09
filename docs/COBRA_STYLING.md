# COBRA Styling Guide

> **Version:** 1.0.0
> **Last Updated:** 2025-12-04

This document describes the COBRA styling system used throughout Cadence applications. All frontend code must follow these guidelines for visual consistency.

---

## Table of Contents

1. [Overview](#overview)
2. [Critical Rules](#critical-rules)
3. [COBRA Components](#cobra-components)
4. [Theme Colors](#theme-colors)
5. [Spacing & Layout](#spacing--layout)
6. [Typography](#typography)
7. [Common Patterns](#common-patterns)
8. [Component Reference](#component-reference)

---

## Overview

COBRA (COnsistent BRand Architecture) is Cadence's standardized styling system built on Material-UI (MUI). It ensures visual consistency across all Cadence applications while maintaining accessibility standards.

### Design Principles

1. **Consistency**: Same visual language across all applications
2. **Accessibility**: WCAG 2.1 AA compliant colors and contrast
3. **Efficiency**: Pre-built components reduce development time
4. **Maintainability**: Centralized theme for easy updates

---

## Critical Rules

### NEVER Use Raw MUI Components

This is the most important rule. Always use COBRA components instead of raw MUI.

```typescript
// ❌ NEVER DO THIS
import { Button, TextField, Dialog } from '@mui/material';

<Button variant="contained" color="primary">Save</Button>
<TextField label="Title" />
```

```typescript
// ✅ ALWAYS DO THIS
import {
  CobraPrimaryButton,
  CobraTextField,
  CobraDialog
} from '@/theme/styledComponents';

<CobraPrimaryButton onClick={onSave}>Save</CobraPrimaryButton>
<CobraTextField label="Title" />
```

### NEVER Hardcode Colors

```typescript
// ❌ NEVER DO THIS
<Box sx={{ backgroundColor: '#0020C2', color: '#FFFFFF' }}>

// ✅ ALWAYS DO THIS
import { useTheme } from '@mui/material/styles';

const theme = useTheme();
<Box sx={{
  backgroundColor: theme.palette.buttonPrimary.main,
  color: theme.palette.buttonPrimary.contrastText
}}>
```

### NEVER Hardcode Spacing

```typescript
// ❌ NEVER DO THIS
<Stack spacing={2} sx={{ padding: '20px' }}>

// ✅ ALWAYS DO THIS
import CobraStyles from '@/theme/CobraStyles';

<Stack
  spacing={CobraStyles.Spacing.FormFields}
  sx={{ padding: CobraStyles.Padding.MainWindow }}
>
```

---

## COBRA Components

### Button Components

| Component | Use Case | Appearance |
|-----------|----------|------------|
| `CobraPrimaryButton` | Primary actions (Save, Create, Submit) | Filled cobalt blue |
| `CobraSecondaryButton` | Secondary actions | Outlined cobalt blue |
| `CobraDeleteButton` | Destructive actions | Filled red with delete icon |
| `CobraLinkButton` | Text-only actions (Cancel, Back) | Text only, no background |

#### Usage Examples

```typescript
import {
  CobraPrimaryButton,
  CobraSecondaryButton,
  CobraDeleteButton,
  CobraLinkButton
} from '@/theme/styledComponents';

// Primary action
<CobraPrimaryButton onClick={handleSave}>
  Save Changes
</CobraPrimaryButton>

// Secondary action
<CobraSecondaryButton onClick={handleExport}>
  Export
</CobraSecondaryButton>

// Delete action (includes trash icon automatically)
<CobraDeleteButton onClick={handleDelete}>
  Delete
</CobraDeleteButton>

// Cancel/dismiss action
<CobraLinkButton onClick={handleCancel}>
  Cancel
</CobraLinkButton>

// With loading state (for CobraPrimaryButton)
<CobraPrimaryButton onClick={handleSave} disabled={loading}>
  {loading ? 'Saving...' : 'Save'}
</CobraPrimaryButton>

// With start icon
<CobraPrimaryButton startIcon={<AddIcon />} onClick={handleCreate}>
  New Note
</CobraPrimaryButton>
```

### Input Components

| Component | Use Case |
|-----------|----------|
| `CobraTextField` | All text inputs (single-line and multiline) |

#### Usage Examples

```typescript
import { CobraTextField } from '@/theme/styledComponents';

// Single-line input
<CobraTextField
  label="Title"
  value={title}
  onChange={(e) => setTitle(e.target.value)}
  fullWidth
  required
/>

// Multiline input
<CobraTextField
  label="Content"
  value={content}
  onChange={(e) => setContent(e.target.value)}
  multiline
  rows={4}
  fullWidth
/>

// With error state
<CobraTextField
  label="Email"
  value={email}
  error={!!emailError}
  helperText={emailError}
  fullWidth
/>

// With placeholder
<CobraTextField
  label="Search"
  placeholder="Search notes..."
  fullWidth
/>
```

### Button Placement in Dialogs

Standard button order in dialog actions (left to right):
1. Cancel (CobraLinkButton)
2. Delete (CobraDeleteButton) - if applicable
3. Primary action (CobraPrimaryButton)

```typescript
<DialogActions>
  <CobraLinkButton onClick={onCancel}>
    Cancel
  </CobraLinkButton>
  <CobraDeleteButton onClick={onDelete}>
    Delete
  </CobraDeleteButton>
  <CobraPrimaryButton onClick={onSave}>
    Save
  </CobraPrimaryButton>
</DialogActions>
```

---

## Theme Colors

### Accessing Theme Colors

```typescript
import { useTheme } from '@mui/material/styles';

const MyComponent = () => {
  const theme = useTheme();

  return (
    <Box sx={{
      backgroundColor: theme.palette.buttonPrimary.main,
      color: theme.palette.buttonPrimary.contrastText,
      borderColor: theme.palette.divider,
    }}>
      Content
    </Box>
  );
};
```

### Color Palette

| Palette Key | Color | Use Case |
|-------------|-------|----------|
| `buttonPrimary.main` | Cobalt Blue (#0020C2) | Primary buttons, links |
| `buttonPrimary.contrastText` | White (#FFFFFF) | Text on primary buttons |
| `buttonDelete.main` | Lava Red (#E42217) | Delete buttons, error states |
| `buttonDelete.contrastText` | White (#FFFFFF) | Text on delete buttons |
| `background.default` | White (#FFFFFF) | Page background |
| `background.paper` | White (#FFFFFF) | Card/dialog background |
| `text.primary` | Dark gray (#212121) | Primary text |
| `text.secondary` | Medium gray (#757575) | Secondary text |
| `divider` | Light gray (#E0E0E0) | Borders, dividers |

### Semantic Colors

```typescript
// Progress bar colors (from cobraTheme)
import { getProgressColor } from '@/theme/cobraTheme';

const progressColor = getProgressColor(percentage);
// Returns: red (0-33%), yellow (34-66%), green (67-100%)

// Status chip colors
import { getStatusChipColor } from '@/theme/cobraTheme';

const chipColor = getStatusChipColor(status);
// Returns appropriate color based on status
```

---

## Spacing & Layout

### CobraStyles Constants

```typescript
import CobraStyles from '@/theme/CobraStyles';

// Available constants:
CobraStyles.Spacing.FormFields    // 12px - Between form fields
CobraStyles.Spacing.AfterSeparator // 18px - After dividers

CobraStyles.Padding.MainWindow    // 18px - Page content padding
CobraStyles.Padding.DialogContent // 15px - Dialog interior padding
```

### Usage in Components

```typescript
import { Stack, Box } from '@mui/material';
import CobraStyles from '@/theme/CobraStyles';

// Form layout
<Stack spacing={CobraStyles.Spacing.FormFields}>
  <CobraTextField label="Title" fullWidth />
  <CobraTextField label="Description" fullWidth />
</Stack>

// Page content
<Box padding={CobraStyles.Padding.MainWindow}>
  {/* Page content */}
</Box>

// Dialog content
<DialogContent sx={{ padding: CobraStyles.Padding.DialogContent }}>
  {/* Dialog content */}
</DialogContent>
```

### MUI Spacing Units

When `CobraStyles` constants aren't applicable, use MUI's spacing system:

```typescript
// MUI spacing: 1 unit = 8px
<Box sx={{ mt: 2, mb: 1, px: 3 }}>
  {/* mt: 16px, mb: 8px, px: 24px */}
</Box>

// Or use theme.spacing()
<Box sx={{ margin: theme.spacing(2) }}>
  {/* 16px margin */}
</Box>
```

---

## Typography

### Theme Typography

The COBRA theme uses Roboto font family with pre-configured variants:

```typescript
// Available variants (use via Typography component)
<Typography variant="h1">Heading 1</Typography>
<Typography variant="h2">Heading 2</Typography>
<Typography variant="h3">Heading 3</Typography>
<Typography variant="h4">Heading 4</Typography>
<Typography variant="h5">Heading 5</Typography>
<Typography variant="h6">Heading 6</Typography>
<Typography variant="body1">Body text</Typography>
<Typography variant="body2">Secondary body text</Typography>
<Typography variant="caption">Caption text</Typography>
<Typography variant="button">Button text</Typography>
```

### Common Typography Patterns

```typescript
// Page title
<Typography variant="h5" component="h1" gutterBottom>
  Notes
</Typography>

// Card title
<Typography variant="h6" component="h2">
  My Note Title
</Typography>

// Body content
<Typography variant="body1">
  This is the main content text.
</Typography>

// Secondary text
<Typography variant="body2" color="text.secondary">
  Last updated: 2 hours ago
</Typography>

// Error text
<Typography variant="body2" color="error">
  This field is required
</Typography>
```

---

## Common Patterns

### Form Layout

```typescript
import { Stack, DialogActions } from '@mui/material';
import { CobraTextField, CobraPrimaryButton, CobraLinkButton } from '@/theme/styledComponents';
import CobraStyles from '@/theme/CobraStyles';

const NoteForm = ({ onSubmit, onCancel }) => {
  return (
    <Stack spacing={CobraStyles.Spacing.FormFields}>
      <CobraTextField
        label="Title"
        fullWidth
        required
      />

      <CobraTextField
        label="Content"
        multiline
        rows={4}
        fullWidth
      />

      <DialogActions sx={{ px: 0, pt: 2 }}>
        <CobraLinkButton onClick={onCancel}>
          Cancel
        </CobraLinkButton>
        <CobraPrimaryButton onClick={onSubmit}>
          Save
        </CobraPrimaryButton>
      </DialogActions>
    </Stack>
  );
};
```

### Dialog Pattern

```typescript
import { Dialog, DialogTitle, DialogContent, DialogActions, Stack } from '@mui/material';
import { CobraTextField, CobraPrimaryButton, CobraLinkButton } from '@/theme/styledComponents';
import CobraStyles from '@/theme/CobraStyles';

const CreateNoteDialog = ({ open, onClose, onCreate }) => {
  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>Create Note</DialogTitle>

      <DialogContent>
        <Stack spacing={CobraStyles.Spacing.FormFields} sx={{ mt: 1 }}>
          <CobraTextField
            label="Title"
            fullWidth
            required
            autoFocus
          />

          <CobraTextField
            label="Content"
            multiline
            rows={4}
            fullWidth
          />
        </Stack>
      </DialogContent>

      <DialogActions>
        <CobraLinkButton onClick={onClose}>
          Cancel
        </CobraLinkButton>
        <CobraPrimaryButton onClick={onCreate}>
          Create Note
        </CobraPrimaryButton>
      </DialogActions>
    </Dialog>
  );
};
```

### Page Layout

```typescript
import { Box, Typography, Stack } from '@mui/material';
import { CobraPrimaryButton } from '@/theme/styledComponents';
import CobraStyles from '@/theme/CobraStyles';
import AddIcon from '@mui/icons-material/Add';

const NotesPage = () => {
  return (
    <Box padding={CobraStyles.Padding.MainWindow}>
      {/* Page header */}
      <Stack
        direction="row"
        justifyContent="space-between"
        alignItems="center"
        marginBottom={3}
      >
        <Typography variant="h5" component="h1">
          Notes
        </Typography>

        <CobraPrimaryButton startIcon={<AddIcon />} onClick={handleCreate}>
          New Note
        </CobraPrimaryButton>
      </Stack>

      {/* Page content */}
      <Box>
        {/* Content here */}
      </Box>
    </Box>
  );
};
```

### Card Pattern

```typescript
import { Card, CardContent, CardActions, Typography, Box } from '@mui/material';
import { CobraPrimaryButton, CobraDeleteButton } from '@/theme/styledComponents';

const NoteCard = ({ note, onEdit, onDelete }) => {
  return (
    <Card>
      <CardContent>
        <Typography variant="h6" gutterBottom>
          {note.title}
        </Typography>

        <Typography variant="body2" color="text.secondary">
          {note.content}
        </Typography>
      </CardContent>

      <CardActions sx={{ justifyContent: 'flex-end' }}>
        <CobraDeleteButton size="small" onClick={() => onDelete(note.id)}>
          Delete
        </CobraDeleteButton>
        <CobraPrimaryButton size="small" onClick={() => onEdit(note.id)}>
          Edit
        </CobraPrimaryButton>
      </CardActions>
    </Card>
  );
};
```

### Loading State

```typescript
import { Box, CircularProgress, Typography } from '@mui/material';

// Full page loading
const LoadingPage = () => (
  <Box
    display="flex"
    justifyContent="center"
    alignItems="center"
    minHeight="50vh"
  >
    <CircularProgress />
  </Box>
);

// Inline loading
const InlineLoading = () => (
  <Box display="flex" alignItems="center" gap={1}>
    <CircularProgress size={20} />
    <Typography variant="body2">Loading...</Typography>
  </Box>
);
```

### Error State

```typescript
import { Box, Typography } from '@mui/material';
import { CobraPrimaryButton } from '@/theme/styledComponents';

const ErrorDisplay = ({ error, onRetry }) => (
  <Box textAlign="center" py={4}>
    <Typography variant="h6" color="error" gutterBottom>
      Something went wrong
    </Typography>
    <Typography variant="body2" color="text.secondary" paragraph>
      {error}
    </Typography>
    {onRetry && (
      <CobraPrimaryButton onClick={onRetry}>
        Try Again
      </CobraPrimaryButton>
    )}
  </Box>
);
```

### Empty State

```typescript
import { Box, Typography } from '@mui/material';
import { CobraPrimaryButton } from '@/theme/styledComponents';
import AddIcon from '@mui/icons-material/Add';

const EmptyState = ({ onCreate }) => (
  <Box textAlign="center" py={4}>
    <Typography variant="h6" gutterBottom>
      No notes yet
    </Typography>
    <Typography variant="body2" color="text.secondary" paragraph>
      Create your first note to get started
    </Typography>
    <CobraPrimaryButton startIcon={<AddIcon />} onClick={onCreate}>
      Create Note
    </CobraPrimaryButton>
  </Box>
);
```

---

## Component Reference

### CobraPrimaryButton

Primary action button with cobalt blue background.

| Prop | Type | Default | Description |
|------|------|---------|-------------|
| `children` | ReactNode | required | Button text |
| `onClick` | function | - | Click handler |
| `disabled` | boolean | false | Disable state |
| `startIcon` | ReactNode | - | Icon before text |
| `endIcon` | ReactNode | - | Icon after text |
| `size` | 'small' \| 'medium' \| 'large' | 'medium' | Button size |

### CobraSecondaryButton

Secondary action button with outlined style.

Same props as CobraPrimaryButton.

### CobraDeleteButton

Destructive action button with red background and delete icon.

Same props as CobraPrimaryButton. Automatically includes delete icon.

### CobraLinkButton

Text-only button for cancel/dismiss actions.

Same props as CobraPrimaryButton.

### CobraTextField

Text input field with COBRA styling.

| Prop | Type | Default | Description |
|------|------|---------|-------------|
| `label` | string | - | Input label |
| `value` | string | - | Input value |
| `onChange` | function | - | Change handler |
| `error` | boolean | false | Error state |
| `helperText` | string | - | Helper/error text |
| `fullWidth` | boolean | false | Full width input |
| `required` | boolean | false | Required field |
| `multiline` | boolean | false | Textarea mode |
| `rows` | number | 1 | Textarea rows |
| `placeholder` | string | - | Placeholder text |
| `disabled` | boolean | false | Disable state |

---

## Validation Checklist

Before submitting code, verify:

- [ ] No raw MUI Button, TextField, or Dialog imports
- [ ] All buttons use appropriate COBRA component
- [ ] No hardcoded color values (hex, rgb, etc.)
- [ ] No hardcoded spacing values
- [ ] CobraStyles constants used for padding/spacing
- [ ] Typography uses semantic variants
- [ ] Dialog follows standard button order
- [ ] Loading and error states implemented
