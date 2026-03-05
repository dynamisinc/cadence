# security-scanning/S04: Dynamic Application Security Testing (DAST)

**Priority:** P1
**Status:** Not Started

## Story

**As a** DevOps Engineer,
**I want** the running Cadence application scanned for vulnerabilities on a weekly schedule,
**So that** runtime security issues that cannot be detected from source code alone are identified before they affect customers.

## Context

SAST (S02) and IaC scanning (S03) analyze code and configuration before deployment. DAST complements these by scanning the actual running application — sending real HTTP requests and inspecting responses for vulnerabilities like injection flaws, missing security headers, authentication bypasses, and insecure cookies. These vulnerabilities are only visible at runtime and cannot be found by reading source code.

OWASP ZAP (Zed Attack Proxy) is the chosen DAST tool because it is open-source, maintained by OWASP, has a well-supported GitHub Actions integration, and the baseline scan mode is safe to run against UAT environments without risk of destructive side effects. The baseline scan uses a passive spider and limited active checks — it does not attempt to exploit found vulnerabilities.

The scan targets the UAT environment because it is a realistic deployment of Cadence with real infrastructure (App Service, Azure SQL, SignalR) and is safe to probe without affecting any customer data. Production scanning is explicitly out of scope.

The workflow verifies the target is reachable before running ZAP so that a down UAT environment produces a clear, skipped result rather than a confusing ZAP failure.

## Acceptance Criteria

### Workflow Schedule and Triggers

- [ ] **Given** the DAST workflow exists at `.github/workflows/security-dast.yml`, **when** the configured weekly schedule fires (Monday 2AM UTC), **then** the workflow triggers automatically
- [ ] **Given** the DAST workflow exists, **when** a developer manually triggers it via `workflow_dispatch`, **then** the workflow runs using the provided target URL or falls back to the default UAT URL
- [ ] **Given** the `workflow_dispatch` trigger is configured, **when** a developer provides a custom URL input, **then** that URL is used as the ZAP scan target instead of the default UAT URL

### Target Reachability Check

- [ ] **Given** the workflow starts, **when** the target reachability check step runs, **then** it issues an HTTP request to the target URL and checks for a successful response (HTTP 2xx or 3xx)
- [ ] **Given** the target URL returns no response or a connection error, **when** the reachability check completes, **then** the step outputs a clear message ("Target unreachable, skipping DAST scan") and the workflow exits successfully without running ZAP
- [ ] **Given** the target URL responds successfully, **when** the reachability check completes, **then** the workflow proceeds to the ZAP scan step

### ZAP Baseline Scan

- [ ] **Given** the target is reachable, **when** the ZAP step runs, **then** it uses the `zaproxy/action-baseline` action (passive spider plus limited active checks)
- [ ] **Given** the ZAP step runs, **when** configured, **then** the target is the UAT environment URL stored as a GitHub Actions environment variable or repository secret
- [ ] **Given** a `.zap/rules.tsv` file exists in the repository, **when** ZAP runs, **then** it reads the rules file to classify alerts as WARN, FAIL, or IGNORE
- [ ] **Given** the `.zap/rules.tsv` file is configured, **when** a developer reads it, **then** each overridden rule has a comment explaining whether it is ignored (accepted risk), demoted to warning (known and tracked), or elevated to failure (must fix)
- [ ] **Given** the ZAP scan completes, **when** the duration is measured, **then** the scan completes within 10 minutes from the ZAP step start

### Output and Artifacts

- [ ] **Given** the ZAP scan completes, **when** results are processed, **then** ZAP produces an HTML report
- [ ] **Given** the HTML report is produced, **when** the workflow upload step runs, **then** the HTML report is uploaded as a GitHub Actions artifact named `dast-report` with 30-day retention
- [ ] **Given** the scan completes, **when** the workflow writes to `$GITHUB_STEP_SUMMARY`, **then** the summary includes the number of alerts found, broken down by risk level (High, Medium, Low, Informational)

## Out of Scope

- ZAP full active scan — the intrusive scan mode that attempts to exploit vulnerabilities and may cause data corruption or availability issues
- Authenticated scanning — scanning pages behind Cadence login requires passing API tokens or session cookies to ZAP; this is a future enhancement
- API-specific DAST using an OpenAPI specification — requires generating and maintaining an OpenAPI spec, deferred to a future story
- DAST scan triggered on every deployment (every PR merge)
- Scanning the production environment

## Dependencies

- A stable UAT environment URL must be deployed and accessible from GitHub-hosted runners (public internet)
- The UAT environment should have representative data to exercise more of the application surface area

## Open Questions

- [ ] Is the UAT URL stable enough that a weekly scan will reliably find the environment running? Or should the scan only run after a successful deployment workflow?
- [ ] Should scan results trigger a GitHub issue or team notification when High-risk alerts are found?
- [ ] After the baseline is established, should certain alert types be promoted to FAIL in `.zap/rules.tsv`?

## Domain Terms

| Term | Definition |
|------|------------|
| **DAST** | Dynamic Application Security Testing — scanning a running application by sending crafted HTTP requests to detect runtime vulnerabilities |
| **ZAP** | OWASP Zed Attack Proxy — open-source DAST tool for finding vulnerabilities in running web applications |
| **Baseline scan** | ZAP scan mode that uses a passive spider and limited active checks; safe for UAT environments, does not attempt exploitation |
| **Full active scan** | ZAP scan mode that actively probes for vulnerabilities and may cause unintended side effects; NOT used here |
| **Spider** | ZAP component that crawls the application by following links to discover pages and endpoints |
| **Alert** | ZAP term for a detected finding, classified by risk level: High, Medium, Low, or Informational |
| **rules.tsv** | ZAP configuration file mapping alert IDs to desired behavior: FAIL (break build), WARN (log only), or IGNORE (suppress) |
| **UAT** | User Acceptance Testing environment — a pre-production deployment used for testing before changes reach production |

## Technical Notes

- The `zaproxy/action-baseline` action wraps `zap-baseline.py` and handles scan lifecycle, report generation, and exit codes
- The `rules_file_name` parameter on `zaproxy/action-baseline` points to `.zap/rules.tsv`
- The reachability check can be implemented with `curl --fail --max-time 10 $TARGET_URL` in a bash step; use `continue-on-error: true` on this step and check the output in a conditional on the ZAP step
- Store the UAT URL as a GitHub Actions repository variable (`vars.UAT_URL`) so it can be updated without editing workflow files
- ZAP baseline scan by default exits with code 1 if any alerts are found; `fail_action: false` on the action prevents this from failing the workflow
