# security-scanning/S03: Infrastructure-as-Code Security Scanning

**Priority:** P1
**Status:** Not Started

## Story

**As a** DevOps Engineer,
**I want** Bicep infrastructure templates scanned for security misconfigurations on every PR that changes infrastructure,
**So that** insecure configurations are identified before they are deployed to Azure.

## Context

Cadence's Azure infrastructure is defined as code using Bicep templates in the `infrastructure/` directory. These templates configure App Service, Azure SQL, SignalR, Static Web Apps, and supporting resources. Misconfigurations in these templates — such as overly permissive CORS rules, missing TLS version enforcement, or disabled HTTPS-only settings — become live security gaps the moment the template is deployed.

IaC scanning runs static analysis against the Bicep files before deployment, catching these issues at the PR stage when they are cheapest to fix. Checkov is the chosen tool because it supports Bicep natively, has well-maintained built-in checks for Azure resources, and produces SARIF output compatible with the consolidated reporting story (S06).

The workflow triggers only on changes to `infrastructure/**` to avoid unnecessary scan runs on PRs that only touch source code. This keeps CI costs low and signal-to-noise high.

Findings are initially soft-fail (informational only) to allow the team to review the initial baseline, suppress accepted-risk findings with documented rationale, and harden the Bicep files (see S05) before enforcing zero-findings on merge.

## Acceptance Criteria

### Workflow Trigger and Scope

- [ ] **Given** the IaC workflow exists at `.github/workflows/security-iac.yml`, **when** a PR targeting main includes changes to any file under `infrastructure/`, **then** the workflow triggers automatically
- [ ] **Given** the IaC workflow exists, **when** a commit pushed to main includes changes to any file under `infrastructure/`, **then** the workflow triggers automatically
- [ ] **Given** a PR includes no changes to files under `infrastructure/`, **when** CI runs, **then** the IaC workflow does NOT trigger (path filter is applied)

### Checkov Scan Configuration

- [ ] **Given** the IaC workflow triggers, **when** the scan step runs, **then** it uses the `bridgecrewio/checkov-action` GitHub Action
- [ ] **Given** the Checkov action runs, **when** configured, **then** `framework` is set to `bicep` so only Bicep-relevant checks are applied
- [ ] **Given** the Checkov action runs, **when** configured, **then** `soft_fail` is set to `true` so the workflow job succeeds even when findings are present
- [ ] **Given** a `.checkov.yml` config file exists in the repository root, **when** Checkov runs, **then** it reads `.checkov.yml` to apply check suppressions for accepted-risk findings
- [ ] **Given** a finding is suppressed in `.checkov.yml`, **when** a developer reads `.checkov.yml`, **then** each suppressed check ID has an inline comment explaining the accepted risk and the decision date

### Output and Artifacts

- [ ] **Given** the Checkov scan completes, **when** results are processed, **then** a SARIF file is written as output
- [ ] **Given** the SARIF file is produced, **when** the workflow step runs, **then** the SARIF file is uploaded as a GitHub Actions artifact named `iac-sarif` with 30-day retention
- [ ] **Given** the scan produces findings, **when** a developer reviews the workflow run, **then** the findings are visible in the step logs listing each check ID, resource name, and file location

### Known Finding Coverage

- [ ] **Given** the current Bicep templates are scanned, **when** Checkov runs without any suppressions, **then** it produces at least one finding related to TLS version configuration (e.g., App Service minimum TLS, SQL Server minimum TLS)
- [ ] **Given** the current Bicep templates are scanned, **when** Checkov runs without any suppressions, **then** it produces at least one finding related to CORS wildcard configuration (e.g., SignalR or App Service CORS `*`)

## Out of Scope

- Terraform scanning (Cadence uses Bicep exclusively)
- ARM template scanning (templates are authored as Bicep, not raw ARM JSON)
- Auto-remediation of findings (findings are reported; fixes are manual)
- Blocking PRs on IaC findings — the `soft_fail: true` setting is intentional for the initial phase

## Dependencies

- `infrastructure/` Bicep files must exist and be structured as modules (they are already in place)
- security-scanning/S05 (infrastructure hardening) should reference IaC scan findings to guide which Bicep changes to make

## Open Questions

- [ ] Should the `.checkov.yml` config file live in the repository root or in `infrastructure/`?
- [ ] After the hardening work in S05, should `soft_fail` be changed to `false` to enforce zero-findings on infrastructure PRs?
- [ ] Should the workflow also run on a weekly schedule (like SAST) to catch new Checkov rules being added to the rule database?

## Domain Terms

| Term | Definition |
|------|------------|
| **IaC** | Infrastructure-as-Code — defining cloud infrastructure in declarative files (Bicep, Terraform, ARM) rather than through the portal |
| **Bicep** | Microsoft's domain-specific language for defining Azure infrastructure; compiles to ARM JSON |
| **Checkov** | Open-source IaC scanning tool by Bridgecrew/Prisma Cloud that checks Bicep, Terraform, ARM, and other formats |
| **Soft fail** | Checkov configuration that allows the scan to produce findings without failing the CI job |
| **Check suppression** | Explicitly ignoring a Checkov check ID for a specific resource when the finding represents an accepted risk |
| **SARIF** | Static Analysis Results Interchange Format — standard JSON schema for exchanging static analysis results |
| **CSPM** | Cloud Security Posture Management — continuous assessment of cloud resource configuration |

## Technical Notes

- The `bridgecrewio/checkov-action` accepts a `directory` parameter; set this to `infrastructure/` to scope the scan
- SARIF output from Checkov is enabled via `output_format: sarif` and `output_file_path: results.sarif`
- `.checkov.yml` suppression format uses `skip-check:` with a list of check IDs (e.g., `CKV_AZURE_13`)
- Each suppressed check should include a comment in `.checkov.yml` citing the business reason (e.g., "SignalR CORS wildcard replaced with parameterized origins in S05 — suppressed until Bicep is updated")
- Checkov check IDs for common Azure findings: `CKV_AZURE_13` (App Service HTTPS only), `CKV_AZURE_14` (App Service min TLS), `CKV_AZURE_23` (SQL Server AD admin), `CKV_AZURE_24` (SQL Server auditing)
