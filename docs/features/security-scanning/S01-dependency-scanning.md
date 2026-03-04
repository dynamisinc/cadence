# security-scanning/S01: Dependency Vulnerability Scanning

**Priority:** P0
**Status:** Not Started

## Story

**As a** Developer,
**I want** automated scanning of NuGet and npm dependencies for known vulnerabilities,
**So that** I am alerted when third-party packages introduce security risks before they reach production.

## Context

Cadence depends on dozens of third-party packages across the .NET backend (NuGet) and React frontend (npm). Any of these packages may have publicly disclosed vulnerabilities (CVEs) that appear after the package version was initially adopted. Without automated scanning, the team has no systematic way to detect when a dependency becomes dangerous.

Dependency scanning is the highest-priority security story because it requires no additional tooling beyond what GitHub and the .NET SDK already provide. Dependabot is free for private repositories and operates without CI minutes. The `dotnet list package --vulnerable` command is built into the .NET SDK. Together these provide continuous coverage with minimal operational overhead.

The `security-scan` CI job must run independently of `build-backend` so that a compilation failure never masks a dependency vulnerability result. Dependency findings at High or Critical severity fail the build immediately to prevent vulnerable code from merging.

## Acceptance Criteria

### Dependabot Configuration

- [ ] **Given** the repository has a `.github/dependabot.yml` file, **when** Dependabot runs its weekly check, **then** it scans NuGet packages for all three project directories: `src/Cadence.Core`, `src/Cadence.WebApi`, and `src/Cadence.Functions`
- [ ] **Given** the repository has a `.github/dependabot.yml` file, **when** Dependabot runs its weekly check, **then** it scans npm packages in `src/frontend`
- [ ] **Given** the repository has a `.github/dependabot.yml` file, **when** Dependabot runs its weekly check, **then** it scans GitHub Actions in `.github/workflows` for outdated action versions
- [ ] **Given** a dependency has a known vulnerability, **when** Dependabot detects it, **then** Dependabot opens a PR with the fix and links the CVE in the PR description

### NuGet Vulnerability Scanning in CI

- [ ] **Given** a PR is raised or a push to main occurs, **when** the `security-scan` CI job runs, **then** it executes `dotnet list package --vulnerable --include-transitive` against `src/Cadence.Core/Cadence.Core.csproj`
- [ ] **Given** a PR is raised or a push to main occurs, **when** the `security-scan` CI job runs, **then** it executes `dotnet list package --vulnerable --include-transitive` against `src/Cadence.WebApi/Cadence.WebApi.csproj`
- [ ] **Given** a PR is raised or a push to main occurs, **when** the `security-scan` CI job runs, **then** it executes `dotnet list package --vulnerable --include-transitive` against `src/Cadence.Functions/Cadence.Functions.csproj`
- [ ] **Given** any scanned .NET project has a dependency with a High or Critical severity CVE, **when** the `security-scan` job completes, **then** the job exits with a non-zero status code and the build fails
- [ ] **Given** all scanned .NET projects have only Moderate or lower severity findings (or none), **when** the `security-scan` job completes, **then** the job exits successfully

### npm Vulnerability Scanning in CI

- [ ] **Given** a PR is raised or a push to main occurs, **when** the `security-scan` CI job runs, **then** it executes `npm audit --audit-level=high --omit=dev` in the `src/frontend` directory
- [ ] **Given** the npm audit finds any High severity vulnerability, **when** the `security-scan` job completes, **then** the job exits with a non-zero status code and the build fails
- [ ] **Given** the npm audit finds only Moderate or lower severity vulnerabilities (or none), **when** the `security-scan` job completes, **then** the job exits successfully
- [ ] **Given** the `--omit=dev` flag is applied, **when** the npm audit runs, **then** vulnerabilities in devDependencies-only packages do not cause a build failure

### CI Gate Integration

- [ ] **Given** the `security-scan` job is configured in the CI pipeline, **when** all checks are evaluated for merge eligibility, **then** `security-scan` is included in the `all-checks-passed` gate and blocks merge on failure
- [ ] **Given** the `security-scan` job is configured in the CI pipeline, **when** the job is triggered, **then** it does NOT list `build-backend` or any other job in its `needs:` declaration and runs independently

## Out of Scope

- License compliance scanning (e.g., detecting GPL packages in a commercial product)
- Container image scanning (no Docker images in this project)
- Software Bill of Materials (SBOM) generation
- Automatic merging of Dependabot PRs

## Dependencies

- None — this is the first security story and has no prerequisite stories

## Open Questions

- [ ] Should Moderate severity NuGet or npm findings generate a warning annotation on the PR without failing the build?
- [ ] Should the Dependabot schedule be weekly (lower noise) or daily (faster detection)?
- [ ] Should Dependabot group minor and patch updates into a single PR to reduce PR volume?

## Domain Terms

| Term | Definition |
|------|------------|
| **CVE** | Common Vulnerabilities and Exposures — a standardized identifier for a publicly known security vulnerability |
| **Dependabot** | GitHub's automated service that opens PRs to update vulnerable or outdated dependencies |
| **NuGet** | The package manager for .NET; packages are declared in `.csproj` files |
| **npm audit** | Command-line tool that checks npm packages against a database of known vulnerabilities |
| **Transitive dependency** | A package that is not directly referenced but is pulled in as a dependency of a dependency |
| **High / Critical severity** | CVSSv3 score ranges used to categorize vulnerability impact; High = 7.0-8.9, Critical = 9.0-10.0 |

## Technical Notes

- The `dotnet list package --vulnerable` command queries the NuGet vulnerability database and returns packages with known CVEs
- Use `--include-transitive` to catch vulnerabilities in indirect dependencies, not just direct references
- The CI job should restore packages (`dotnet restore`) before running the vulnerability check, as the vulnerability data is fetched during restore
- `npm audit --omit=dev` excludes devDependencies to focus on packages that ship to production
- Consider caching npm and NuGet packages in CI to keep the `security-scan` job fast
