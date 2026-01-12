# Coding Standards

> **Version:** 1.0.0
> **Last Updated:** 2025-01-09

This document defines the coding standards and conventions for the Cadence template. All code contributions should follow these guidelines.

---

## Table of Contents

1. [General Principles](#general-principles)
2. [C# / .NET Standards](#c--net-standards)
3. [TypeScript / React Standards](#typescript--react-standards)
4. [Testing Standards](#testing-standards)
5. [Git Conventions](#git-conventions)
6. [Documentation Standards](#documentation-standards)

---

## General Principles

### Code Quality
- **Readability over cleverness**: Code is read more often than written
- **Single Responsibility**: Each function/class should do one thing well
- **DRY (Don't Repeat Yourself)**: Extract common patterns, but don't over-abstract
- **YAGNI (You Aren't Gonna Need It)**: Don't add functionality until needed
- **Fail Fast**: Validate inputs early and throw meaningful errors

### Consistency
- Follow existing patterns in the codebase
- Use automated formatters (Prettier for TypeScript, dotnet format for C#)
- Use linters and address all warnings

---

## C# / .NET Standards

### File Organization

```
Features/
└── FeatureName/
    ├── Functions/           # Azure Function triggers
    │   └── FeatureFunction.cs
    ├── Models/
    │   ├── Entities/        # Database entities
    │   │   └── Feature.cs
    │   └── DTOs/            # Data transfer objects
    │       └── FeatureDto.cs
    ├── Services/            # Business logic
    │   ├── IFeatureService.cs
    │   └── FeatureService.cs
    └── Mappers/             # Entity ↔ DTO mapping
        └── FeatureMapper.cs
```

### Naming Conventions

| Element | Convention | Example |
|---------|------------|---------|
| Namespace | PascalCase, match folder | `Cadence.Core.Features.Exercises` |
| Class | PascalCase, noun | `NotesService` |
| Interface | IPascalCase | `INotesService` |
| Method | PascalCase, verb | `GetAllAsync()` |
| Async method | Suffix with Async | `CreateNoteAsync()` |
| Property | PascalCase | `public string Title { get; set; }` |
| Private field | _camelCase | `private readonly AppDbContext _context;` |
| Parameter | camelCase | `public void Create(string title)` |
| Local variable | camelCase | `var result = await service.GetAsync();` |
| Constant | PascalCase | `public const int MaxLength = 200;` |

### Code Style

#### Using Directives
```csharp
// Global usings in GlobalUsings.cs
global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Threading.Tasks;
global using Microsoft.EntityFrameworkCore;
global using Microsoft.Extensions.Logging;

// File-scoped namespaces (C# 10+)
namespace Cadence.Core.Features.Exercises.Services;

public class ExerciseService { }
```

#### Classes and Methods
```csharp
public class NotesService : INotesService
{
    // 1. Private fields
    private readonly AppDbContext _context;
    private readonly ILogger<NotesService> _logger;

    // 2. Constructor
    public NotesService(AppDbContext context, ILogger<NotesService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // 3. Public methods (interface implementation)
    public async Task<List<NoteDto>> GetAllAsync()
    {
        _logger.LogInformation("Fetching all notes");

        var notes = await _context.Notes
            .AsNoTracking()
            .OrderByDescending(n => n.UpdatedAt)
            .ToListAsync();

        return notes.Select(NoteMapper.ToDto).ToList();
    }

    // 4. Private helper methods
    private void ValidateNote(Note note)
    {
        if (string.IsNullOrWhiteSpace(note.Title))
            throw new ArgumentException("Title is required", nameof(note));
    }
}
```

#### Async/Await
```csharp
// ✅ GOOD: Proper async pattern
public async Task<NoteDto?> GetByIdAsync(string id)
{
    var note = await _context.Notes.FindAsync(id);
    return note is null ? null : NoteMapper.ToDto(note);
}

// ❌ BAD: Blocking on async
public NoteDto? GetById(string id)
{
    var note = _context.Notes.FindAsync(id).Result; // Blocks!
    return note is null ? null : NoteMapper.ToDto(note);
}

// ❌ BAD: Unnecessary async
public async Task<int> GetCount()
{
    return await Task.FromResult(_items.Count); // Just return directly
}
```

#### Entity Framework
```csharp
// ✅ GOOD: AsNoTracking for read-only queries
var notes = await _context.Notes
    .AsNoTracking()
    .Where(n => n.IsActive)
    .ToListAsync();

// ✅ GOOD: Explicit includes
var note = await _context.Notes
    .Include(n => n.Tags)
    .Include(n => n.Author)
    .FirstOrDefaultAsync(n => n.Id == id);

// ✅ GOOD: Select only needed fields
var titles = await _context.Notes
    .Select(n => new { n.Id, n.Title })
    .ToListAsync();

// ❌ BAD: Loading all entities then filtering in memory
var activeNotes = _context.Notes.ToList().Where(n => n.IsActive);
```

#### Error Handling
```csharp
// Let middleware handle exceptions
public async Task<NoteDto> CreateAsync(CreateNoteDto dto)
{
    if (string.IsNullOrWhiteSpace(dto.Title))
        throw new ArgumentException("Title is required", nameof(dto));

    var note = new Note
    {
        Title = dto.Title,
        Content = dto.Content
    };

    _context.Notes.Add(note);
    await _context.SaveChangesAsync();

    _logger.LogInformation("Created note {NoteId} with title {Title}", note.Id, note.Title);

    return NoteMapper.ToDto(note);
}
```

#### Logging
```csharp
// ✅ GOOD: Structured logging with semantic parameters
_logger.LogInformation("Creating note {NoteId} for user {UserId}", noteId, userId);
_logger.LogWarning("Note {NoteId} not found", noteId);
_logger.LogError(ex, "Failed to create note: {Error}", ex.Message);

// ❌ BAD: String interpolation
_logger.LogInformation($"Creating note {noteId} for user {userId}");

// Log levels:
// - Trace: Detailed debugging info
// - Debug: Development debugging
// - Information: Normal operations
// - Warning: Unexpected but handled
// - Error: Failures requiring attention
// - Critical: System failures
```

#### DTOs and Records
```csharp
// Use records for immutable DTOs
public record NoteDto(
    string Id,
    string Title,
    string? Content,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record CreateNoteDto(
    string Title,
    string? Content
);

public record UpdateNoteDto(
    string Title,
    string? Content
);
```

### Entities
```csharp
public class Note
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(10000)]
    public string? Content { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<Tag> Tags { get; set; } = new List<Tag>();
}
```

---

## TypeScript / React Standards

### File Organization

```
features/
└── featureName/
    ├── components/           # Reusable components for this feature
    │   ├── FeatureCard.tsx
    │   └── FeatureCard.test.tsx
    ├── pages/                # Page components (route targets)
    │   ├── FeaturePage.tsx
    │   └── FeaturePage.test.tsx
    ├── hooks/                # Custom React hooks
    │   ├── useFeature.ts
    │   └── useFeature.test.ts
    ├── services/             # API service functions
    │   ├── featureService.ts
    │   └── featureService.test.ts
    └── types/                # TypeScript types
        └── index.ts
```

### Naming Conventions

| Element | Convention | Example |
|---------|------------|---------|
| Component file | PascalCase.tsx | `NotesPage.tsx` |
| Component | PascalCase | `export const NotesPage` |
| Hook | useCamelCase | `useNotes` |
| Service | camelCaseService | `notesService` |
| Type/Interface | PascalCase | `NoteDto` |
| Variable | camelCase | `const notesList = []` |
| Function | camelCase | `const handleSubmit = () => {}` |
| Constant | camelCase or UPPER_SNAKE | `const maxLength = 200` |

### Component Structure

```typescript
// 1. Imports (grouped: react, third-party, local)
import { useState, useEffect, useCallback } from 'react';
import { Box, Typography, CircularProgress } from '@mui/material';
import { toast } from 'react-toastify';
import { CobraPrimaryButton, CobraTextField } from '@/theme/styledComponents';
import CobraStyles from '@/theme/CobraStyles';
import { useNotes } from '../hooks/useNotes';
import type { NoteDto } from '../types';

// 2. Types
interface NotesPageProps {
  initialFilter?: string;
}

// 3. Component
export const NotesPage: React.FC<NotesPageProps> = ({ initialFilter = '' }) => {
  // 3a. Hooks (built-in, then custom)
  const [dialogOpen, setDialogOpen] = useState(false);
  const { notes, loading, error, fetchNotes, createNote } = useNotes();

  // 3b. Derived state / memoization
  const filteredNotes = notes.filter(n =>
    n.title.toLowerCase().includes(initialFilter.toLowerCase())
  );

  // 3c. Callbacks
  const handleCreate = useCallback(async (data: CreateNoteDto) => {
    try {
      await createNote(data);
      setDialogOpen(false);
      toast.success('Note created');
    } catch {
      // Error handling in hook
    }
  }, [createNote]);

  // 3d. Effects
  useEffect(() => {
    fetchNotes();
  }, [fetchNotes]);

  // 3e. Early returns for loading/error states
  if (loading && notes.length === 0) {
    return <CircularProgress />;
  }

  if (error) {
    return <Typography color="error">{error}</Typography>;
  }

  // 3f. Main render
  return (
    <Box padding={CobraStyles.Padding.MainWindow}>
      <Typography variant="h5">Notes</Typography>
      {/* Component JSX */}
    </Box>
  );
};
```

### TypeScript Usage

#### Types vs Interfaces
```typescript
// Use interfaces for object shapes
interface NoteDto {
  id: string;
  title: string;
  content: string | null;
  createdAt: string;
  updatedAt: string;
}

// Use type for unions, intersections, and utility types
type NoteStatus = 'draft' | 'published' | 'archived';
type CreateNoteDto = Omit<NoteDto, 'id' | 'createdAt' | 'updatedAt'>;
```

#### Strict Typing
```typescript
// ✅ GOOD: Explicit types
const notes: NoteDto[] = await notesService.getNotes();

// ✅ GOOD: Type inference where obvious
const count = notes.length; // inferred as number

// ❌ BAD: Using any
const data: any = await fetch('/api/notes');

// ✅ GOOD: Use unknown for uncertain types
const parseResponse = (data: unknown): NoteDto => {
  if (typeof data === 'object' && data !== null && 'id' in data) {
    return data as NoteDto;
  }
  throw new Error('Invalid response');
};
```

#### Import Types
```typescript
// ✅ GOOD: Type-only imports
import type { NoteDto, CreateNoteDto } from '../types';
import { Button, type ButtonProps } from '@mui/material';

// ❌ BAD with verbatimModuleSyntax: Mixed imports
import { Button, ButtonProps } from '@mui/material';
```

### Hooks

#### Custom Hook Pattern
```typescript
// hooks/useNotes.ts
import { useState, useCallback, useEffect } from 'react';
import { toast } from 'react-toastify';
import { notesService } from '../services/notesService';
import type { NoteDto, CreateNoteDto } from '../types';

export const useNotes = () => {
  // State
  const [notes, setNotes] = useState<NoteDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Fetch function
  const fetchNotes = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await notesService.getNotes();
      setNotes(data);
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to load notes';
      setError(message);
      toast.error(message);
    } finally {
      setLoading(false);
    }
  }, []);

  // Create function
  const createNote = useCallback(async (dto: CreateNoteDto): Promise<NoteDto> => {
    setLoading(true);
    try {
      const created = await notesService.createNote(dto);
      setNotes(prev => [...prev, created]);
      return created;
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to create note';
      toast.error(message);
      throw err;
    } finally {
      setLoading(false);
    }
  }, []);

  // Initial fetch
  useEffect(() => {
    fetchNotes();
  }, [fetchNotes]);

  return { notes, loading, error, fetchNotes, createNote };
};
```

### Services

```typescript
// services/notesService.ts
import { apiClient } from '@/core/services/api';
import type { NoteDto, CreateNoteDto, UpdateNoteDto } from '../types';

export const notesService = {
  getNotes: async (): Promise<NoteDto[]> => {
    const response = await apiClient.get<NoteDto[]>('/api/notes');
    return response.data;
  },

  getNote: async (id: string): Promise<NoteDto> => {
    const response = await apiClient.get<NoteDto>(`/api/notes/${id}`);
    return response.data;
  },

  createNote: async (data: CreateNoteDto): Promise<NoteDto> => {
    const response = await apiClient.post<NoteDto>('/api/notes', data);
    return response.data;
  },

  updateNote: async (id: string, data: UpdateNoteDto): Promise<NoteDto> => {
    const response = await apiClient.put<NoteDto>(`/api/notes/${id}`, data);
    return response.data;
  },

  deleteNote: async (id: string): Promise<void> => {
    await apiClient.delete(`/api/notes/${id}`);
  },
};
```

### COBRA Styling (Critical)

```typescript
// ✅ ALWAYS use COBRA components
import { CobraPrimaryButton, CobraTextField } from '@/theme/styledComponents';
import CobraStyles from '@/theme/CobraStyles';

<Stack spacing={CobraStyles.Spacing.FormFields}>
  <CobraTextField label="Title" fullWidth required />
  <CobraPrimaryButton onClick={onSave}>Save</CobraPrimaryButton>
</Stack>

// ❌ NEVER import raw MUI for styled elements
import { Button, TextField } from '@mui/material';
```

### Frontend UX Patterns (Required)

All feature implementations MUST include these quality-of-life patterns for a polished user experience.

#### 1. Form Validation with Zod + React Hook Form

Use Zod schemas for type-safe validation with React Hook Form:

```typescript
// types/validation.ts
import { z } from 'zod';

export const FIELD_LIMITS = {
  name: 200,
  description: 4000,
} as const;

export const createEntitySchema = z.object({
  name: z.string()
    .min(1, 'Name is required')
    .max(FIELD_LIMITS.name, `Name must be ${FIELD_LIMITS.name} characters or less`),
  description: z.string()
    .max(FIELD_LIMITS.description)
    .optional()
    .or(z.literal('')),
});

export type CreateEntityFormValues = z.infer<typeof createEntitySchema>;

// In component:
import { useForm, Controller } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';

const { control, handleSubmit, formState: { errors, isDirty } } = useForm({
  resolver: zodResolver(createEntitySchema),
  mode: 'onBlur',
});
```

#### 2. Optimistic Updates

Update the UI immediately, rollback on error:

```typescript
const createMutation = useMutation({
  mutationFn: service.create,
  onMutate: async (newItem) => {
    await queryClient.cancelQueries({ queryKey: ['items'] });
    const previous = queryClient.getQueryData(['items']);

    // Optimistic update with temp ID
    queryClient.setQueryData(['items'], (old = []) => [
      { ...newItem, id: `temp-${Date.now()}` },
      ...old,
    ]);

    return { previous };
  },
  onSuccess: (created) => {
    // Replace temp with real data
    queryClient.setQueryData(['items'], (old = []) =>
      old.map(item => item.id.startsWith('temp-') ? created : item)
    );
  },
  onError: (_err, _vars, context) => {
    // Rollback on error
    if (context?.previous) {
      queryClient.setQueryData(['items'], context.previous);
    }
  },
});
```

#### 3. Loading Skeletons

Replace spinners with content-shaped skeletons:

```tsx
import { Skeleton } from '@mui/material';

const TableSkeleton = () => (
  <TableBody>
    {Array.from({ length: 5 }, (_, i) => (
      <TableRow key={i}>
        <TableCell><Skeleton variant="text" width={180} /></TableCell>
        <TableCell><Skeleton variant="rounded" width={70} height={24} /></TableCell>
        <TableCell><Skeleton variant="text" width={100} /></TableCell>
      </TableRow>
    ))}
  </TableBody>
);

// Use skeleton when loading with no data
{loading && items.length === 0 ? <TableSkeleton /> : <ActualContent />}
```

#### 4. Unsaved Changes Warning

Warn users before losing form data:

```typescript
// shared/hooks/useUnsavedChangesWarning.ts
import { useEffect } from 'react';
import { useBlocker } from 'react-router-dom';

export const useUnsavedChangesWarning = (hasUnsavedChanges: boolean) => {
  // Browser refresh/close warning
  useEffect(() => {
    const handler = (e: BeforeUnloadEvent) => {
      if (hasUnsavedChanges) {
        e.preventDefault();
        e.returnValue = '';
      }
    };
    window.addEventListener('beforeunload', handler);
    return () => window.removeEventListener('beforeunload', handler);
  }, [hasUnsavedChanges]);

  // React Router navigation blocking
  const blocker = useBlocker(({ currentLocation, nextLocation }) =>
    hasUnsavedChanges && currentLocation.pathname !== nextLocation.pathname
  );

  useEffect(() => {
    if (blocker.state === 'blocked') {
      const shouldLeave = window.confirm('You have unsaved changes. Leave anyway?');
      shouldLeave ? blocker.proceed() : blocker.reset();
    }
  }, [blocker]);
};

// Usage in form pages:
const [isDirty, setIsDirty] = useState(false);
useUnsavedChangesWarning(isDirty && !isSubmitting);
```

#### 5. Engaging Empty States

Empty states should be visually engaging with clear calls to action:

```tsx
// Search/filter returned no results
<Paper sx={{ py: 6, textAlign: 'center', border: '1px dashed', borderColor: 'grey.300' }}>
  <Box sx={{ /* circular icon container */ }}>
    <SearchOffIcon sx={{ fontSize: 40, color: 'grey.500' }} />
  </Box>
  <Typography variant="h6">No matching items</Typography>
  <Typography color="text.secondary">
    Try adjusting your search or filters.
  </Typography>
</Paper>

// First-time empty state (user can create)
<Paper sx={{ py: 8, textAlign: 'center', backgroundColor: 'primary.50' }}>
  <Box sx={{ /* gradient icon container with shadow */ }}>
    <PlaylistAddIcon sx={{ fontSize: 50, color: 'primary.main' }} />
  </Box>
  <Typography variant="h5">Create Your First Item</Typography>
  <Typography color="text.secondary" sx={{ mb: 3 }}>
    Get started by creating an item...
  </Typography>
  <CobraPrimaryButton startIcon={<AddIcon />} size="large">
    Create Item
  </CobraPrimaryButton>
</Paper>

// Empty state (user cannot create)
<Paper sx={{ py: 6, textAlign: 'center', border: '1px dashed' }}>
  <AssignmentIcon sx={{ fontSize: 40, color: 'grey.500' }} />
  <Typography variant="h6">No Items Available</Typography>
  <Typography color="text.secondary">
    Contact your administrator to get access.
  </Typography>
</Paper>
```

#### 6. Toast Notifications

Use consistent toast patterns for user feedback:

```typescript
import { toast } from 'react-toastify';

// Success after mutation
toast.success('Item created');
toast.success('Changes saved');

// Error handling in mutations
onError: (err) => {
  const message = err instanceof Error ? err.message : 'Operation failed';
  toast.error(message);
}
```

#### UX Pattern Checklist

When implementing any CRUD feature, verify:

- [ ] **Forms** use Zod + React Hook Form for validation
- [ ] **Mutations** include optimistic updates with rollback
- [ ] **Loading states** use skeletons instead of spinners
- [ ] **Form pages** warn before losing unsaved changes
- [ ] **Empty states** are engaging with appropriate icons and CTAs
- [ ] **Operations** provide toast feedback for success/error

---

## Testing Standards

### Backend Tests

#### Test Structure
```csharp
public class NotesServiceTests
{
    // Setup
    private readonly AppDbContext _context;
    private readonly NotesService _service;

    public NotesServiceTests()
    {
        _context = TestDbContextFactory.Create();
        _service = new NotesService(_context, Mock.Of<ILogger<NotesService>>());
    }

    // Tests grouped by method
    [Fact]
    public async Task GetAllAsync_WhenNotesExist_ReturnsAllNotes()
    {
        // Arrange
        _context.Notes.Add(new Note { Title = "Test" });
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        result.Should().HaveCount(1);
        result[0].Title.Should().Be("Test");
    }

    [Fact]
    public async Task CreateAsync_WithValidData_CreatesNote()
    {
        // Arrange
        var dto = new CreateNoteDto("New Note", "Content");

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        result.Title.Should().Be("New Note");
        _context.Notes.Should().HaveCount(1);
    }
}
```

#### Naming Convention
`MethodName_StateUnderTest_ExpectedBehavior`

### Frontend Tests

#### Component Tests
```typescript
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@/test/testUtils';
import { NotesPage } from './NotesPage';
import { useNotes } from '../hooks/useNotes';

vi.mock('../hooks/useNotes');

describe('NotesPage', () => {
  const mockUseNotes = {
    notes: [],
    loading: false,
    error: null,
    fetchNotes: vi.fn(),
    createNote: vi.fn(),
  };

  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(useNotes).mockReturnValue(mockUseNotes);
  });

  describe('rendering', () => {
    it('renders page title', () => {
      render(<NotesPage />);
      expect(screen.getByText('Notes')).toBeInTheDocument();
    });

    it('shows loading spinner when loading with no notes', () => {
      vi.mocked(useNotes).mockReturnValue({
        ...mockUseNotes,
        loading: true,
      });

      render(<NotesPage />);
      expect(screen.getByRole('progressbar')).toBeInTheDocument();
    });
  });

  describe('interactions', () => {
    it('opens dialog when New Note clicked', async () => {
      render(<NotesPage />);

      fireEvent.click(screen.getByRole('button', { name: /New Note/i }));

      await waitFor(() => {
        expect(screen.getByRole('dialog')).toBeInTheDocument();
      });
    });
  });
});
```

#### Service Tests
```typescript
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { notesService } from './notesService';
import { apiClient } from '@/core/services/api';

vi.mock('@/core/services/api', () => ({
  apiClient: {
    get: vi.fn(),
    post: vi.fn(),
    put: vi.fn(),
    delete: vi.fn(),
  },
}));

describe('notesService', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('getNotes', () => {
    it('fetches from correct endpoint', async () => {
      const mockNotes = [{ id: '1', title: 'Test' }];
      vi.mocked(apiClient.get).mockResolvedValueOnce({ data: mockNotes });

      const result = await notesService.getNotes();

      expect(apiClient.get).toHaveBeenCalledWith('/api/notes');
      expect(result).toEqual(mockNotes);
    });
  });
});
```

#### Hook Tests
```typescript
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { renderHook, waitFor, act } from '@testing-library/react';
import { useNotes } from './useNotes';
import { notesService } from '../services/notesService';

vi.mock('../services/notesService');

describe('useNotes', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('fetches notes on mount', async () => {
    const mockNotes = [{ id: '1', title: 'Test' }];
    vi.mocked(notesService.getNotes).mockResolvedValueOnce(mockNotes);

    const { result } = renderHook(() => useNotes());

    await waitFor(() => {
      expect(result.current.loading).toBe(false);
    });

    expect(result.current.notes).toEqual(mockNotes);
  });

  it('handles createNote', async () => {
    const newNote = { id: '2', title: 'New' };
    vi.mocked(notesService.getNotes).mockResolvedValueOnce([]);
    vi.mocked(notesService.createNote).mockResolvedValueOnce(newNote);

    const { result } = renderHook(() => useNotes());

    await waitFor(() => {
      expect(result.current.loading).toBe(false);
    });

    await act(async () => {
      await result.current.createNote({ title: 'New' });
    });

    expect(result.current.notes).toContainEqual(newNote);
  });
});
```

---

## Git Conventions

### Branch Naming
```
feature/add-note-tagging
bugfix/fix-note-delete-error
hotfix/security-patch
docs/update-readme
```

### Commit Messages
```
<type>(<scope>): <subject>

<body - optional>

<footer - optional>
```

**Types:**
- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation
- `style`: Formatting, no code change
- `refactor`: Code restructuring
- `test`: Adding tests
- `chore`: Maintenance

**Examples:**
```
feat(notes): add note tagging functionality
fix(api): correct null handling in GetNoteById
docs: update CLAUDE.md with testing guidelines
test(notes): add unit tests for NotesService
refactor(ui): extract NoteCard component
```

### Pull Request Template
```markdown
## Summary
Brief description of changes

## Related Issues
Closes #123

## Changes Made
- Added X
- Updated Y
- Fixed Z

## Testing
- [ ] Unit tests pass
- [ ] Manual testing completed
- [ ] Styling follows COBRA guidelines

## Screenshots (if applicable)
```

---

## Documentation Standards

### Code Comments

```csharp
// ✅ GOOD: Explain WHY, not WHAT
// Soft delete to preserve audit trail - hard delete requires admin approval
public async Task DeleteAsync(string id)

// ❌ BAD: Redundant comment
// Delete the note
public async Task DeleteAsync(string id)
```

```typescript
// ✅ GOOD: Document complex logic
// Debounce search to avoid excessive API calls during typing
const debouncedSearch = useMemo(
  () => debounce((query: string) => search(query), 300),
  [search]
);
```

### JSDoc for Complex Functions
```typescript
/**
 * Filters notes by multiple criteria with fuzzy matching
 *
 * @param notes - Array of notes to filter
 * @param criteria - Filter criteria object
 * @param criteria.search - Search term for title/content
 * @param criteria.tags - Array of tag IDs to match
 * @returns Filtered notes sorted by relevance
 *
 * @example
 * const filtered = filterNotes(notes, { search: 'meeting', tags: ['work'] });
 */
export function filterNotes(notes: NoteDto[], criteria: FilterCriteria): NoteDto[] {
  // Implementation
}
```

### README Structure
```markdown
# Feature Name

Brief description

## Quick Start

How to use this feature

## API Reference

Endpoint documentation

## Examples

Code examples

## Troubleshooting

Common issues and solutions
```

---

## Checklist Before Committing

### Backend
- [ ] Code compiles without warnings
- [ ] All tests pass (`dotnet test`)
- [ ] Async methods end with `Async`
- [ ] Structured logging used
- [ ] No `await Task.FromResult()` anti-pattern

### Frontend
- [ ] No TypeScript errors
- [ ] All tests pass (`npm run test:run`)
- [ ] COBRA components used (not raw MUI)
- [ ] No `any` types
- [ ] Type-only imports where applicable

### General
- [ ] Commit message follows convention
- [ ] Documentation updated if needed
- [ ] No secrets/credentials in code

---

## Creating a New Feature

When creating a new feature module, follow this checklist to ensure consistency with the existing codebase.

### 1. Backend Structure

Create the folder structure under `src/Cadence.Core/Features/YourFeature/`:

```
Features/
└── YourFeature/
    ├── Functions/
    │   └── YourFeatureFunction.cs
    ├── Models/
    │   ├── Entities/
    │   │   └── YourEntity.cs
    │   └── DTOs/
    │       └── YourEntityDto.cs
    ├── Services/
    │   ├── IYourFeatureService.cs
    │   └── YourFeatureService.cs
    └── Mappers/
        └── YourEntityMapper.cs
```

### 2. Frontend Structure

Create the folder structure under `src/frontend/src/features/yourFeature/`:

```
features/
└── yourFeature/
    ├── components/
    │   └── YourComponent.tsx
    ├── pages/
    │   ├── YourFeaturePage.tsx
    │   └── YourFeaturePage.test.tsx
    ├── hooks/
    │   ├── useYourFeature.ts
    │   └── useYourFeature.test.ts
    ├── services/
    │   ├── yourFeatureService.ts
    │   └── yourFeatureService.test.ts
    └── types/
        └── index.ts
```

### 3. Timezone Handling (Critical)

The codebase uses a **UTC storage, local display** pattern. No special configuration is needed—just follow the existing conventions:

#### Backend (C#)

```csharp
// ✅ ALWAYS use DateTime.UtcNow
public class YourEntity
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

// ❌ NEVER use DateTime.Now (local time)
public DateTime CreatedAt { get; set; } = DateTime.Now; // Wrong!
```

Entities implementing `ITimestampedEntity` get automatic UTC timestamps via `AppDbContext`.

#### Frontend (TypeScript)

```typescript
// Import date utilities for displaying dates
import {
  formatDateTime,      // "Jan 15, 2024, 5:30 AM" (local time)
  formatDate,          // "Jan 15, 2024"
  formatTime,          // "5:30 AM"
  formatRelativeTime,  // "5 minutes ago"
  formatSmartDateTime, // Relative if recent, full date if older
} from '@/shared/utils/dateUtils';

// ✅ Use these functions to display any date from the API
<Typography>{formatDateTime(note.createdAt)}</Typography>
<Typography>{formatRelativeTime(note.updatedAt)}</Typography>

// ❌ NEVER display raw date strings from the API
<Typography>{note.createdAt}</Typography> // Wrong! Shows UTC
```

#### How It Works

| Layer | Timezone | Example |
|-------|----------|---------|
| Database | UTC (`GETUTCDATE()`) | `2024-01-15 15:30:00` |
| API Response | UTC (no conversion) | `"2024-01-15T15:30:00"` |
| Frontend Display | Local (via `dateUtils`) | `"Jan 15, 2024, 10:30 AM"` (EST) |

### 4. Entity Registration

Register your entity in `AppDbContext.cs`:

```csharp
// Add DbSet
public DbSet<YourEntity> YourEntities { get; set; }

// Configure in OnModelCreating
modelBuilder.Entity<YourEntity>(entity =>
{
    entity.HasKey(e => e.Id);
    entity.Property(e => e.Name).IsRequired().HasMaxLength(200);

    // Timestamps (if implementing ITimestampedEntity)
    entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
    entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
});
```

### 5. Service Registration

Register your service in `Program.cs`:

```csharp
builder.Services.AddScoped<IYourToolService, YourToolService>();
```

### 6. Add Route

Add the route in `App.tsx`:

```typescript
import { YourToolPage } from '@/tools/yourTool/pages/YourToolPage';

// In Routes:
<Route path="/your-tool" element={<YourToolPage />} />
```

### 7. Database Migration

After adding your entity:

```bash
cd src/api
dotnet ef migrations add AddYourEntity
dotnet ef database update
```

### 8. New Feature Checklist

Before submitting your PR:

- [ ] **Backend**
  - [ ] Entity created with `DateTime.UtcNow` for timestamps
  - [ ] Entity registered in `AppDbContext` with `GETUTCDATE()` defaults
  - [ ] Service interface and implementation created
  - [ ] Service registered in `Program.cs`
  - [ ] Function endpoints created
  - [ ] Structured logging added
  - [ ] Unit tests written

- [ ] **Frontend**
  - [ ] Types defined in `types/index.ts`
  - [ ] Service created with proper API endpoints
  - [ ] Custom hook created for state management
  - [ ] Page component created with COBRA styling
  - [ ] Route added to `App.tsx`
  - [ ] Date displays use `dateUtils` functions
  - [ ] Unit tests written for service, hook, and page

- [ ] **Database**
  - [ ] Migration created and applied
  - [ ] Migration tested locally
