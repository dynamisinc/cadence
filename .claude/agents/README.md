# Cadence Claude Agents

This folder contains Claude Code agent definitions for the Cadence MSEL platform.

## Available Agents

| Agent | Purpose |
|-------|---------|
| `cadence-domain-agent` | HSEEP terminology and domain expertise |
| `azure-agent` | Azure infrastructure and deployment |
| `backend-agent` | .NET API development |
| `business-analyst-agent` | Requirements and user stories |
| `code-review` | Code quality and standards |
| `database-agent` | EF Core and schema design |
| `frontend-agent` | React/TypeScript development |
| `infrastructure-agent` | Phase 0 project setup |
| `orchestrator` | Multi-domain coordination |
| `realtime-agent` | SignalR and live updates |
| `story-agent` | Story tracking and refinement |
| `testing-agent` | TDD and test coverage |

## Usage

Agents are invoked automatically by Claude Code when their domain is relevant, or can be explicitly called:

```
@backend-agent Create the InjectService with CRUD operations
@database-agent Design the Exercise and Inject entities
@orchestrator Plan implementation of inject firing feature
```

## Key Differences from Reference App

These agents have been adapted for Cadence:

1. **Namespace**: `Cadence.*` (not `BullIT.*`)
2. **Domain**: HSEEP/MSEL terminology (not family dashboard)
3. **Roles**: Administrator, Exercise Director, Controller, Evaluator, Observer
4. **No Google OAuth**: Standard authentication
5. **No wall display**: Standard responsive design

## HSEEP Quick Reference

| Term | Meaning |
|------|---------|
| Exercise | Planned event to test capabilities |
| MSEL | Master Scenario Events List |
| Inject | Single scenario event |
| Fire | Deliver an inject to players |
| Controller | Delivers injects, manages flow |
| Evaluator | Records observations |

See `cadence-domain-agent.md` for complete HSEEP terminology.
