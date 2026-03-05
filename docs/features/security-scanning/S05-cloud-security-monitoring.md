# security-scanning/S05: Cloud Security Monitoring and Infrastructure Hardening

**Priority:** P1
**Status:** Not Started

## Story

**As a** DevOps Engineer,
**I want** Microsoft Defender for Cloud enabled on our Azure subscription with security findings exported to Log Analytics,
**So that** we have continuous infrastructure monitoring and can demonstrate a measurable security posture to customers.

## Context

The earlier scanning stories (S01-S04) cover code and dependencies. This story addresses the cloud infrastructure layer itself — the Azure resources that host Cadence. Even if the code is secure, misconfigured Azure resources (open ports, weak TLS versions, wildcard CORS, FTPS enabled) are a direct attack surface.

This story has two complementary parts:

**Part 1 — Defender for Cloud:** Enable Microsoft's free Cloud Security Posture Management (CSPM) tier on the Azure subscription. This provides continuous assessment of resource configuration against security benchmarks (CIS, NIST, PCI-DSS), security alerts, and a Secure Score metric that stakeholders can cite in security reviews. A Log Analytics workspace receives the continuous export so findings persist beyond the portal's retention window.

**Part 2 — Infrastructure Hardening:** Fix known security misconfigurations in the Bicep templates identified by Checkov (S03). The specific gaps are: TLS version floor missing on SQL Server, App Service, and Function App; FTPS not explicitly disabled; SignalR CORS set to wildcard `*` rather than parameterized allowed origins. These are low-effort, high-impact fixes that directly improve Secure Score.

Because Bicep templates may have drifted from the actual deployed Azure resource state over time, the Bicep files must be reconciled with current resource state via `az` CLI exports before modifications are made, to avoid overwriting settings that were changed manually in the portal.

## Acceptance Criteria

### Defender for Cloud Bicep Module

- [ ] **Given** a new file `infrastructure/modules/defender.bicep` is created, **when** a developer reviews the module, **then** it declares a Log Analytics Workspace resource with the Free pricing tier (sufficient for this use case)
- [ ] **Given** the `defender.bicep` module is created, **when** a developer reviews the module, **then** it declares a Defender for Cloud `pricingTier: 'Free'` setting scoped to the subscription (free CSPM, no paid workload protection)
- [ ] **Given** the `defender.bicep` module is created, **when** a developer reviews the module, **then** it declares a security contacts resource with at least one email address and `alertNotifications: 'On'` and `alertsToAdmins: 'On'`
- [ ] **Given** the `defender.bicep` module is created, **when** a developer reviews the module, **then** it declares a continuous export configuration that routes security findings to the Log Analytics Workspace

### Main Bicep Integration

- [ ] **Given** `infrastructure/main.bicep` is updated, **when** a developer reviews it, **then** it includes a module reference to `infrastructure/modules/defender.bicep`
- [ ] **Given** App Insights is configured in the Bicep, **when** a developer reviews the App Insights resource, **then** it references the Log Analytics Workspace ID as its `workspaceResourceId` so telemetry and security data are colocated

### TLS Hardening

- [ ] **Given** the SQL Server Bicep resource is updated, **when** a developer reviews the resource definition, **then** `minimalTlsVersion` is set to `'1.2'`
- [ ] **Given** the App Service Bicep resource is updated, **when** a developer reviews the site config, **then** `minTlsVersion` is set to `'1.2'`
- [ ] **Given** the Function App Bicep resource is updated, **when** a developer reviews the site config, **then** `minTlsVersion` is set to `'1.2'`
- [ ] **Given** the App Service Bicep resource is updated, **when** a developer reviews the site config, **then** `ftpsState` is set to `'Disabled'`
- [ ] **Given** the Function App Bicep resource is updated, **when** a developer reviews the site config, **then** `ftpsState` is set to `'Disabled'`

### SignalR CORS Hardening

- [ ] **Given** the SignalR Bicep resource currently has a CORS `allowedOrigins: ['*']` setting, **when** the resource is updated, **then** the wildcard is replaced with a Bicep parameter (`allowedOrigins`) defaulting to an array of known origins (Static Web App URL, localhost for development)
- [ ] **Given** the `allowedOrigins` parameter is added, **when** a developer reviews `infrastructure/main.bicep`, **then** the parameter is passed through from the top-level Bicep file so it can be overridden per environment

### Resource State Reconciliation

- [ ] **Given** the Bicep files may have drifted from the actual Azure resource state, **when** the DevOps Engineer begins this story, **then** they first run `az resource show` (or equivalent) for each modified resource and compare current properties to the Bicep before making changes
- [ ] **Given** any discrepancies are found between Bicep and actual state, **when** the Bicep is updated, **then** the actual state is captured as a comment or note in the PR description so the team is aware of any drift that was corrected

## Out of Scope

- Defender for Cloud paid workload protection plans (Defender for App Service, Defender for SQL, Defender for Storage) — the free CSPM tier is sufficient for this story
- Azure Workbook or custom dashboard for customer-facing security reporting (deferred to a later story)
- Azure Lighthouse for multi-tenant subscription management
- Automated remediation of Defender for Cloud recommendations
- Network Security Groups (NSGs) — Cadence uses platform-managed networking (App Service, Azure SQL firewall rules)

## Dependencies

- `az` CLI access to the Azure subscription to export current resource state before modifying Bicep
- security-scanning/S03: IaC scanning should be in place so that the hardened Bicep files are automatically validated on the PR that introduces the changes

## Open Questions

- [ ] What email address should be configured as the Defender for Cloud security contact? (Team DL, or individual?)
- [ ] Should the Log Analytics Workspace be in the same resource group as the application, or a dedicated `security-rg` resource group?
- [ ] After initial deployment, should the Bicep `allowedOrigins` list be maintained in the Bicep file or moved to an App Configuration service to allow runtime updates without redeployment?
- [ ] Is there a need to enable Defender for Cloud's just-in-time VM access? (No VMs in this architecture, so likely not applicable.)

## Domain Terms

| Term | Definition |
|------|------------|
| **Defender for Cloud** | Microsoft's cloud security posture management and threat protection service for Azure resources |
| **CSPM** | Cloud Security Posture Management — continuous assessment and reporting of cloud resource configuration against security benchmarks |
| **Log Analytics Workspace** | Azure service that collects and queries log data; Defender for Cloud exports findings here for long-term retention |
| **Secure Score** | Defender for Cloud's numeric metric (0-100) representing the security posture of a subscription based on implemented recommendations |
| **Continuous export** | Defender for Cloud feature that streams security findings and alerts to a Log Analytics Workspace or Event Hub in real time |
| **TLS** | Transport Layer Security — cryptographic protocol for securing network communications; version 1.2 is the current minimum acceptable version |
| **FTPS** | FTP over TLS — a legacy protocol for file transfer; disabling it removes an unnecessary attack surface on App Service |
| **CORS** | Cross-Origin Resource Sharing — browser security mechanism that controls which origins can make requests to an API; wildcard `*` is overly permissive |
| **Bicep drift** | Condition where the deployed Azure resource configuration no longer matches the Bicep source template due to manual portal changes |

## Technical Notes

- The Defender for Cloud free CSPM tier (`pricingTier: 'Free'`) is enabled via a `Microsoft.Security/pricings` resource scoped to the subscription
- Log Analytics Workspace SKU `pergb2018` is the standard pay-as-you-go tier; the `Free` SKU has a 500MB/day cap which may be acceptable for a small deployment
- `az resource show --ids /subscriptions/{sub}/resourceGroups/{rg}/providers/{provider}/{name}` exports full resource JSON including properties not in the Bicep
- SignalR allowed origins should include the Static Web App production URL, the UAT URL, and `http://localhost:5173` for local development
- `ftpsState: 'Disabled'` is preferred over `'FtpsOnly'` because Cadence has no legitimate FTP use case
- App Insights workspace linking (`workspaceResourceId`) upgrades the App Insights instance from classic (local storage) to workspace-based (Log Analytics backed), enabling correlation between application telemetry and security events
