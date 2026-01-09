# Getting Started

> **Version:** 1.0.0
> **Last Updated:** 2025-12-04

Welcome to the Cadence template! This guide will help you set up your development environment and start building.

---

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Quick Start](#quick-start)
3. [Project Structure Overview](#project-structure-overview)
4. [Development Workflow](#development-workflow)
5. [Creating Your First Feature](#creating-your-first-feature)
6. [Common Tasks](#common-tasks)
7. [Next Steps](#next-steps)

---

## Prerequisites

Before you begin, ensure you have the following installed:

### Required Software

| Tool                           | Version          | Installation                                           |
| ------------------------------ | ---------------- | ------------------------------------------------------ |
| **.NET SDK**                   | 10.0 Preview     | [Download](https://dotnet.microsoft.com/download)      |
| **Node.js**                    | 20+              | [Download](https://nodejs.org/)                        |
| **Azure Functions Core Tools** | v4               | `npm install -g azure-functions-core-tools@4`          |
| **SQL Server**                 | 2019+ or LocalDB | [Download](https://www.microsoft.com/en-us/sql-server) |
| **Git**                        | Latest           | [Download](https://git-scm.com/)                       |
| **VS Code** (recommended)      | Latest           | [Download](https://code.visualstudio.com/)             |
| **Azure CLI** (for deployment) | Latest           | `winget install Microsoft.AzureCLI`                    |

### Verify Installation

```bash
# Check .NET
dotnet --version
# Should output: 10.0.xxx

# Check Node.js
node --version
# Should output: v20.x.x or higher

# Check Azure Functions Core Tools
func --version
# Should output: 4.x.x

# Check SQL Server (Windows with LocalDB)
sqllocaldb info
# Should list MSSQLLocalDB
```

### Recommended VS Code Extensions

When you open this project in VS Code, you'll be prompted to install recommended extensions. The key ones are:

**Essential:**

- C# Dev Kit - C# language support and debugging
- ESLint - Linting and formatting for TypeScript/JavaScript
- Azure Functions - Azure Functions development

**Highly Recommended:**

- GitLens - Enhanced Git integration
- Bicep - Infrastructure as Code support
- REST Client - API testing
- SQL Server (mssql) - Database management

See [.vscode/extensions.json](../.vscode/extensions.json) for the full list.

---

## Quick Start

### 1. Create Your Repository

Click **"Use this template"** on GitHub, or clone and push to your own repo:

```bash
# Clone the template
git clone https://github.com/dynamisinc/dynamis-reference-app.git my-app
cd my-app

# Remove template origin and add your own
git remote remove origin
git remote add origin https://github.com/your-org/my-app.git
git push -u origin main
```

### 2. Set Up the Backend

You can run the backend as either Azure Functions (Serverless) or a standard Web API (App Service).

**Option A: Azure Functions (Serverless)**

```bash
cd src/Cadence.Functions

# Copy the example local settings
cp local.settings.example.json local.settings.json

# Edit local.settings.json with your connection string
# For LocalDB:
# "ConnectionStrings:DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=MyApp;Trusted_Connection=True;TrustServerCertificate=True;"

# Restore packages
dotnet restore

# Restore local .NET tools (for Entity Framework)
dotnet tool restore

# Apply database migrations (point to WebApi project for design-time context)
# dotnet ef database update --project ../Cadence.WebApi

# Start the Azure Functions host
func start
```

**Option B: Web API (App Service)**

```bash
cd src/Cadence.WebApi

# Update appsettings.json with your connection string if needed
# (Default uses localdb)

# Restore packages
dotnet restore

# Start the Web API host
dotnet run
```

**API Documentation:**
This app uses Scalar for interactive API documentation. After starting the backend, open:

- **Functions:** http://localhost:7171/api/docs
- **Web API:** http://localhost:5071/scalar/v1

in your browser to view and test the API endpoints. Scalar loads the OpenAPI spec and provides a modern UI for exploring the API.

**Verify:**

- Functions: http://localhost:7171/api/health
- Web API: http://localhost:5071/health

### 3. Set Up the Frontend

Open a new terminal:

```bash
cd src/frontend

# Copy the example environment file
cp .env.example .env

# Install dependencies
npm install

# Start the development server
npm run dev
```

**Verify:** Open http://localhost:5197 - you should see the app

### 4. You're Ready

Both services should now be running:

- **API:** http://localhost:7171/api (Functions) or http://localhost:5071/api (Web API)
- **Frontend:** http://localhost:5197

Try the Notes feature to see everything working together.

---

## Project Structure Overview

```
my-app/
├── src/
│   ├── Cadence.Core/           # Business logic & Domain models
│   │   ├── Data/               # EF Core DbContext
│   │   ├── Features/           # Feature modules (Tools)
│   │   │   └── Notes/          # Sample feature
│   │   └── Shared/             # Shared infrastructure
│   │
│   ├── Cadence.Functions/      # Azure Functions Host
│   │   ├── Functions/          # HTTP Triggers
│   │   └── Program.cs          # Host configuration
│   │
│   ├── Cadence.WebApi/         # ASP.NET Core Web API Host
│   │   ├── Controllers/        # API Controllers
│   │   └── Program.cs          # Host configuration
│   │
│   ├── Cadence.Tests/          # Shared test infrastructure
│   │
│   └── frontend/               # Frontend (React + TypeScript)
│       └── src/
│           ├── core/           # Infrastructure (API client)
│           ├── shared/         # Shared hooks/components
│           ├── tools/          # Feature modules
│           │   └── notes/      # Sample feature
│           └── theme/          # COBRA styling
│
├── docs/                       # Documentation
├── infrastructure/             # Azure Bicep templates
└── scripts/                    # Helper scripts
```

### Key Concepts

1. **Tools Architecture:** Features are organized as self-contained "tools" in both backend and frontend. This allows features to be developed independently and potentially extracted to separate services.

2. **COBRA Styling:** All UI components use the COBRA design system. Never use raw MUI components - always use the styled versions from `@/theme/styledComponents`.

3. **Service Layer:** Business logic lives in `Cadence.Core` (backend) and custom hooks in `hooks/` (frontend). This ensures logic is decoupled from the hosting model (Functions vs Web API).

---

## Development Workflow

### Standard Development Loop

1. **Start backend:**

   ```bash
   cd src/api && func start
   ```

2. **Start frontend (separate terminal):**

   ```bash
   cd src/frontend && npm run dev
   ```

3. **Make changes** - both services have hot reload

4. **Run tests before committing:**

   ```bash
   # Backend
   cd src/api.Tests && dotnet test

   # Frontend
   cd src/frontend && npm run test:run
   ```

### Database Changes

When you modify entities:

```bash
cd src/api

# Create migration
dotnet ef migrations add YourMigrationName

# Apply to database
dotnet ef database update
```

### Adding Dependencies

```bash
# Backend (NuGet)
cd src/api
dotnet add package PackageName

# Frontend (npm)
cd src/frontend
npm install package-name
```

---

## Creating Your First Feature

Let's walk through creating a new "Tasks" feature.

### Backend

#### 1. Create the folder structure

```
src/api/Tools/Tasks/
├── Functions/
│   └── TasksFunction.cs
├── Models/
│   ├── Entities/
│   │   └── TaskItem.cs
│   └── DTOs/
│       └── TaskDto.cs
├── Services/
│   ├── ITasksService.cs
│   └── TasksService.cs
└── Mappers/
    └── TaskMapper.cs
```

#### 2. Create the entity

```csharp
// Tools/Tasks/Models/Entities/TaskItem.cs
namespace Cadence.Api.Tools.Tasks.Models.Entities;

public class TaskItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
```

#### 3. Add to DbContext

```csharp
// Core/Data/AppDbContext.cs
public DbSet<TaskItem> Tasks { get; set; }

// In OnModelCreating:
modelBuilder.Entity<TaskItem>(entity =>
{
    entity.HasKey(e => e.Id);
    entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
});
```

#### 4. Create migration

```bash
dotnet ef migrations add AddTasks
dotnet ef database update
```

#### 5. Create DTO

```csharp
// Tools/Tasks/Models/DTOs/TaskDto.cs
namespace Cadence.Api.Tools.Tasks.Models.DTOs;

public record TaskDto(
    string Id,
    string Title,
    string? Description,
    bool IsCompleted,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record CreateTaskDto(string Title, string? Description);
public record UpdateTaskDto(string Title, string? Description, bool IsCompleted);
```

#### 6. Create service

```csharp
// Tools/Tasks/Services/ITasksService.cs
public interface ITasksService
{
    Task<List<TaskDto>> GetAllAsync();
    Task<TaskDto?> GetByIdAsync(string id);
    Task<TaskDto> CreateAsync(CreateTaskDto dto);
    Task<TaskDto?> UpdateAsync(string id, UpdateTaskDto dto);
    Task<bool> DeleteAsync(string id);
}

// Tools/Tasks/Services/TasksService.cs
public class TasksService : ITasksService
{
    private readonly AppDbContext _context;
    private readonly ILogger<TasksService> _logger;

    public TasksService(AppDbContext context, ILogger<TasksService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<TaskDto>> GetAllAsync()
    {
        _logger.LogInformation("Fetching all tasks");
        var tasks = await _context.Tasks.AsNoTracking().ToListAsync();
        return tasks.Select(TaskMapper.ToDto).ToList();
    }

    // ... implement other methods
}
```

#### 7. Register service in Program.cs

```csharp
builder.Services.AddScoped<ITasksService, TasksService>();
```

#### 8. Create Function

```csharp
// Tools/Tasks/Functions/TasksFunction.cs
public class TasksFunction
{
    private readonly ITasksService _service;

    public TasksFunction(ITasksService service)
    {
        _service = service;
    }

    [Function("GetTasks")]
    public async Task<HttpResponseData> GetAll(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "tasks")]
        HttpRequestData req)
    {
        var tasks = await _service.GetAllAsync();
        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(tasks);
        return response;
    }
}
```

### Frontend

#### 1. Create folder structure

```
src/frontend/src/tools/tasks/
├── components/
├── pages/
│   ├── TasksPage.tsx
│   └── TasksPage.test.tsx
├── hooks/
│   ├── useTasks.ts
│   └── useTasks.test.ts
├── services/
│   ├── tasksService.ts
│   └── tasksService.test.ts
└── types/
    └── index.ts
```

#### 2. Create types

```typescript
// tools/tasks/types/index.ts
export interface TaskDto {
  id: string;
  title: string;
  description: string | null;
  isCompleted: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface CreateTaskDto {
  title: string;
  description?: string;
}
```

#### 3. Create service

```typescript
// tools/tasks/services/tasksService.ts
import { apiClient } from "@/core/services/api";
import type { TaskDto, CreateTaskDto } from "../types";

export const tasksService = {
  getTasks: async (): Promise<TaskDto[]> => {
    const response = await apiClient.get<TaskDto[]>("/api/tasks");
    return response.data;
  },

  createTask: async (data: CreateTaskDto): Promise<TaskDto> => {
    const response = await apiClient.post<TaskDto>("/api/tasks", data);
    return response.data;
  },
};
```

#### 4. Create hook

```typescript
// tools/tasks/hooks/useTasks.ts
import { useState, useEffect, useCallback } from "react";
import { toast } from "react-toastify";
import { tasksService } from "../services/tasksService";
import type { TaskDto, CreateTaskDto } from "../types";

export const useTasks = () => {
  const [tasks, setTasks] = useState<TaskDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const fetchTasks = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await tasksService.getTasks();
      setTasks(data);
    } catch (err) {
      const message =
        err instanceof Error ? err.message : "Failed to load tasks";
      setError(message);
      toast.error(message);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchTasks();
  }, [fetchTasks]);

  return { tasks, loading, error, fetchTasks };
};
```

#### 5. Create page

```typescript
// tools/tasks/pages/TasksPage.tsx
import { Box, Typography, CircularProgress } from "@mui/material";
import { CobraPrimaryButton } from "@/theme/styledComponents";
import CobraStyles from "@/theme/CobraStyles";
import { useTasks } from "../hooks/useTasks";

export const TasksPage: React.FC = () => {
  const { tasks, loading, error } = useTasks();

  if (loading && tasks.length === 0) {
    return <CircularProgress />;
  }

  if (error) {
    return <Typography color="error">{error}</Typography>;
  }

  return (
    <Box padding={CobraStyles.Padding.MainWindow}>
      <Typography variant="h5">Tasks</Typography>
      {/* Render tasks */}
    </Box>
  );
};
```

#### 6. Add route

```typescript
// App.tsx
import { TasksPage } from "@/tools/tasks/pages/TasksPage";

// In routes:
<Route path="/tasks" element={<TasksPage />} />;
```

#### 7. Write tests

Follow the patterns in the existing Notes tests.

---

## Common Tasks

### Run All Tests

```bash
# Backend
cd src/api.Tests && dotnet test

# Frontend
cd src/frontend && npm run test:run
```

### Test Coverage

Generate test coverage reports to see which code is covered by tests:

#### Frontend Coverage

```bash
cd src/frontend

# Generate coverage report
npm run test:coverage
```

This generates an HTML report in `src/frontend/coverage/` that you can open in a browser.

#### Backend Coverage

```bash
cd src/api.Tests

# Generate coverage report (outputs to TestResults/)
dotnet test --collect:"XPlat Code Coverage"

# For a more readable HTML report, install ReportGenerator:
dotnet tool install -g dotnet-reportgenerator-globaltool

# Generate HTML report
reportgenerator -reports:"TestResults/**/coverage.cobertura.xml" -targetdir:"coverage-report" -reporttypes:Html

# Open coverage-report/index.html in your browser
```

#### Coverage in CI

The CI pipeline runs tests but does not enforce coverage thresholds. Coverage reports are available as build artifacts for review.

### Check Types (Frontend)

```bash
cd src/frontend && npm run type-check
```

### Lint (Frontend)

```bash
cd src/frontend && npm run lint
```

### Build for Production

```bash
# Backend
cd src/api && dotnet publish -c Release

# Frontend
cd src/frontend && npm run build
```

### Reset Database

```bash
cd src/api
dotnet ef database drop --force
dotnet ef database update
```

---

## Optional: Real-Time Updates with Azure SignalR

The reference app includes optional real-time update functionality using Azure SignalR Service. When enabled, changes made in one browser session are automatically reflected in other browser sessions.

### Why Azure SignalR Service?

Azure Functions with SignalR bindings require Azure SignalR Service - there's no local emulator. However, Azure offers a **free tier** that's perfect for local development:

- **20 concurrent connections** (plenty for local testing)
- **20,000 messages/day**
- **No credit card required** for free tier

### Setting Up Azure SignalR Service (Free Tier)

#### 1. Create the SignalR Service

##### Option A: Azure Portal

1. Go to [Azure Portal](https://portal.azure.com)
2. Click **Create a resource** → Search for **SignalR Service**
3. Click **Create** and fill in:
   - **Resource Group:** Create new or use existing
   - **Resource Name:** `myapp-signalr-dev` (must be globally unique)
   - **Region:** Choose closest to you
   - **Pricing tier:** **Free** (F1)
   - **Service mode:** **Serverless** (required for Azure Functions)
4. Click **Review + Create** → **Create**
5. Wait for deployment (~1 minute)

##### Option B: Azure CLI

```bash
# Login to Azure
az login

# Create resource group (if needed)
az group create --name myapp-dev-rg --location eastus

# Create SignalR Service (Free tier, Serverless mode)
az signalr create \
  --name myapp-signalr-dev \
  --resource-group myapp-dev-rg \
  --sku Free_F1 \
  --service-mode Serverless \
  --location eastus
```

#### 2. Get the Connection String

##### Azure Portal

1. Go to your SignalR resource
2. Click **Keys** in the left menu
3. Copy the **Connection string** (Primary)

##### Azure CLI

```bash
az signalr key list \
  --name myapp-signalr-dev \
  --resource-group myapp-dev-rg \
  --query primaryConnectionString \
  --output tsv
```

#### 3. Configure the Backend

Edit `src/api/local.settings.json`:

```json
{
  "Values": {
    "AzureSignalRConnectionString": "Endpoint=https://myapp-signalr-dev.service.signalr.net;AccessKey=YOUR_KEY;Version=1.0;"
  }
}
```

#### 4. Configure the Frontend

Edit `src/frontend/.env`:

```env
# Uncomment and set the SignalR URL
VITE_SIGNALR_URL=http://localhost:7171/api
```

#### 5. Restart Both Services

```bash
# Backend
cd src/api && func start

# Frontend (new terminal)
cd src/frontend && npm run dev
```

### Testing Real-Time Updates

1. Open the app in two browser windows (or use incognito)
2. Navigate to Notes in both windows
3. Create, edit, or delete a note in one window
4. The change should appear in the other window automatically

### Troubleshooting

**SignalR connection fails:**

- Verify the connection string is correct
- Ensure the SignalR Service is in **Serverless** mode
- Check that `VITE_SIGNALR_URL` is set in the frontend

**No real-time updates:**

- Check browser console for SignalR connection errors
- Verify both frontend and backend are running
- Ensure you're using different user IDs in each browser (or incognito mode)

**"SignalR negotiation failed":**

- The `/api/negotiate` endpoint must be accessible
- Check CORS settings in `local.settings.json`

### Cost Considerations

- **Free tier:** No cost, 20 concurrent connections
- **Standard tier:** ~$50/month for 1,000 concurrent connections
- For production, consider Standard tier for reliability and scale

### Without SignalR (Default)

If you don't configure SignalR:

- The app works normally
- Changes are persisted to the database
- Other browser sessions see changes after refreshing
- No errors are shown - SignalR gracefully degrades

---

## Next Steps

1. **Explore the Notes feature** - It demonstrates all the patterns you need
2. **Read CLAUDE.md** - Essential guide for AI-assisted development
3. **Review CODING_STANDARDS.md** - Coding conventions to follow
4. **Study COBRA_STYLING.md** - Styling system reference
5. **Plan your features** - Use the user story template in `docs/templates/`
6. **Set up CI/CD** - See DEPLOYMENT.md for GitHub Actions setup

---

## Getting Help

- **Documentation:** Check the `docs/` folder
- **Issues:** Create an issue in the repository
- **AI Assistant:** Use Claude with CLAUDE.md context

Happy coding!
