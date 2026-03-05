using '../main.bicep'

// ============================================================================
// Production Environment Parameters
// Resource Group: rg-cadence-prod-eastus2
// ============================================================================

param environment = 'prod'
param location = 'eastus2'

// SQL Server
param sqlAdminLogin = 'sqladmin'
param sqlAdminPassword = readEnvironmentVariable('SQL_ADMIN_PASSWORD', '')
param sqlEntraAdminLogin = 'tbull@dynamis.com'
param sqlEntraAdminObjectId = 'b0461af1-3891-4923-9fe2-01f330a3cf89'

// Hosting
param hostingModel = 'webapi'
param frontendUrl = 'https://cadence.cobrasoftware.com' // TODO: confirm production domain

// Static Web App
param repositoryUrl = 'https://github.com/dynamisinc/cadence'

// Communication / Email
// Production shares UAT's ACS + Email Service (acs-cadence-uat / email-cadence-uat).
// The EMAIL_CONNECTION_STRING secret should point to the UAT ACS resource.
// FUTURE: To give prod its own email service, set deployCommunication=true and use
// a subdomain (e.g., emailCustomDomain='prod.cobrasoftware.com') — see docs/INFRA_EMAIL.md
param deployCommunication = false
param emailSenderAddress = 'DoNotReply@f726439b-6085-4d29-b117-35b9ac24d3b2.azurecomm.net'

// Security
param securityContactEmail = 'tbull@dynamis.com'

// Secrets — sourced from environment variables (set in CI/CD from GitHub secrets)
param jwtSecretKey = readEnvironmentVariable('JWT_SECRET_KEY', '')
param emailConnectionString = readEnvironmentVariable('EMAIL_CONNECTION_STRING', '')
