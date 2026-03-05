# security-scanning/S02: Static Application Security Testing (SAST)

**Priority:** P0
**Status:** Not Started

## Story

**As a** Developer,
**I want** static analysis of C# and TypeScript source code for security vulnerabilities on every PR,
**So that** security issues are caught before code is merged rather than discovered after deployment.

## Context

SAST tools analyze source code without executing it, finding common vulnerability patterns such as SQL injection, cross-site scripting (XSS), hardcoded secrets, insecure deserialization, and missing authentication checks. These are the vulnerabilities covered by the OWASP Top Ten.

Semgrep is the chosen SAST tool because it supports C#, TypeScript, and React with maintained community rule packs, integrates cleanly with GitHub Actions, and produces SARIF output compatible with the consolidated reporting story (S06). It requires no cloud account for the open-source rule packs used here.

The Roslyn security analyzers built into the .NET SDK provide a second, complementary layer of static analysis at compile time, catching .NET-specific anti-patterns that Semgrep's cross-language rules may miss.

SAST findings are initially non-blocking. The team needs time to triage the initial finding set, classify false positives, and establish a suppression baseline before enforcing zero-findings on merge. The workflow is structured so that blocking can be enabled by removing `continue-on-error: true` from a single step.

## Acceptance Criteria

### Workflow Trigger and Scope

- [ ] **Given** the SAST workflow exists at `.github/workflows/security-sast.yml`, **when** a PR targeting main is opened or updated, **then** the workflow triggers automatically
- [ ] **Given** the SAST workflow exists, **when** a commit is pushed directly to main, **then** the workflow triggers automatically
- [ ] **Given** the SAST workflow exists, **when** the configured weekly schedule fires (Sunday midnight UTC), **then** the workflow triggers automatically
- [ ] **Given** the workflow triggers, **when** Semgrep runs, **then** it scans the `src/` directory only and does NOT scan `src/Cadence.Core.Tests/`, migration files, or generated code

### Semgrep Rule Coverage

- [ ] **Given** the Semgrep scan runs, **when** configured, **then** it applies the `auto` rule pack (Semgrep-maintained language-appropriate rules)
- [ ] **Given** the Semgrep scan runs, **when** configured, **then** it applies the `p/csharp` rule pack for .NET-specific security patterns
- [ ] **Given** the Semgrep scan runs, **when** configured, **then** it applies the `p/typescript` rule pack for TypeScript security patterns
- [ ] **Given** the Semgrep scan runs, **when** configured, **then** it applies the `p/react` rule pack for React-specific security patterns (e.g., dangerouslySetInnerHTML usage)
- [ ] **Given** the Semgrep scan runs, **when** configured, **then** it applies the `p/secrets` rule pack to detect hardcoded credentials, API keys, and tokens
- [ ] **Given** the Semgrep scan runs, **when** configured, **then** it applies the `p/owasp-top-ten` rule pack to categorize findings by OWASP category
- [ ] **Given** a `.semgrepignore` file exists, **when** Semgrep runs, **then** it skips test projects, migration files, and any other paths listed in `.semgrepignore`

### Output and Artifacts

- [ ] **Given** the Semgrep scan completes, **when** results are processed, **then** a SARIF file is written as output
- [ ] **Given** the SARIF file is produced, **when** the workflow step runs, **then** the SARIF file is uploaded as a GitHub Actions artifact with 30-day retention
- [ ] **Given** the scan completes, **when** the workflow writes to `$GITHUB_STEP_SUMMARY`, **then** the summary includes the total number of findings found in this scan run
- [ ] **Given** the workflow includes a commented-out step for `github/codeql-action/upload-sarif@v3`, **when** a developer reads the workflow file, **then** the comment explains this step is ready for future GitHub Advanced Security (GHAS) enablement

### Build Behavior

- [ ] **Given** the Semgrep scan step has `continue-on-error: true`, **when** Semgrep finds vulnerabilities, **then** the workflow job completes successfully (non-blocking) and does not prevent merge
- [ ] **Given** the SAST workflow is a separate workflow file from the main CI workflow, **when** the CI `all-checks-passed` gate is evaluated, **then** the SAST workflow result does NOT block merge during the initial triage period

### Roslyn Security Analyzers

- [ ] **Given** all three .csproj files are configured, **when** a developer inspects `Cadence.Core.csproj`, `Cadence.WebApi.csproj`, and `Cadence.Functions.csproj`, **then** each contains `<AnalysisMode>Recommended</AnalysisMode>` in a `PropertyGroup`
- [ ] **Given** `AnalysisMode` is set to `Recommended`, **when** the backend is compiled in CI, **then** Roslyn security analyzer warnings are emitted as build warnings and are visible in CI logs

## Out of Scope

- Custom Semgrep rules written specifically for Cadence business logic
- Semgrep Pro or Semgrep Team features (cloud dashboard, cross-file analysis, proprietary rules)
- Blocking PRs on SAST findings — this will be enabled after initial triage period is complete
- SonarQube or other alternative SAST platforms

## Dependencies

- None — can run in parallel with security-scanning/S01

## Open Questions

- [ ] Which paths should be listed in `.semgrepignore` beyond test projects and migrations? (e.g., `node_modules`, `dist`, `.vite`)
- [ ] After the initial triage period, what finding threshold triggers blocking? (e.g., block on any High finding, or only on new findings compared to baseline?)
- [ ] Should the weekly scheduled scan run on main, or on the latest PR branch?

## Domain Terms

| Term | Definition |
|------|------------|
| **SAST** | Static Application Security Testing — analysis of source code for vulnerabilities without executing the application |
| **SARIF** | Static Analysis Results Interchange Format — a standard JSON schema for exchanging static analysis tool results |
| **Semgrep** | Open-source static analysis tool with community rule packs for multiple languages and frameworks |
| **Roslyn Analyzers** | .NET compiler-integrated code analyzers that surface security and quality warnings at build time |
| **Rule Pack** | A named collection of Semgrep rules, e.g., `p/owasp-top-ten` contains rules mapped to OWASP Top Ten categories |
| **GHAS** | GitHub Advanced Security — paid GitHub feature that enables CodeQL scanning and SARIF upload to the Security tab |
| **False Positive** | A finding reported by a scanner that does not represent a real vulnerability in context |

## Technical Notes

- Semgrep can be invoked via the `semgrep/semgrep-action` GitHub Action or directly via `pip install semgrep && semgrep --config ...`
- The SARIF output flag is `--sarif --output results.sarif`
- The `.semgrepignore` file follows `.gitignore` syntax
- `AnalysisMode` set to `Recommended` in the SDK-style project file enables the built-in .NET security analyzers without requiring additional NuGet packages
- If Semgrep's free-tier rate limits become an issue, the workflow can fall back to individual `--config` flags instead of the `auto` pack
