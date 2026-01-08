# Dynamis Reference App

> **A GitHub template for building modern web applications with React, Azure Functions, and COBRA styling**

[![Azure Functions](https://img.shields.io/badge/Azure%20Functions-.NET%2010-blue)](https://azure.microsoft.com/en-us/services/functions/)
[![React](https://img.shields.io/badge/React-18.x-61DAFB)](https://reactjs.org/)
[![TypeScript](https://img.shields.io/badge/TypeScript-5.x-3178C6)](https://www.typescriptlang.org/)
[![Material-UI](https://img.shields.io/badge/MUI-7.x-007FFF)](https://mui.com/)

---

## Overview

This is Dynamis' reference template for building production-quality web applications. It provides:

- **Frontend**: React 18 + TypeScript + Vite with COBRA styling system
- **Backend**: Dual-host architecture supporting:
  - **Azure Functions** (.NET 10 Isolated Worker) for serverless scale
  - **Azure App Service** (ASP.NET Core Web API) for always-on enterprise workloads
- **Database**: Entity Framework Core with Azure SQL
- **Real-time**: Azure SignalR Service integration
- **CI/CD**: GitHub Actions with automated deployment to Azure
- **Documentation**: Comprehensive guides for developers and AI assistants

## Quick Start

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/)
- [Azure Functions Core Tools v4](https://docs.microsoft.com/en-us/azure/azure-functions/functions-run-local)
- [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli)
- [SQL Server](https://www.microsoft.com/en-us/sql-server) or [Azure SQL](https://azure.microsoft.com/en-us/products/azure-sql/)

### Local Development Setup

1. **Clone the repository**

   ```bash
   git clone https://github.com/dynamisinc/dynamis-reference-app.git
   cd dynamis-reference-app
   ```

2. **Set up the backend**

   **Option A: Azure Functions (Serverless)**

   ```bash
   cd src/Dynamis.Functions

   # Copy the example settings file
   cp local.settings.example.json local.settings.json

   # Configure your database connection string
   func settings add ConnectionStrings:DefaultConnection "Server=(localdb)\\MSSQLLocalDB;Database=DynamisReferenceApp;Trusted_Connection=True;TrustServerCertificate=True;"

   # Restore dependencies
   dotnet restore

   # Apply database migrations (run from solution root or point to WebApi project)
   # dotnet ef database update --project ../Dynamis.WebApi

   # Start the Azure Functions host
   func start
   ```

   **Option B: App Service (Web API)**

   ```bash
   cd src/Dynamis.WebApi

   # Update appsettings.json with your connection string if needed
   # (Default uses localdb)

   # Start the Web API host
   dotnet run
   ```

3. **Set up the frontend**

   ```bash
   cd src/frontend

   # Copy the example environment file
   cp .env.example .env

   # Install dependencies
   npm install

   # Start the development server
   npm run dev
   ```

4. **Access the application**
   - Frontend: http://localhost:5173
   - API: http://localhost:7071/api
   - Health check: http://localhost:7071/api/health
   - API Documentation (Scalar): http://localhost:7071/api/docs

## Project Structure

```
dynamis-reference-app/
├── .github/                    # GitHub Actions & templates
│   ├── workflows/              # CI/CD pipelines
│   └── ISSUE_TEMPLATE/         # Issue templates
├── src/
│   ├── Dynamis.Core/           # Business logic & Domain models
│   ├── Dynamis.Functions/      # Azure Functions host
│   ├── Dynamis.WebApi/         # ASP.NET Core Web API host
│   ├── Dynamis.Tests/          # Shared test infrastructure
│   └── frontend/               # React SPA
│       ├── src/
│       │   ├── core/           # App infrastructure
│       │   ├── shared/         # Shared components
│       │   ├── tools/          # Feature modules
│       │   └── theme/          # COBRA styling
│       └── public/
├── infrastructure/             # Bicep templates (IaC)
├── database/                   # SQL scripts
├── scripts/                    # Helper scripts
└── docs/                       # Documentation
```

## Architecture

```
┌──────────────────────────────────────┐
│      Azure Static Web App (SWA)      │
│  ┌────────────────────────────────┐  │
│  │        React Frontend          │  │
│  │   Global CDN + Managed SSL     │  │
│  └────────────────────────────────┘  │
└──────────────────────────────────────┘
                 │
                 ▼
┌──────────────────────────────────────┐    ┌──────────────────────────────────────┐
│     Azure Functions (.NET 10)        │    │     Azure Web API (App Service)      │
│  ┌────────────────────────────────┐  │ OR │  ┌────────────────────────────────┐  │
│  │   HTTP Triggers (REST API)     │  │    │   ASP.NET Core Controllers        │  │
│  │   EF Core + Structured Logging │  │    │   EF Core + Structured Logging    │  │
│  └────────────────────────────────┘  │    └────────────────────────────────┘  │
└──────────────────────────────────────┘    └──────────────────────────────────────┘
                 │                                          │
        ┌────────┴──────────────────────────────────────────┘
        ▼                 ▼
┌───────────────┐  ┌───────────────────┐
│  Azure SQL    │  │ Azure SignalR     │
│  Database     │  │ Service           │
└───────────────┘  └───────────────────┘
```

## Key Features

### COBRA Styling System

All UI components use the COBRA design system for consistent styling across Dynamis applications.

```tsx
// Always use COBRA components, never raw MUI
import { CobraPrimaryButton, CobraTextField } from "@/theme/styledComponents";
import CobraStyles from "@/theme/CobraStyles";

<Stack spacing={CobraStyles.Spacing.FormFields}>
  <CobraTextField label="Name" fullWidth />
  <CobraPrimaryButton onClick={handleSave}>Save</CobraPrimaryButton>
</Stack>;
```

See [docs/COBRA_STYLING.md](docs/COBRA_STYLING.md) for the complete guide.

### Modular Tool Architecture

Features are organized as self-contained "tools" that can be developed independently:

```
src/api/Tools/
└── Notes/                    # Example tool
    ├── Functions/            # API endpoints
    ├── Models/               # Entities & DTOs
    ├── Services/             # Business logic
    └── Mappers/              # Entity ↔ DTO mapping

src/frontend/src/tools/
└── notes/                    # Matching frontend
    ├── components/
    ├── pages/
    ├── services/
    ├── hooks/
    └── types/
```

### API Documentation (Scalar)

Interactive API documentation powered by [Scalar](https://scalar.com/):

- **OpenAPI Spec**: `/api/openapi.json` - Machine-readable API specification
- **Scalar UI**: `/api/docs` - Interactive documentation with dark mode, request testing, and code examples

The documentation is automatically generated and includes all endpoints, request/response schemas, and parameter descriptions.

### Robust Logging

Structured logging with optional Application Insights:

```csharp
_logger.LogInformation("Creating note for user {UserId}", userId);
_logger.LogError(ex, "Failed to create note: {Error}", ex.Message);
```

### Real-time Updates

SignalR integration for real-time features:

```tsx
const { connection, isConnected } = useSignalR("/hubs/notifications");

useEffect(() => {
  connection?.on("NoteCreated", (note) => {
    // Handle real-time update
  });
}, [connection]);
```

## Documentation

| Document                                                | Description                  |
| ------------------------------------------------------- | ---------------------------- |
| [CLAUDE.md](docs/CLAUDE.md)                             | AI assistant instructions    |
| [GETTING_STARTED.md](docs/GETTING_STARTED.md)           | New developer onboarding     |
| [CODING_STANDARDS.md](docs/CODING_STANDARDS.md)         | Code conventions             |
| [DEVELOPMENT_WORKFLOW.md](docs/DEVELOPMENT_WORKFLOW.md) | User stories → GitHub issues |
| [COBRA_STYLING.md](docs/COBRA_STYLING.md)               | Styling system reference     |
| [DEPLOYMENT.md](docs/DEPLOYMENT.md)                     | Azure deployment guide       |
| [LOGGING_GUIDE.md](docs/LOGGING_GUIDE.md)               | Logging patterns             |

## Deployment

### GitHub Actions (Recommended)

Push to `main` branch triggers automatic deployment:

1. Run tests
2. Build API + Frontend
3. Deploy to Azure Functions
4. Deploy to Azure Static Web App
5. Validate with health checks

### Manual Deployment

See [docs/DEPLOYMENT.md](docs/DEPLOYMENT.md) for manual deployment instructions and Azure resource setup.

## Creating a New Project

1. Click **"Use this template"** on GitHub
2. Name your new repository
3. Clone and follow the [Getting Started guide](docs/GETTING_STARTED.md)
4. Set up Azure resources using the [Deployment guide](docs/DEPLOYMENT.md)

## Contributing

1. Create a feature branch from `main`
2. Write user stories in `docs/features/{feature}/USER_STORIES.md`
3. Create GitHub issues from user stories
4. Implement and test
5. Submit PR with reference to user stories

## License

MIT License - see [LICENSE](LICENSE) for details.

---

**Built with care by Dynamis Inc.**
