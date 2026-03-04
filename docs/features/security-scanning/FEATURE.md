# Feature: Security Scanning

**Phase:** Standard
**Status:** Ready

## Overview

Application security assessment program for Cadence, covering automated vulnerability scanning across the full stack: dependencies, source code (SAST), infrastructure-as-code (IaC), and the running application (DAST). All scan results feed into a consolidated OWASP-categorized report for stakeholder review.

This feature is a cross-cutting infrastructure concern. It adds security scanning to CI/CD pipelines rather than user-facing product capabilities. Cadence serves as the proving ground for this security program before the same approach is adopted by the COBRA5 product engineering team.

## Problem Statement

No automated security scanning exists in the Cadence codebase today. The team committed to SAST, DAST, dependency scanning, and cloud monitoring as part of the COBRA5 RFP. Without scanning, security vulnerabilities in third-party packages, source code, infrastructure configuration, and the running application can go undetected until customer-reported or exploited. Cadence needs a demonstrable, repeatable security posture that can be reviewed at Technical Review meetings and cited in future RFP responses.

## User Stories

| Story | Title | Priority | Status |
|-------|-------|----------|--------|
| [S01](./S01-dependency-scanning.md) | Dependency Vulnerability Scanning | P0 | Not Started |
| [S02](./S02-sast-code-analysis.md) | Static Application Security Testing (SAST) | P0 | Not Started |
| [S03](./S03-iac-security-scanning.md) | Infrastructure-as-Code Security Scanning | P1 | Not Started |
| [S04](./S04-dast-application-scanning.md) | Dynamic Application Security Testing (DAST) | P1 | Not Started |
| [S05](./S05-cloud-security-monitoring.md) | Cloud Security Monitoring and Infrastructure Hardening | P1 | Not Started |
| [S06](./S06-security-reporting.md) | Consolidated OWASP Security Report | P0 | Not Started |

## User Personas

| Persona | Interaction |
|---------|-------------|
| **Developer** | Receives scan results in CI, triages findings, fixes vulnerabilities before merge |
| **DevOps Engineer** | Configures scanning workflows, maintains suppression lists, monitors cloud security posture |
| **Administrator** | Reviews security posture at a system level, acts on Defender for Cloud alerts |
| **Customer Stakeholder** | Reviews consolidated OWASP report at Technical Review meetings to assess security posture |

## Key Concepts

| Term | Definition |
|------|------------|
| **SAST** | Static Application Security Testing — analysis of source code for vulnerabilities without executing the code |
| **DAST** | Dynamic Application Security Testing — scanning a running application by sending crafted requests to find runtime vulnerabilities |
| **SARIF** | Static Analysis Results Interchange Format — a standard JSON schema for exchanging static analysis results across tools |
| **OWASP Top Ten** | Open Web Application Security Project list of the ten most critical web application security risks |
| **Dependabot** | GitHub's built-in automated dependency update and vulnerability alert service |
| **IaC Scanning** | Analysis of infrastructure definition files (Bicep, Terraform, ARM) for security misconfigurations before deployment |
| **Defender for Cloud** | Microsoft's cloud security posture management (CSPM) and threat protection service for Azure resources |
| **Vulnerability** | A weakness in software or configuration that can be exploited by an attacker |
| **CVE** | Common Vulnerabilities and Exposures — a standardized identifier for publicly known security vulnerabilities |
| **Semgrep** | Open-source static analysis tool supporting custom and community rule packs across multiple languages |
| **Checkov** | Open-source IaC scanning tool that checks Terraform, Bicep, ARM, and other infrastructure definitions |
| **ZAP** | OWASP Zed Attack Proxy — open-source DAST tool for finding vulnerabilities in running web applications |
| **CSPM** | Cloud Security Posture Management — continuous assessment of cloud configuration against security best practices |

## Dependencies

- GitHub Actions CI/CD pipelines (`.github/workflows/`)
- Azure infrastructure definitions in `infrastructure/` (Bicep files)
- .NET 10 backend projects (`Cadence.Core`, `Cadence.WebApi`, `Cadence.Functions`)
- React/npm frontend in `src/frontend/`
- Stable UAT environment URL reachable from GitHub-hosted runners (required for DAST)
- Azure subscription with sufficient permissions to enable Defender for Cloud

## Acceptance Criteria (Feature-Level)

- [ ] Dependabot monitors all NuGet and npm dependencies and raises PRs for vulnerable packages
- [ ] SAST workflow scans C# and TypeScript source on every PR and produces a SARIF artifact
- [ ] IaC workflow scans Bicep templates on every PR touching `infrastructure/` and produces a SARIF artifact
- [ ] DAST workflow scans the running UAT environment weekly and produces an HTML report artifact
- [ ] Defender for Cloud is enabled on the Azure subscription with security contacts and continuous export configured
- [ ] Consolidated OWASP report artifact is available on demand, combining SARIF output from SAST, IaC, and dependency scans with OWASP Top Ten categorization

## Notes

- All workflows are initially non-blocking (informational only) until the team has triaged the initial finding set and established a baseline
- Scan suppression decisions must be documented in config files (`.semgrepignore`, `.checkov.yml`, `.zap/rules.tsv`) with rationale, not silently ignored
- The security scanning approach here mirrors the COBRA5 Azure DevOps pipeline approach, adapted for GitHub Actions and the Cadence tech stack
- DAST requires a network-accessible UAT environment; if UAT is not deployed, the DAST workflow skips gracefully rather than failing
