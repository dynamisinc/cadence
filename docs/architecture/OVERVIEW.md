# Architecture Overview

This document describes the high-level architecture of the Cadence.

---

## System Architecture

```
┌─────────────────────────────────────────────────────────────────────────┐
│                              CLIENTS                                     │
├─────────────────────────────────────────────────────────────────────────┤
│   Browser (React SPA)          Mobile (Future)         Third-Party      │
│   └─ COBRA Styling             └─ React Native         └─ REST API      │
│   └─ MUI 7 Components          └─ Shared Types                          │
└─────────────────────────────────────────────────────────────────────────┘
                │                        │                    │
                │         HTTPS          │                    │
                ▼                        ▼                    ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                         AZURE STATIC WEB APPS                            │
│   ┌─────────────────────────────────────────────────────────────────┐   │
│   │  React SPA (Vite build)                                         │   │
│   │  └─ Global CDN distribution                                     │   │
│   │  └─ Managed SSL certificates                                    │   │
│   │  └─ Custom domain support                                       │   │
│   └─────────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────────┘
                │
                │  /api/* proxy
                ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                         AZURE FUNCTIONS                                  │
│   ┌─────────────────────────────────────────────────────────────────┐   │
│   │  .NET 10 Isolated Worker                                        │   │
│   │  ├─ HTTP Triggers (REST API)                                    │   │
│   │  ├─ SignalR Triggers (Real-time)                                │   │
│   │  └─ Timer Triggers (Background jobs - future)                   │   │
│   ├─────────────────────────────────────────────────────────────────┤   │
│   │  Middleware Pipeline:                                           │   │
│   │  ┌─────────┐ ┌──────────┐ ┌─────────────┐ ┌───────────────────┐ │   │
│   │  │  CORS   │→│ Security │→│ Correlation │→│ Exception Handler │ │   │
│   │  │         │ │ Headers  │ │     ID      │ │                   │ │   │
│   │  └─────────┘ └──────────┘ └─────────────┘ └───────────────────┘ │   │
│   └─────────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────────┘
                │                                      │
                │                                      │
                ▼                                      ▼
┌───────────────────────────────┐    ┌────────────────────────────────────┐
│        AZURE SQL              │    │      AZURE SIGNALR SERVICE         │
│   ┌───────────────────────┐   │    │   ┌────────────────────────────┐   │
│   │  Entity Framework     │   │    │   │  Serverless Mode           │   │
│   │  Core 10              │   │    │   │  └─ Broadcast messages     │   │
│   │  └─ Code-first        │   │    │   │  └─ User-targeted msgs     │   │
│   │  └─ Migrations        │   │    │   │  └─ Group messaging        │   │
│   │  └─ Soft deletes      │   │    │   └────────────────────────────┘   │
│   └───────────────────────┘   │    └────────────────────────────────────┘
└───────────────────────────────┘
                │
                ▼
┌───────────────────────────────┐
│    APPLICATION INSIGHTS       │
│    (Optional)                 │
│   └─ Structured logging       │
│   └─ Performance metrics      │
│   └─ Exception tracking       │
└───────────────────────────────┘
```

---

## Project Structure

### Modular "Tools" Architecture

The application is organized around **tools** - self-contained feature modules that can be developed, tested, and potentially extracted independently.

```
src/
├── api/                          # Backend
│   ├── Core/                     # Shared infrastructure
│   │   ├── Data/                 # DbContext, base entities
│   │   ├── Extensions/           # DI registration
│   │   ├── Logging/              # Correlation, structured logs
│   │   └── Middleware/           # Request pipeline
│   │
│   ├── Shared/                   # Cross-tool features
│   │   └── Health/               # Health check endpoints
│   │
│   ├── Tools/                    # Feature modules
│   │   └── Notes/                # Example tool
│   │       ├── Functions/        # HTTP triggers
│   │       ├── Models/           # Entities & DTOs
│   │       ├── Services/         # Business logic
│   │       └── Mappers/          # Entity ↔ DTO mapping
│   │
│   └── Hubs/                     # SignalR hub definitions
│
├── api.Tests/                    # Backend tests
│
└── frontend/                     # React SPA
    └── src/
        ├── core/                 # App-wide infrastructure
        │   ├── components/       # Layout, ErrorBoundary
        │   ├── services/         # API client
        │   └── utils/            # Helpers, validation
        │
        ├── shared/               # Shared across tools
        │   └── hooks/            # useSignalR, usePermissions
        │
        ├── tools/                # Feature modules
        │   └── notes/            # Example tool
        │       ├── components/   # UI components
        │       ├── pages/        # Route pages
        │       ├── hooks/        # useNotes
        │       ├── services/     # API calls
        │       └── types/        # TypeScript types
        │
        ├── theme/                # COBRA styling
        │   ├── cobraTheme.ts     # MUI theme config
        │   └── styledComponents/ # Branded components
        │
        └── admin/                # Admin features
```

### Key Principles

1. **Tools are self-contained** - Each tool has its own models, services, and UI
2. **Core provides infrastructure** - Database, logging, auth shared across tools
3. **Frontend mirrors backend** - Same `tools/` structure for consistency
4. **Tests are colocated** - Frontend tests next to source files

---

## Data Flow

### Request Lifecycle

```
1. Browser makes HTTP request
        │
        ▼
2. Azure Static Web App routes /api/* to Functions
        │
        ▼
3. Azure Functions middleware pipeline:
   ├─ CORS validation
   ├─ Security headers added
   ├─ Correlation ID generated
   └─ Exception handling wrapped
        │
        ▼
4. Function handler executes:
   ├─ Parse & validate request
   ├─ Call service layer
   ├─ Service uses DbContext
   └─ Return response + SignalR broadcast
        │
        ▼
5. Response flows back through middleware
        │
        ▼
6. Browser receives JSON response
        │
        ▼
7. SignalR notifies other connected clients
```

### Real-Time Updates

```
Client A creates note:
        │
        ▼
┌───────────────────────────────────────────────────────────┐
│ POST /api/notes                                           │
│ ┌─────────────┐    ┌─────────────┐    ┌───────────────┐  │
│ │ NotesFunction│ → │ NotesService│ → │ DbContext.Save │  │
│ └─────────────┘    └─────────────┘    └───────────────┘  │
│        │                                                  │
│        ▼                                                  │
│ Return: HTTP 201 + SignalR message                       │
└───────────────────────────────────────────────────────────┘
        │
        ├─────────────────────────────────────┐
        ▼                                     ▼
   Client A:                            Azure SignalR:
   Receives 201 Created                 Broadcasts to all clients
   Updates local state                          │
                                               ▼
                                         Client B, C, D:
                                         Receive "noteCreated" event
                                         Refresh notes list
```

---

## Technology Decisions

### Backend

| Choice | Rationale |
|--------|-----------|
| **.NET 10** | Latest LTS, best Azure integration |
| **Azure Functions Isolated** | Cold start improvements, dependency injection |
| **EF Core 10** | Code-first, migrations, LINQ queries |
| **Serilog** | Structured logging, Application Insights sink |

### Frontend

| Choice | Rationale |
|--------|-----------|
| **React 19** | Latest stable, concurrent features |
| **TypeScript 5** | Type safety, IDE support |
| **Vite 7** | Fast builds, HMR, modern tooling |
| **MUI 7** | COBRA-compatible components |

### Infrastructure

| Choice | Rationale |
|--------|-----------|
| **Azure Static Web Apps** | Global CDN, managed SSL, easy deploy |
| **Azure Functions** | Serverless, pay-per-execution, auto-scale |
| **Azure SQL** | Managed SQL Server, geo-replication |
| **Azure SignalR Service** | Managed WebSockets, serverless mode |

---

## Security Model

### Current (Development)

```
┌─────────────┐        X-User-Id Header        ┌─────────────┐
│   Browser   │ ───────────────────────────▶  │   Backend   │
│             │                                │             │
│  Mock user  │                                │  Trust      │
│  selector   │                                │  header     │
└─────────────┘                                └─────────────┘
```

### Production (Recommended)

See [docs/guides/AUTHENTICATION.md](../guides/AUTHENTICATION.md) for implementation details.

```
┌─────────────┐     ┌──────────────┐     ┌─────────────┐
│   Browser   │────▶│  Azure AD    │────▶│   Backend   │
│   (MSAL)    │◀────│  B2C         │◀────│  (JWT)      │
└─────────────┘     └──────────────┘     └─────────────┘
        │                                        │
        │  Access Token (JWT)                    │
        └────────────────────────────────────────┘
```

---

## Scalability Considerations

### Horizontal Scaling

| Component | Scaling Strategy |
|-----------|-----------------|
| Static Web App | Auto-scales globally via CDN |
| Azure Functions | Consumption plan auto-scales to demand |
| Azure SQL | Scale up/out, read replicas |
| Azure SignalR | Unit-based scaling (1 unit = 1,000 connections) |

### Performance Patterns

1. **Database**
   - Indexes on `UserId`, `IsDeleted`, `UpdatedAt`
   - `AsNoTracking()` for read-only queries
   - Connection pooling enabled

2. **API**
   - Response caching headers
   - Pagination for list endpoints
   - Correlation IDs for tracing

3. **Frontend**
   - Code splitting by route
   - React.lazy for tools
   - Optimistic UI updates

---

## Related Documentation

- [GETTING_STARTED.md](../GETTING_STARTED.md) - Setup instructions
- [CODING_STANDARDS.md](../CODING_STANDARDS.md) - Code conventions
- [DEPLOYMENT.md](../DEPLOYMENT.md) - Azure deployment
- [guides/](../guides/) - Implementation guides for production features
