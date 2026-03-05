# Email Service: Per-Environment Migration Guide

## Current Setup (Shared)

Production shares UAT's Azure Communication Services and Email Service:

- **ACS resource:** `acs-cadence-uat` (in `rg-cadence-uat-centralus`)
- **Email Service:** `email-cadence-uat` with custom domain `cobrasoftware.com`
- **Sender address:** `DoNotReply@f726439b-6085-4d29-b117-35b9ac24d3b2.azurecomm.net` (Azure managed domain)
- **Prod `EMAIL_CONNECTION_STRING`** GitHub secret points to UAT's ACS connection string

Both environments send email through the same ACS resource. This works fine but means:
- UAT outage affects prod email
- Usage/billing is combined
- Can't have different sender domains per environment

## Migration to Per-Environment Email (Option 3: Subdomains)

When you're ready to give each environment its own email service:

### 1. DNS Setup

Create subdomain DNS records for each environment:

| Environment | Email Domain | DNS Records Needed |
|---|---|---|
| UAT | `uat.cobrasoftware.com` | SPF, DKIM (3 CNAMEs), DMARC |
| Production | `cobrasoftware.com` | Already configured |

### 2. Update Parameter Files

**`uat.bicepparam`** — add custom domain:
```
param emailCustomDomain = 'uat.cobrasoftware.com'
```

**`prod.bicepparam`** — enable communication module:
```
// Remove these:
// param deployCommunication = false
// param emailSenderAddress = 'DoNotReply@...'

// Add these:
param deployCommunication = true       // or remove — defaults to true
param emailCustomDomain = 'cobrasoftware.com'
```

### 3. Update GitHub Secrets

Set a **separate** `EMAIL_CONNECTION_STRING` for the `prod` environment, pointing to the new `acs-cadence-prod` resource (created by the deployment).

### 4. Deploy

1. Deploy UAT first: `deploy-infrastructure.yml` with `uat` environment
2. Verify UAT email domain DNS validation completes (check Azure Portal > Email Service > Domains)
3. Deploy prod: `deploy-infrastructure.yml` with `prod` environment
4. Verify prod email domain DNS validation
5. Update `EMAIL_CONNECTION_STRING` secret for prod with the new ACS connection string

### 5. Verify

- Send a test email from each environment
- Confirm sender shows correct domain (`DoNotReply@uat.cobrasoftware.com` vs `DoNotReply@cobrasoftware.com`)

## Architecture Reference

```
Shared (current):
  UAT webapp  ──> acs-cadence-uat ──> cobrasoftware.com
  Prod webapp ──> acs-cadence-uat ──> cobrasoftware.com

Per-environment (future):
  UAT webapp  ──> acs-cadence-uat  ──> uat.cobrasoftware.com
  Prod webapp ──> acs-cadence-prod ──> cobrasoftware.com
```
