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
param emailCustomDomain = 'cobrasoftware.com'

// Security
param securityContactEmail = 'tbull@dynamis.com'

// Secrets — sourced from environment variables (set in CI/CD from GitHub secrets)
param jwtSecretKey = readEnvironmentVariable('JWT_SECRET_KEY', '')
param emailConnectionString = readEnvironmentVariable('EMAIL_CONNECTION_STRING', '')
