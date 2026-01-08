# Cadence

> **HSEEP-compliant MSEL management platform for emergency exercise conduct**

[![Azure App Service](https://img.shields.io/badge/Azure%20App%20Service-.NET%2010-blue)](https://azure.microsoft.com/en-us/services/app-service/)
[![React](https://img.shields.io/badge/React-18.x-61DAFB)](https://reactjs.org/)
[![TypeScript](https://img.shields.io/badge/TypeScript-5.x-3178C6)](https://www.typescriptlang.org/)
[![Material-UI](https://img.shields.io/badge/MUI-7.x-007FFF)](https://mui.com/)

---

## Overview

Cadence is a Master Scenario Events List (MSEL) management platform designed for emergency management exercise conduct. Unlike full lifecycle planning tools, Cadence focuses specifically on the **operations phase** of exercises—where Controllers deliver injects, Evaluators record observations, and Exercise Directors maintain situational awareness.

### Key Capabilities

| Capability | Description |
|------------|-------------|
| **Offline Operation** | Full functionality without internet connectivity |
| **Dual Time Tracking** | Separate scheduled time (wall clock) and scenario time |
| **Practice Mode** | Training exercises excluded from production reports |
| **Excel Workflow** | Import/Export preserves familiar spreadsheet workflows |
| **HSEEP Compliance** | Aligned with Homeland Security Exercise and Evaluation Program |

### Target Users

| Role | Primary Responsibilities |
|------|-------------------------|
| **Administrator** | System configuration, user management |
| **Exercise Director** | Overall exercise oversight, real-time status |
| **Controller** | Inject delivery, player guidance |
| **Evaluator** | Performance observation, documentation |
| **Observer** | Read-only monitoring |

## Quick Start

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/)
- [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli)
- [SQL Server](https://www.microsoft.com/en-us/sql-server) (LocalDB for development)

### Local Development Setup

1. **Clone the repository**

   ```bash
   git clone https://github.com/[your-org]/cadence.git
   cd cadence
   ```

2. **Set up the database**

   ```bash
   cd src/Cadence.WebApi

   # Initialize user secrets (one time)
   dotnet user-secrets init

   # Set connection string
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost\\cadence;Database=Cadence;Trusted_Connection=True;TrustServerCertificate=True;"

   # Apply database migrations
   dotnet ef database update
   ```

3. **Start the backend**

   ```bash
   cd src/Cadence.WebApi
   dotnet run
   ```

4. **Set up the frontend**

   ```bash
   cd src/frontend

   # Copy the example environment file
   cp .env.example .env

   # Install dependencies
   npm install

   # Start the development server
   npm run dev
   ```

5. **Access the application**
   - Frontend: http://localhost:5197
   - API: http://localhost:5071/api
   - Health check: http://localhost:5071/api/health
   - API Documentation (Scalar): http://localhost:5071/api/docs

## Project Structure

```
cadence/
├── .github/                    # GitHub Actions & templates
│   ├── workflows/              # CI/CD pipelines
│   └── ISSUE_TEMPLATE/         # Issue templates
├── src/
│   ├── Cadence.Core/           # Business logic & domain models
│   ├── Cadence.WebApi/         # ASP.NET Core Web API
│   ├── Cadence.Tests/          # Unit & integration tests
│   └── frontend/               # React SPA
│       ├── src/
│       │   ├── core/           # App infrastructure
│       │   ├── shared/         # Shared components
│       │   ├── features/       # Feature modules
│       │   └── theme/          # COBRA styling
│       └── public/
├── infrastructure/             # Bicep templates (IaC)
├── scripts/                    # Helper scripts
└── docs/                       # Documentation
    └── requirements/           # User stories & specifications
```

## Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              CADENCE ARCHITECTURE                            │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                    React Frontend (Vite + TypeScript)                │   │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌────────────┐  │   │
│  │  │  Exercise   │  │    MSEL     │  │   Inject    │  │  Offline   │  │   │
│  │  │  Management │  │  Authoring  │  │   Conduct   │  │   Sync     │  │   │
│  │  └─────────────┘  └─────────────┘  └─────────────┘  └────────────┘  │   │
│  │                                                                      │   │
│  │  ┌─────────────────────────────────────────────────────────────┐    │   │
│  │  │  IndexedDB (Offline Storage) + Service Workers               │    │   │
│  │  └─────────────────────────────────────────────────────────────┘    │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                    │                                        │
│                                    ▼                                        │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                 ASP.NET Core Web API (.NET 10)                       │   │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌────────────┐  │   │
│  │  │  Exercise   │  │   Inject    │  │   Excel     │  │  SignalR   │  │   │
│  │  │    API      │  │    API      │  │   Import    │  │    Hub     │  │   │
│  │  └─────────────┘  └─────────────┘  └─────────────┘  └────────────┘  │   │
│  │                                                                      │   │
│  │  ┌─────────────────────────────────────────────────────────────┐    │   │
│  │  │  Entity Framework Core + Structured Logging                  │    │   │
│  │  └─────────────────────────────────────────────────────────────┘    │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                    │                                        │
│                 ┌──────────────────┼──────────────────┐                    │
│                 ▼                  ▼                  ▼                    │
│  ┌───────────────────┐  ┌───────────────────┐  ┌───────────────────┐      │
│  │    Azure SQL      │  │  Azure SignalR    │  │ Application       │      │
│  │    Database       │  │  Service          │  │ Insights          │      │
│  └───────────────────┘  └───────────────────┘  └───────────────────┘      │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

## Key Features

### Dual Time Tracking

Cadence tracks two distinct timestamps for each inject:

| Time Type | Purpose | Storage |
|-----------|---------|---------|
| **Scheduled Time** | When to deliver inject (wall clock) | UTC in database, displayed in exercise time zone |
| **Scenario Time** | In-scenario time (e.g., "Day 2, 14:00") | Day number + time of day |

### Excel Import/Export

Maintain familiar spreadsheet workflows:

- Import MSELs from Excel (.xlsx, .xls, .csv)
- Column mapping wizard with saved templates
- Export preserves formatting for stakeholder sharing
- Round-trip compatible (export → edit → re-import)

### Offline Capability

Full functionality without internet:

- IndexedDB stores exercises and injects locally
- Service Workers enable offline access
- Automatic sync when connectivity restored
- Conflict resolution with clear user feedback

### HSEEP Compliance

Aligned with [Homeland Security Exercise and Evaluation Program](https://www.fema.gov/emergency-managers/national-preparedness/exercises/hseep) 2020 doctrine:

- Standard terminology (inject, MSEL, Controller, Evaluator)
- Exercise objective tracking and linking
- Support for TTX, Functional, and Full-Scale exercises
- Observation capture for After-Action Reports

### COBRA Styling System

All UI components use the COBRA design system for consistent styling:

```tsx
import { CobraPrimaryButton, CobraTextField } from "@/theme/styledComponents";
import CobraStyles from "@/theme/CobraStyles";

<Stack spacing={CobraStyles.Spacing.FormFields}>
  <CobraTextField label="Inject Title" fullWidth />
  <CobraPrimaryButton onClick={handleFire}>Fire Inject</CobraPrimaryButton>
</Stack>
```

See [docs/COBRA_STYLING.md](docs/COBRA_STYLING.md) for the complete guide.

## Documentation

### Developer Guides

| Document | Description |
|----------|-------------|
| [CLAUDE.md](docs/CLAUDE.md) | AI assistant instructions |
| [GETTING_STARTED.md](docs/GETTING_STARTED.md) | New developer onboarding |
| [CODING_STANDARDS.md](docs/CODING_STANDARDS.md) | Code conventions |
| [DEVELOPMENT_WORKFLOW.md](docs/DEVELOPMENT_WORKFLOW.md) | User stories → GitHub issues |
| [COBRA_STYLING.md](docs/COBRA_STYLING.md) | Styling system reference |
| [DEPLOYMENT.md](docs/DEPLOYMENT.md) | Azure deployment guide |
| [LOGGING_GUIDE.md](docs/LOGGING_GUIDE.md) | Logging patterns |

### Requirements Documentation

| Document | Description |
|----------|-------------|
| [requirements/README.md](docs/requirements/README.md) | Requirements overview |
| [requirements/ROADMAP.md](docs/requirements/ROADMAP.md) | Development phases (MVP → Standard → Advanced) |
| [requirements/DOMAIN_GLOSSARY.md](docs/requirements/DOMAIN_GLOSSARY.md) | HSEEP terminology definitions |

#### Feature Requirements

| Feature | Description |
|---------|-------------|
| [exercise-crud](docs/requirements/exercise-crud/) | Exercise lifecycle management |
| [exercise-config](docs/requirements/exercise-config/) | Roles, participants, time zone |
| [exercise-objectives](docs/requirements/exercise-objectives/) | Objective management |
| [exercise-phases](docs/requirements/exercise-phases/) | Phase definition |
| [inject-crud](docs/requirements/inject-crud/) | Inject CRUD with dual time |
| [excel-import](docs/requirements/excel-import/) | Excel import wizard |
| [excel-export](docs/requirements/excel-export/) | Excel export |
| [inject-filtering](docs/requirements/inject-filtering/) | Filter and search |
| [inject-organization](docs/requirements/inject-organization/) | Sort, group, reorder |

## Deployment

### GitHub Actions

Push to `main` branch triggers automatic deployment:

1. Run unit tests
2. Build API + Frontend
3. Deploy to Azure App Service
4. Deploy frontend to Azure Static Web Apps
5. Validate with health checks

### Azure Resources

| Resource | Purpose |
|----------|---------|
| Azure App Service | Web API hosting |
| Azure SQL Database | Data persistence |
| Azure SignalR Service | Real-time sync |
| Azure Static Web Apps | Frontend hosting |
| Application Insights | Monitoring & logging |

See [docs/DEPLOYMENT.md](docs/DEPLOYMENT.md) for setup instructions.

## Development Workflow

1. **Pick a story** from `docs/requirements/{feature}/`
2. **Create a branch**: `feat/{feature}/S{number}-{description}`
3. **Implement** with tests
4. **Submit PR** referencing the story
5. **Deploy** to UAT after approval

### Commit Convention

```
feat(exercise-crud): S01 create exercise form
fix(inject-crud): S02 edit validation error
docs(requirements): add excel-import stories
```

## Roadmap

| Phase | Focus | Status |
|-------|-------|--------|
| **MVP** | Core CRUD, Excel import/export, offline | 🔨 In Progress |
| **Standard** | Grid view, branching injects, exercise clock | 📋 Planned |
| **Advanced** | Auto AAR, multi-location sync, channel delivery | 📋 Future |

See [docs/requirements/ROADMAP.md](docs/requirements/ROADMAP.md) for detailed feature breakdown.

## Contributing

1. Review the [CODING_STANDARDS.md](docs/CODING_STANDARDS.md)
2. Check existing stories in `docs/requirements/`
3. Create feature branch from `main`
4. Write tests for new functionality
5. Submit PR with story reference

## License

MIT License - see [LICENSE](LICENSE) for details.

---

**Cadence** - *Bringing rhythm to emergency exercise management*
