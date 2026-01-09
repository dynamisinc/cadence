# CLAUDE.md - AI Assistant Guide

> **Last Updated:** 2025-12-04
> **Template Version:** 1.0.0
> **Status:** Template Repository - Ready for Use

## Table of Contents
1. [Project Overview](#project-overview)
2. [Tech Stack & Architecture](#tech-stack--architecture)
3. [Project Structure](#project-structure)
4. [Development Environment](#development-environment)
5. [Code Conventions & Standards](#code-conventions--standards)
6. [COBRA Styling System](#cobra-styling-system)
7. [Adding New Features](#adding-new-features)
8. [Testing Guidelines](#testing-guidelines)
9. [Azure Deployment](#azure-deployment)
10. [Troubleshooting](#troubleshooting)
11. [FAQ for AI Assistants](#faq-for-ai-assistants)

---

## Project Overview

### What is This?
A **GitHub template repository** for building modern web applications with:
- Azure Functions serverless backend
- React SPA frontend with COBRA styling
- Real-time capabilities via Azure SignalR
- CI/CD via GitHub Actions

### Template Purpose
This template provides a starting point for Cadence projects with:
- Pre-configured COBRA design system
- Structured logging with optional Application Insights
- Modular "tools" architecture for feature isolation
- Comprehensive test setup (frontend and backend)
- User story workflow documentation

### Sample Feature: Notes
The template includes a complete "Notes" feature demonstrating:
- Full CRUD operations
- Backend: Azure Function + EF Core + Service layer
- Frontend: React page + custom hook + API service
- Tests: Backend service tests + Frontend component tests

---

## Tech Stack & Architecture

### Backend
| Technology | Version | Purpose |
|------------|---------|---------|
| .NET | 10.0 (Preview) | Runtime |
| Azure Functions | Isolated Worker v4 | Serverless compute |
| Entity Framework Core | 10.0 Preview | ORM |
| SQL Server | 2019+ / Azure SQL | Database |
| Serilog | Latest | Structured logging |
| Azure SignalR | Latest | Real-time communication |

### Frontend
| Technology | Version | Purpose |
|------------|---------|---------|
| React | 19.x | UI framework |
| TypeScript | 5.x | Type safety |
| Vite | 7.x | Build tool |
| Material-UI | 7.x (RC) | Component library |
| Axios | 1.x | HTTP client |
| @microsoft/signalr | 10.x | Real-time client |
| react-toastify | 11.x | Notifications |
| date-fns | 4.x | Date utilities |
| Vitest | 4.x | Test runner |
| React Testing Library | 16.x | Component testing |

### Architecture Diagram
```
┌──────────────────────────────────────┐
│      Azure Static Web App (SWA)      │
│  ┌────────────────────────────────┐  │
│  │     React + TypeScript SPA     │  │
│  │   COBRA Styling + MUI 7        │  │
│  │   Global CDN + Managed SSL     │  │
│  └────────────────────────────────┘  │
└──────────────────────────────────────┘
                 │
                 ▼
┌──────────────────────────────────────┐
│     Azure Functions (.NET 10)        │
│  ┌────────────────────────────────┐  │
│  │   HTTP Triggers (REST API)     │  │
│  │   SignalR Triggers (Real-time) │  │
│  │   EF Core + Serilog Logging    │  │
│  └────────────────────────────────┘  │
└──────────────────────────────────────┘
                 │
        ┌────────┴────────┐
        ▼                 ▼
┌───────────────┐  ┌───────────────────┐
│  Azure SQL    │  │ Azure SignalR     │
│  Database     │  │ Service           │
└───────────────┘  └───────────────────┘
```

---

## Project Structure

```
cadence/
├── .github/
│   ├── workflows/              # CI/CD pipelines
│   │   ├── ci.yml              # PR validation
│   │   └── deploy.yml          # Production deployment
│   └── ISSUE_TEMPLATE/         # Issue templates for user stories
│
├── src/
│   ├── api/                    # Azure Functions backend
│   │   ├── Core/               # Shared infrastructure
│   │   │   ├── Data/
│   │   │   │   ├── AppDbContext.cs
│   │   │   │   └── AppDbContextFactory.cs    # Design-time factory
│   │   │   ├── Extensions/
│   │   │   │   └── ServiceCollectionExtensions.cs
│   │   │   ├── Logging/
│   │   │   │   ├── CorrelationIdMiddleware.cs
│   │   │   │   └── LoggingExtensions.cs
│   │   │   └── Middleware/
│   │   │       └── ExceptionHandlingMiddleware.cs
│   │   │
│   │   ├── Shared/             # Cross-tool features
│   │   │   └── Health/
│   │   │       └── HealthFunction.cs
│   │   │
│   │   ├── Tools/              # Feature modules (lift-and-shift ready)
│   │   │   └── Notes/          # Sample tool
│   │   │       ├── Functions/
│   │   │       │   └── NotesFunction.cs
│   │   │       ├── Models/
│   │   │       │   ├── Entities/
│   │   │       │   │   └── Note.cs
│   │   │       │   └── DTOs/
│   │   │       │       └── NoteDto.cs
│   │   │       ├── Services/
│   │   │       │   ├── INotesService.cs
│   │   │       │   └── NotesService.cs
│   │   │       └── Mappers/
│   │   │           └── NoteMapper.cs
│   │   │
│   │   ├── Hubs/               # SignalR hubs
│   │   │   └── NotificationHub.cs
│   │   │
│   │   ├── Migrations/         # EF Core migrations
│   │   ├── GlobalUsings.cs     # Global using directives
│   │   ├── Program.cs          # App entry point
│   │   ├── Api.csproj          # Project file
│   │   └── local.settings.json # Local config (git-ignored)
│   │
│   ├── api.Tests/              # Backend tests
│   │   ├── Helpers/
│   │   │   └── TestDbContextFactory.cs
│   │   ├── Services/
│   │   │   └── NotesServiceTests.cs
│   │   └── Api.Tests.csproj
│   │
│   └── frontend/               # React SPA
│       ├── src/
│       │   ├── core/           # App-wide infrastructure
│       │   │   └── services/
│       │   │       └── api.ts  # Axios client
│       │   │
│       │   ├── shared/         # Shared components/hooks
│       │   │   └── hooks/      # (future: usePermissions, etc.)
│       │   │
│       │   ├── tools/          # Feature modules
│       │   │   └── notes/      # Sample tool
│       │   │       ├── components/
│       │   │       ├── pages/
│       │   │       │   ├── NotesPage.tsx
│       │   │       │   └── NotesPage.test.tsx
│       │   │       ├── hooks/
│       │   │       │   ├── useNotes.ts
│       │   │       │   └── useNotes.test.ts
│       │   │       ├── services/
│       │   │       │   ├── notesService.ts
│       │   │       │   └── notesService.test.ts
│       │   │       └── types/
│       │   │           └── index.ts
│       │   │
│       │   ├── theme/          # COBRA styling
│       │   │   ├── cobraTheme.ts
│       │   │   ├── cobraTheme.test.ts
│       │   │   ├── CobraStyles.ts
│       │   │   └── styledComponents/
│       │   │       ├── index.ts
│       │   │       ├── CobraPrimaryButton.tsx
│       │   │       ├── CobraSecondaryButton.tsx
│       │   │       ├── CobraDeleteButton.tsx
│       │   │       ├── CobraLinkButton.tsx
│       │   │       └── CobraTextField.tsx
│       │   │
│       │   ├── test/           # Test utilities
│       │   │   ├── setup.ts    # Vitest setup
│       │   │   └── testUtils.tsx
│       │   │
│       │   ├── App.tsx
│       │   └── main.tsx
│       │
│       ├── package.json
│       ├── vite.config.ts
│       ├── tsconfig.json
│       └── .env.example
│
├── infrastructure/             # Azure Bicep templates
├── database/                   # SQL scripts
├── scripts/                    # Helper scripts
├── docs/                       # Documentation
│
├── CLAUDE.md                   # This file
├── README.md                   # Project overview
└── .gitignore
```

---

## Development Environment

### Prerequisites
- **.NET 10 SDK** (Preview): `dotnet --version`
- **Node.js 20+**: `node --version`
- **Azure Functions Core Tools v4**: `func --version`
- **SQL Server 2019+** or **LocalDB**

### Initial Setup

#### 1. Clone and Configure
```bash
git clone https://github.com/cadence/cadence.git
cd cadence
```

#### 2. Backend Setup
```bash
cd src/api

# Copy example settings
cp local.settings.example.json local.settings.json

# Edit local.settings.json with your connection string:
# "ConnectionStrings:DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=Cadence;..."

# Restore packages
dotnet restore

# Apply migrations
dotnet ef database update

# Start the Functions host
func start
```

**API Endpoints:**
- Health: http://localhost:5071/api/health
- Notes: http://localhost:5071/api/notes

#### 3. Frontend Setup
```bash
cd src/frontend

# Copy example env
cp .env.example .env

# Install dependencies
npm install

# Start dev server
npm run dev
```

**Frontend:** http://localhost:5197

### Useful Commands

#### Backend
```bash
# Run Functions locally
func start

# Run tests
dotnet test

# Add migration
dotnet ef migrations add MigrationName

# Update database
dotnet ef database update

# Generate SQL script
dotnet ef migrations script
```

#### Frontend
```bash
# Development server
npm run dev

# Run tests
npm run test

# Run tests once
npm run test:run

# Run tests with coverage
npm run test:coverage

# Type check
npm run type-check

# Build for production
npm run build

# Lint
npm run lint
```

---

## Code Conventions & Standards

### C# Backend

#### Naming Conventions
```csharp
// Classes: PascalCase
public class NotesService { }

// Interfaces: IPascalCase
public interface INotesService { }

// Public properties/methods: PascalCase
public string Title { get; set; }
public async Task<List<Note>> GetAllAsync() { }

// Private fields: _camelCase
private readonly AppDbContext _context;
private readonly ILogger<NotesService> _logger;

// Parameters/locals: camelCase
public void CreateNote(string title, string content) { }

// Constants: PascalCase or UPPER_SNAKE_CASE
private const int MaxTitleLength = 200;
```

#### Async/Await Pattern
- **Always use async/await** for I/O operations
- **Suffix async methods with "Async"**

```csharp
// ✅ GOOD
public async Task<Note?> GetByIdAsync(string id)
{
    return await _context.Notes.FindAsync(id);
}

// ❌ BAD - synchronous database call
public Note? GetById(string id)
{
    return _context.Notes.Find(id);
}
```

#### Entity Framework Patterns
```csharp
// Use AsNoTracking for read-only queries
var notes = await _context.Notes
    .AsNoTracking()
    .OrderByDescending(n => n.UpdatedAt)
    .ToListAsync();

// Include related entities explicitly
var note = await _context.Notes
    .Include(n => n.Tags)
    .FirstOrDefaultAsync(n => n.Id == id);
```

#### Logging Pattern
```csharp
// Use structured logging with semantic parameters
_logger.LogInformation("Creating note with title {Title} for user {UserId}", title, userId);
_logger.LogError(ex, "Failed to create note: {Error}", ex.Message);

// Log at appropriate levels:
// - Trace: Detailed debugging
// - Debug: Development debugging
// - Information: Normal operations
// - Warning: Unexpected but handled
// - Error: Failures requiring attention
// - Critical: System failures
```

### TypeScript Frontend

#### Code Formatting (ESLint Stylistic)

This project uses **ESLint Stylistic** for code formatting instead of Prettier. ESLint handles both linting and formatting in a single tool.

**Key formatting rules:**
- **Indentation:** 2 spaces
- **Quotes:** Single quotes (`'string'`)
- **Semicolons:** None (no semicolons)
- **Trailing commas:** Required for multiline
- **Max line length:** 100 characters

**Format on Save:** VS Code is configured to auto-format on save using ESLint.

**Manual formatting:**
```bash
cd src/frontend

# Check for issues
npm run lint

# Auto-fix formatting issues
npm run format
# or
npm run lint:fix
```

**Why ESLint Stylistic over Prettier?**
- Single tool for linting + formatting (no conflicts)
- Better TypeScript integration
- Official successor to ESLint's deprecated formatting rules
- Faster (one pass instead of two)

#### Naming Conventions
```typescript
// Components: PascalCase
export const NotesPage: React.FC = () => { };

// Interfaces/Types: PascalCase
export interface NoteDto {
  id: string;
  title: string;
}

// Variables/functions: camelCase
const handleSubmit = () => { };
const notesList = [];

// Custom hooks: useCamelCase
export const useNotes = () => { };

// Constants: camelCase or UPPER_SNAKE_CASE
const apiBaseUrl = import.meta.env.VITE_API_URL;
const MAX_TITLE_LENGTH = 200;
```

#### Component Structure
```typescript
// 1. Imports
import { useState, useEffect } from 'react';
import { Box, Typography } from '@mui/material';
import { CobraPrimaryButton } from '@/theme/styledComponents';

// 2. Types
interface NotesPageProps {
  initialFilter?: string;
}

// 3. Component
export const NotesPage: React.FC<NotesPageProps> = ({ initialFilter }) => {
  // 3a. Hooks (state, effects, custom hooks)
  const { notes, loading, error, fetchNotes, createNote } = useNotes();
  const [dialogOpen, setDialogOpen] = useState(false);

  // 3b. Derived state
  const filteredNotes = notes.filter(n => n.title.includes(initialFilter ?? ''));

  // 3c. Event handlers
  const handleCreate = async (data: CreateNoteDto) => {
    await createNote(data);
    setDialogOpen(false);
  };

  // 3d. Render
  return (
    <Box>
      {/* JSX */}
    </Box>
  );
};
```

#### API Service Pattern
```typescript
// services/notesService.ts
import { apiClient } from '@/core/services/api';
import type { NoteDto, CreateNoteDto } from '../types';

export const notesService = {
  getNotes: async (): Promise<NoteDto[]> => {
    const response = await apiClient.get<NoteDto[]>('/api/notes');
    return response.data;
  },

  createNote: async (data: CreateNoteDto): Promise<NoteDto> => {
    const response = await apiClient.post<NoteDto>('/api/notes', data);
    return response.data;
  },
};
```

#### Custom Hook Pattern
```typescript
// hooks/useNotes.ts
export const useNotes = () => {
  const [notes, setNotes] = useState<NoteDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const fetchNotes = async () => {
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
  };

  useEffect(() => {
    fetchNotes();
  }, []);

  return { notes, loading, error, fetchNotes };
};
```

### Markdown Linting

This project uses **markdownlint** (via VS Code extension) for documentation quality. The `.markdownlint.json` config disables rules that conflict with the existing documentation style:

| Rule | Description | Why Disabled |
|------|-------------|--------------|
| MD013 | Line length limit | Long tables and code examples |
| MD022 | Headings surrounded by blank lines | Compact doc style |
| MD024 | No duplicate headings | Repeated section names (e.g., "Usage") |
| MD031 | Code blocks surrounded by blank lines | Compact doc style |
| MD032 | Lists surrounded by blank lines | Compact doc style |
| MD033 | No inline HTML | Tables use HTML for formatting |
| MD034 | No bare URLs | URLs in code examples |
| MD040 | Code blocks should have language | Some blocks are plain text |
| MD058 | Tables surrounded by blank lines | Compact doc style |

---

## COBRA Styling System

### Critical Rule
**NEVER import raw MUI components for styled elements. ALWAYS use COBRA components.**

```typescript
// ❌ NEVER DO THIS
import { Button, TextField } from '@mui/material';

// ✅ ALWAYS DO THIS
import { CobraPrimaryButton, CobraTextField } from '@/theme/styledComponents';
```

### Available Components

| Component | Use Case |
|-----------|----------|
| `CobraPrimaryButton` | Primary actions (Save, Create, Submit) |
| `CobraSecondaryButton` | Secondary actions (outlined style) |
| `CobraDeleteButton` | Destructive actions (red, with delete icon) |
| `CobraLinkButton` | Text-only actions (Cancel, Back) |
| `CobraTextField` | All text inputs |

### Using CobraStyles Constants

```typescript
import CobraStyles from '@/theme/CobraStyles';

<Stack spacing={CobraStyles.Spacing.FormFields}>  {/* 12px */}
  <CobraTextField label="Title" fullWidth />
  <CobraTextField label="Content" multiline rows={4} fullWidth />
</Stack>
```

**Available Constants:**
- `CobraStyles.Spacing.FormFields` (12px) - Between form fields
- `CobraStyles.Spacing.AfterSeparator` (18px) - After dividers
- `CobraStyles.Padding.MainWindow` (18px) - Page content padding
- `CobraStyles.Padding.DialogContent` (15px) - Dialog interior

### Theme Colors

```typescript
import { useTheme } from '@mui/material/styles';

const theme = useTheme();

// ✅ Use theme colors
<Box sx={{ color: theme.palette.buttonPrimary.main }}>  {/* Cobalt Blue */}
<Box sx={{ color: theme.palette.buttonDelete.main }}>   {/* Lava Red */}

// ❌ Never hardcode colors
<Box sx={{ color: '#0020C2' }}>
```

### Common Patterns

#### Form Layout
```typescript
<Stack spacing={CobraStyles.Spacing.FormFields}>
  <CobraTextField label="Title" fullWidth required />
  <CobraTextField label="Content" multiline rows={4} fullWidth />

  <DialogActions>
    <CobraLinkButton onClick={onCancel}>Cancel</CobraLinkButton>
    <CobraPrimaryButton onClick={onSave}>Save</CobraPrimaryButton>
  </DialogActions>
</Stack>
```

#### Dialog
```typescript
<Dialog open={open} onClose={onClose}>
  <DialogTitle>Create Note</DialogTitle>
  <DialogContent>
    <Stack spacing={CobraStyles.Spacing.FormFields} sx={{ mt: 1 }}>
      <CobraTextField label="Title" fullWidth />
    </Stack>
  </DialogContent>
  <DialogActions>
    <CobraLinkButton onClick={onClose}>Cancel</CobraLinkButton>
    <CobraPrimaryButton onClick={onCreate}>Create Note</CobraPrimaryButton>
  </DialogActions>
</Dialog>
```

---

## Adding New Features

### Backend: Add a New Tool

#### 1. Create Folder Structure
```
src/api/Tools/YourTool/
├── Functions/
│   └── YourToolFunction.cs
├── Models/
│   ├── Entities/
│   │   └── YourEntity.cs
│   └── DTOs/
│       └── YourEntityDto.cs
├── Services/
│   ├── IYourToolService.cs
│   └── YourToolService.cs
└── Mappers/
    └── YourEntityMapper.cs
```

#### 2. Create Entity
```csharp
// Models/Entities/YourEntity.cs
namespace Cadence.Api.Tools.YourTool.Models.Entities;

public class YourEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
```

#### 3. Update DbContext
```csharp
// Core/Data/AppDbContext.cs
public DbSet<YourEntity> YourEntities { get; set; }

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // ... existing config ...

    modelBuilder.Entity<YourEntity>(entity =>
    {
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
    });
}
```

#### 4. Create Migration
```bash
dotnet ef migrations add AddYourEntity
dotnet ef database update
```

#### 5. Create Service
```csharp
// Services/IYourToolService.cs
public interface IYourToolService
{
    Task<List<YourEntityDto>> GetAllAsync();
    Task<YourEntityDto?> GetByIdAsync(string id);
    Task<YourEntityDto> CreateAsync(CreateYourEntityDto dto);
}

// Services/YourToolService.cs
public class YourToolService : IYourToolService
{
    private readonly AppDbContext _context;
    private readonly ILogger<YourToolService> _logger;

    public YourToolService(AppDbContext context, ILogger<YourToolService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<YourEntityDto>> GetAllAsync()
    {
        _logger.LogInformation("Fetching all entities");
        var entities = await _context.YourEntities.AsNoTracking().ToListAsync();
        return entities.Select(YourEntityMapper.ToDto).ToList();
    }
}
```

#### 6. Register Service
```csharp
// Program.cs - in ConfigureServices
builder.Services.AddScoped<IYourToolService, YourToolService>();
```

#### 7. Create Function
```csharp
// Functions/YourToolFunction.cs
public class YourToolFunction
{
    private readonly IYourToolService _service;

    public YourToolFunction(IYourToolService service)
    {
        _service = service;
    }

    [Function("GetAllYourEntities")]
    public async Task<HttpResponseData> GetAll(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "your-entities")]
        HttpRequestData req)
    {
        var items = await _service.GetAllAsync();
        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(items);
        return response;
    }
}
```

### Frontend: Add a New Tool

#### 1. Create Folder Structure
```
src/frontend/src/tools/yourTool/
├── components/
├── pages/
│   ├── YourToolPage.tsx
│   └── YourToolPage.test.tsx
├── hooks/
│   ├── useYourTool.ts
│   └── useYourTool.test.ts
├── services/
│   ├── yourToolService.ts
│   └── yourToolService.test.ts
└── types/
    └── index.ts
```

#### 2. Create Types
```typescript
// types/index.ts
export interface YourEntityDto {
  id: string;
  name: string;
  createdAt: string;
  updatedAt: string;
}

export interface CreateYourEntityDto {
  name: string;
}
```

#### 3. Create Service
```typescript
// services/yourToolService.ts
import { apiClient } from '@/core/services/api';
import type { YourEntityDto, CreateYourEntityDto } from '../types';

export const yourToolService = {
  getAll: async (): Promise<YourEntityDto[]> => {
    const response = await apiClient.get<YourEntityDto[]>('/api/your-entities');
    return response.data;
  },

  create: async (data: CreateYourEntityDto): Promise<YourEntityDto> => {
    const response = await apiClient.post<YourEntityDto>('/api/your-entities', data);
    return response.data;
  },
};
```

#### 4. Create Hook
```typescript
// hooks/useYourTool.ts
import { useState, useEffect } from 'react';
import { toast } from 'react-toastify';
import { yourToolService } from '../services/yourToolService';
import type { YourEntityDto } from '../types';

export const useYourTool = () => {
  const [items, setItems] = useState<YourEntityDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const fetchItems = async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await yourToolService.getAll();
      setItems(data);
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to load items';
      setError(message);
      toast.error(message);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchItems();
  }, []);

  return { items, loading, error, fetchItems };
};
```

#### 5. Create Page
```typescript
// pages/YourToolPage.tsx
import { Box, Typography, CircularProgress } from '@mui/material';
import { CobraPrimaryButton } from '@/theme/styledComponents';
import CobraStyles from '@/theme/CobraStyles';
import { useYourTool } from '../hooks/useYourTool';

export const YourToolPage: React.FC = () => {
  const { items, loading, error } = useYourTool();

  if (loading) {
    return <CircularProgress />;
  }

  if (error) {
    return <Typography color="error">{error}</Typography>;
  }

  return (
    <Box padding={CobraStyles.Padding.MainWindow}>
      <Typography variant="h5">Your Tool</Typography>
      {/* Render items */}
    </Box>
  );
};
```

#### 6. Add Route
```typescript
// App.tsx
import { YourToolPage } from '@/tools/yourTool/pages/YourToolPage';

// In routes:
<Route path="/your-tool" element={<YourToolPage />} />
```

---

## Testing Guidelines

### Test Organization

**Backend:** Tests in separate project mirroring source structure
```
src/api.Tests/
├── Services/
│   └── NotesServiceTests.cs
└── Helpers/
    └── TestDbContextFactory.cs
```

**Frontend:** Colocated tests next to source files
```
src/tools/notes/
├── pages/
│   ├── NotesPage.tsx
│   └── NotesPage.test.tsx
├── hooks/
│   ├── useNotes.ts
│   └── useNotes.test.ts
└── services/
    ├── notesService.ts
    └── notesService.test.ts
```

### Backend Testing

```csharp
// Services/NotesServiceTests.cs
public class NotesServiceTests
{
    private readonly AppDbContext _context;
    private readonly NotesService _service;

    public NotesServiceTests()
    {
        _context = TestDbContextFactory.Create();
        _service = new NotesService(_context, Mock.Of<ILogger<NotesService>>());
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllNotes()
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
}
```

### Frontend Testing

#### Component Tests
```typescript
// pages/NotesPage.test.tsx
import { describe, it, expect, vi } from 'vitest';
import { render, screen, waitFor } from '@/test/testUtils';
import { NotesPage } from './NotesPage';
import { useNotes } from '../hooks/useNotes';

vi.mock('../hooks/useNotes');

describe('NotesPage', () => {
  it('renders notes list', async () => {
    vi.mocked(useNotes).mockReturnValue({
      notes: [{ id: '1', title: 'Test Note', content: null }],
      loading: false,
      error: null,
      fetchNotes: vi.fn(),
      createNote: vi.fn(),
    });

    render(<NotesPage />);

    await waitFor(() => {
      expect(screen.getByText('Test Note')).toBeInTheDocument();
    });
  });

  it('shows loading spinner', () => {
    vi.mocked(useNotes).mockReturnValue({
      notes: [],
      loading: true,
      error: null,
      fetchNotes: vi.fn(),
      createNote: vi.fn(),
    });

    render(<NotesPage />);

    expect(screen.getByRole('progressbar')).toBeInTheDocument();
  });
});
```

#### Service Tests
```typescript
// services/notesService.test.ts
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

  it('getNotes fetches from correct endpoint', async () => {
    const mockNotes = [{ id: '1', title: 'Test' }];
    vi.mocked(apiClient.get).mockResolvedValueOnce({ data: mockNotes });

    const result = await notesService.getNotes();

    expect(apiClient.get).toHaveBeenCalledWith('/api/notes');
    expect(result).toEqual(mockNotes);
  });
});
```

#### Hook Tests
```typescript
// hooks/useNotes.test.ts
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { renderHook, waitFor } from '@testing-library/react';
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

    expect(result.current.loading).toBe(true);

    await waitFor(() => {
      expect(result.current.loading).toBe(false);
    });

    expect(result.current.notes).toEqual(mockNotes);
  });
});
```

### Running Tests

```bash
# Backend
cd src/api.Tests
dotnet test

# Frontend
cd src/frontend
npm run test        # Watch mode
npm run test:run    # Single run
npm run test:coverage  # With coverage
```

---

## Azure Deployment

### Azure Resources Required

1. **Azure Function App** - Consumption or Premium plan
2. **Azure SQL Database** - Basic tier for dev, Standard for prod
3. **Azure Static Web App** - Free or Standard tier
4. **Azure SignalR Service** - Free tier for dev
5. **Application Insights** (optional) - For monitoring

### Environment Variables

#### Azure Functions (Application Settings)
```
ConnectionStrings__DefaultConnection=Server=xxx.database.windows.net;Database=xxx;...
AzureSignalRConnectionString=Endpoint=https://xxx.service.signalr.net;...
APPLICATIONINSIGHTS_CONNECTION_STRING=InstrumentationKey=xxx;...
```

#### Frontend (.env.production)
```
VITE_API_URL=https://your-function-app.azurewebsites.net
VITE_SIGNALR_URL=https://your-function-app.azurewebsites.net
```

### GitHub Actions Deployment

The template includes workflows for:
- **ci.yml** - Runs on PRs: tests, build validation
- **deploy.yml** - Runs on main merge: deploys to Azure

Required secrets:
- `AZURE_FUNCTIONAPP_PUBLISH_PROFILE`
- `AZURE_STATIC_WEB_APPS_API_TOKEN`

---

## Troubleshooting

### Backend Issues

#### ".NET 10 not supported by Azure Functions SDK"
The template includes a workaround in `Api.csproj`:
```xml
<Target Name="_SetNet10ToolingSuffix" BeforeTargets="_FunctionsPreBuild">
  <PropertyGroup>
    <_ToolingSuffix Condition="'$(TargetFrameworkVersion)' == 'v10.0'">net10-isolated</_ToolingSuffix>
  </PropertyGroup>
</Target>
```

#### "WriteAsync not found on HttpResponse"
Ensure `Microsoft.AspNetCore.Http` is in GlobalUsings.cs:
```csharp
global using Microsoft.AspNetCore.Http;
```

#### EF Core Migration Errors
```bash
# Reset migrations
rm -rf Migrations/
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### Frontend Issues

#### Tests Fail with "ResizeObserver is not defined"
The test setup must mock ResizeObserver as a class:
```typescript
// test/setup.ts
class ResizeObserverMock {
  observe = vi.fn();
  unobserve = vi.fn();
  disconnect = vi.fn();
}
global.ResizeObserver = ResizeObserverMock;
```

#### API Calls Return 404
Check that:
1. Backend is running (`func start`)
2. `.env` has correct `VITE_API_URL=http://localhost:5071`
3. Service uses correct path (`/api/notes` not `/notes`)

#### Type Import Errors
Use `type` keyword for type-only imports:
```typescript
// ✅ Correct
import { Button, type ButtonProps } from '@mui/material';

// ❌ Error with verbatimModuleSyntax
import { Button, ButtonProps } from '@mui/material';
```

---

## FAQ for AI Assistants

**Q: What's the primary architecture pattern?**
A: Modular "tools" architecture. Each feature is self-contained in `src/api/Tools/{ToolName}` and `src/frontend/src/tools/{toolName}`. This enables lift-and-shift of features between projects.

**Q: Should I use raw MUI components?**
A: **No.** Always use COBRA styled components from `@/theme/styledComponents`. Never import Button, TextField, etc. directly from `@mui/material`.

**Q: Where do I put business logic?**
A: Backend: `Services/` layer. Frontend: Custom hooks (`hooks/`).

**Q: How do I handle errors?**
A: Backend: Let exceptions bubble to `ExceptionHandlingMiddleware`. Frontend: Try/catch in hooks, show toast notifications.

**Q: What's the testing strategy?**
A: Backend tests in separate project. Frontend tests colocated with source files. Both use mocking for dependencies.

**Q: How do I add real-time features?**
A: Use Azure SignalR Service. Backend: Add hub in `Hubs/`. Frontend: Use `@microsoft/signalr` with connection hooks.

**Q: What database should I use locally?**
A: SQL Server LocalDB for Windows, or Docker SQL Server for cross-platform.

**Q: How do I deploy?**
A: Push to main branch triggers GitHub Actions. Manual deploy via `func azure functionapp publish` and `swa deploy`.

---

## Change Log

| Date | Version | Changes |
|------|---------|---------|
| 2025-12-04 | 1.0.0 | Initial template release with Notes sample feature, COBRA styling, comprehensive tests |

---

**For questions or issues, refer to:**
- `README.md` - Project overview and quick start
- `docs/CODING_STANDARDS.md` - Detailed coding conventions
- `docs/COBRA_STYLING.md` - Complete styling guide
- `docs/DEPLOYMENT.md` - Azure deployment guide
