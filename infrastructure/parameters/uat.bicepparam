using '../main.bicep'

// ============================================================================
// UAT Environment Parameters
// Resource Group: rg-cadence-uat-centralus
// ============================================================================

param environment = 'uat'
param location = 'centralus'

// SQL Server
param sqlAdminLogin = 'sqladmin'
param sqlAdminPassword = readEnvironmentVariable('SQL_ADMIN_PASSWORD', '')
param sqlDatabaseName = 'rg-cadence-uat-centralus' // Legacy: DB was named after RG during portal creation
param sqlEntraAdminLogin = 'tbull@dynamis.com'
param sqlEntraAdminObjectId = 'b0461af1-3891-4923-9fe2-01f330a3cf89'

// Hosting
param hostingModel = 'webapi'
param frontendUrl = 'https://uat-cadence.cobrasoftware.com'

// Static Web App (legacy name from original 'refapp' scaffolding)
param staticWebAppName = 'stapp-refapp-uat'
param repositoryUrl = 'https://github.com/dynamisinc/cadence'

// Communication / Email
param emailCustomDomain = 'cobrasoftware.com'

// Security
param securityContactEmail = 'tbull@dynamis.com'

// Secrets — sourced from environment variables (set in CI/CD from GitHub secrets)
param jwtSecretKey = readEnvironmentVariable('JWT_SECRET_KEY', '')
param emailConnectionString = readEnvironmentVariable('EMAIL_CONNECTION_STRING', '')
