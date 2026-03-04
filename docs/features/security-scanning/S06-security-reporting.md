# security-scanning/S06: Consolidated OWASP Security Report

**Priority:** P0
**Status:** Not Started

## Story

**As a** Customer Stakeholder,
**I want** a consolidated vulnerability scan report categorized by OWASP Top Ten that I can review at Technical Review meetings,
**So that** I can assess the application's security posture across all scan types in a single document.

## Context

This story is the primary deliverable of the entire security scanning feature. Stories S01 through S05 each produce scan artifacts — SARIF files from dependency scanning, SAST, and IaC scanning, plus an HTML report from DAST. Without consolidation, stakeholders would need to download and interpret multiple tool-specific artifacts from multiple workflow runs. That is not a viable review process.

The consolidated report merges all SARIF sources into one file, categorizes findings by OWASP Top Ten, and produces a single HTML document that can be handed to a customer stakeholder with no tooling knowledge required. The report must be producible on demand (manual trigger) so it can be regenerated immediately before a Technical Review meeting.

The `sarif-multitool` .NET global tool is used because it understands the SARIF specification deeply, can merge multiple SARIF files with proper deduplication, and can transform SARIF to HTML with OWASP categorization. It runs on the standard GitHub-hosted Ubuntu runner without additional infrastructure.

Because this workflow depends on artifacts from other workflows, it downloads the most recent artifacts from each upstream scan workflow run rather than running the scans itself. This keeps the report generation fast and separates the concern of scanning from the concern of reporting.

## Acceptance Criteria

### Workflow Schedule and Triggers

- [ ] **Given** the report workflow exists at `.github/workflows/security-report.yml`, **when** the configured weekly schedule fires (after all other security scans have had time to complete), **then** the workflow triggers automatically
- [ ] **Given** the report workflow exists, **when** a developer manually triggers it via `workflow_dispatch`, **then** the workflow runs immediately and generates a fresh report from the most recent available scan artifacts
- [ ] **Given** the `workflow_dispatch` trigger is configured, **when** a developer triggers the workflow from the GitHub Actions UI, **then** no additional input is required (the workflow uses sensible defaults)

### Artifact Collection

- [ ] **Given** the report workflow runs, **when** the artifact download step executes, **then** it downloads the most recent SARIF artifact from the latest successful run of the SAST workflow (`.github/workflows/security-sast.yml`)
- [ ] **Given** the report workflow runs, **when** the artifact download step executes, **then** it downloads the most recent SARIF artifact from the latest successful run of the IaC workflow (`.github/workflows/security-iac.yml`)
- [ ] **Given** the report workflow runs, **when** the artifact download step executes, **then** it downloads the most recent SARIF artifact from the latest successful dependency scan job in the CI workflow
- [ ] **Given** the report workflow runs, **when** the artifact download step executes, **then** it downloads the most recent ZAP HTML report artifact from the latest successful DAST workflow run
- [ ] **Given** one or more upstream scan artifacts are not available (the scan has never run or the artifact has expired), **when** the artifact download step runs, **then** the workflow logs a clear message identifying which artifact is missing and continues with the available artifacts

### SARIF Consolidation

- [ ] **Given** all SARIF artifacts are downloaded, **when** the consolidation step runs, **then** it installs `sarif-multitool` as a .NET global tool (`dotnet tool install -g Microsoft.CodeAnalysis.Sarif.Multitool`)
- [ ] **Given** `sarif-multitool` is installed, **when** the merge command runs, **then** it combines the SAST, IaC, and dependency SARIF files into a single merged SARIF file
- [ ] **Given** the merged SARIF file is produced, **when** a developer inspects it, **then** it is a valid SARIF 2.1.0 document containing results from all three source scans with their original tool names preserved
- [ ] **Given** the merged SARIF file is produced, **when** the HTML generation step runs, **then** `sarif-multitool` converts the merged SARIF to an HTML report with findings organized by OWASP Top Ten category where classification data is available

### Executive Summary

- [ ] **Given** the consolidated report is generated, **when** the workflow writes to `$GITHUB_STEP_SUMMARY`, **then** the summary includes the total number of findings across all scans
- [ ] **Given** the consolidated report is generated, **when** the workflow writes to `$GITHUB_STEP_SUMMARY`, **then** the summary includes a breakdown of findings by severity: Critical, High, Medium, Low
- [ ] **Given** the consolidated report is generated, **when** the workflow writes to `$GITHUB_STEP_SUMMARY`, **then** the summary includes a breakdown of findings by OWASP Top Ten category (e.g., A01: Broken Access Control: 3 findings)
- [ ] **Given** the consolidated report is generated, **when** the workflow writes to `$GITHUB_STEP_SUMMARY`, **then** the summary identifies the date range of the source scans used to produce the report

### Report Artifacts

- [ ] **Given** the merged SARIF file is produced, **when** the workflow upload step runs, **then** the merged SARIF file is uploaded as a GitHub Actions artifact named `security-report-sarif` with 90-day retention
- [ ] **Given** the HTML report is produced from merged SARIF, **when** the workflow upload step runs, **then** the HTML file is uploaded as a GitHub Actions artifact named `security-report-html` with 90-day retention
- [ ] **Given** the ZAP HTML report was downloaded from the DAST workflow, **when** the workflow upload step runs, **then** the ZAP HTML report is also uploaded as a separate artifact named `security-report-dast` with 90-day retention so stakeholders can access all three files from a single workflow run

## Out of Scope

- Automated email distribution of the report to stakeholders
- Creating GitHub Issues from individual findings automatically
- Trend analysis comparing findings across multiple report runs (e.g., finding count over time)
- Hosting the report as a web page (Azure Static Web App, GitHub Pages, etc.)
- Integration with Jira, Azure DevOps, or other issue trackers

## Dependencies

- security-scanning/S01: Dependency scanning must produce a SARIF artifact for the report to include
- security-scanning/S02: SAST workflow must produce a SARIF artifact for the report to include
- security-scanning/S03: IaC workflow must produce a SARIF artifact for the report to include
- security-scanning/S04: DAST workflow must produce an HTML artifact for the report to include

## Open Questions

- [ ] Should the consolidated report be generated after the weekly DAST scan completes (using a `workflow_run` trigger) or on a fixed schedule offset to allow all scans to finish first?
- [ ] If some upstream SARIF artifacts have expired (90-day limit), should the report generation fail or produce a partial report with a prominent warning?
- [ ] Should the report artifact name include the date (e.g., `security-report-html-2026-03-04`) to make versioned reports easily distinguishable, or is overwriting with a stable name preferred for ease of access?
- [ ] Is the `sarif-multitool` HTML output sufficient for stakeholder consumption, or will we need a custom HTML template for branding and readability?

## Domain Terms

| Term | Definition |
|------|------------|
| **OWASP Top Ten** | The Open Web Application Security Project's list of the ten most critical web application security risks, updated periodically |
| **SARIF** | Static Analysis Results Interchange Format — a standard JSON schema for exchanging static analysis results; enables tooling to merge and transform scan output |
| **sarif-multitool** | A .NET global tool from Microsoft that merges, validates, and transforms SARIF files |
| **Merged SARIF** | A single SARIF file combining results from multiple scan tools, with each tool's results attributed to the originating tool |
| **Executive summary** | A concise, non-technical overview of security findings suitable for stakeholder review without deep security expertise |
| **Artifact retention** | The duration GitHub Actions stores uploaded artifacts before automatic deletion; 90 days is used here to cover quarterly review cycles |
| **workflow_dispatch** | GitHub Actions trigger that allows a workflow to be started manually from the GitHub UI or via the API |
| **Secure Score** | (In context of Defender for Cloud reports) A numeric measure of security posture; separate from this workflow but potentially cited in the same stakeholder report |

## Technical Notes

- GitHub Actions artifact download across workflows requires using `dawidd6/action-download-artifact` or the GitHub REST API (`/repos/{owner}/{repo}/actions/artifacts`) since the built-in `actions/download-artifact` only works within the same workflow run
- `sarif-multitool merge` command syntax: `sarif merge *.sarif --output merged.sarif`
- `sarif-multitool` HTML export: `sarif export merged.sarif --output report.html`
- If `sarif-multitool` HTML output lacks OWASP categorization, the `p/owasp-top-ten` Semgrep rule pack tags applied in S02 will populate the `taxa` field in SARIF results, enabling category grouping
- The 90-day artifact retention covers quarterly Technical Review cycles (reviews typically occur every 6-8 weeks; 90 days provides a comfortable buffer)
- Consider pinning the `sarif-multitool` version (`dotnet tool install -g Microsoft.CodeAnalysis.Sarif.Multitool --version x.y.z`) for reproducible report generation
