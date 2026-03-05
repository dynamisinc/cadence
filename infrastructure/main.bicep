targetScope = 'resourceGroup'

// ============================================================================
// Core Parameters
// ============================================================================

@description('Deployment environment (uat, prod)')
param environment string

@description('Azure region for all resources')
param location string = resourceGroup().location

@description('Application name used in resource naming')
param appName string = 'cadence'

// ============================================================================
// SQL Parameters
// ============================================================================

@description('SQL Server admin login')
param sqlAdminLogin string

@secure()
@description('SQL Server admin password')
param sqlAdminPassword string

@description('Override database name (UAT uses RG name; prod uses default sqldb-cadence-{env})')
param sqlDatabaseName string = 'sqldb-${appName}-${environment}'

@description('Entra ID admin login email for SQL Server')
param sqlEntraAdminLogin string = ''

@description('Entra ID admin object ID for SQL Server')
param sqlEntraAdminObjectId string = ''

// ============================================================================
// Hosting Parameters
// ============================================================================

@allowed(['functions', 'webapi', 'both'])
@description('Which hosting model to deploy')
param hostingModel string = 'webapi'

@description('Frontend URL for CORS and auth redirect (e.g., https://uat-cadence.cobrasoftware.com)')
param frontendUrl string = ''

// ============================================================================
// Communication / Email Parameters
// ============================================================================

@description('Deploy ACS + Email Service resources. Set false to share another environment\'s email service.')
param deployCommunication bool = true

@description('Custom email domain (e.g., cobrasoftware.com). Leave empty to use Azure managed domain only.')
param emailCustomDomain string = ''

@description('Email sender address when deployCommunication=false (e.g., DoNotReply@xxx.azurecomm.net from shared ACS)')
param emailSenderAddress string = ''

// ============================================================================
// Security Parameters
// ============================================================================

@description('Email for Defender for Cloud security alerts (deploy defender.bicep separately)')
param securityContactEmail string = ''

// ============================================================================
// Name Overrides (for resources that don't follow the standard pattern)
// ============================================================================

@description('Override Static Web App name (UAT: stapp-refapp-uat)')
param staticWebAppName string = 'stapp-${appName}-${environment}'

@description('GitHub repository URL for Static Web App')
param repositoryUrl string = 'https://github.com/dynamisinc/cadence'

// ============================================================================
// Secrets (set via parameter file or --parameters on CLI)
// ============================================================================

@secure()
@description('JWT signing key (32+ characters)')
param jwtSecretKey string = ''

@secure()
@description('Azure Communication Services connection string for email')
param emailConnectionString string = ''

// ============================================================================
// Resource Naming Convention
// ============================================================================
// Pattern matches actual Azure resources:
//   app-cadence-api-{env}  (webapp)
//   asp-cadence-{env}      (app service plan)
//   sql-cadence-{env}      (sql server)
//   stcadence{env}         (storage - no hyphens)
//   appi-cadence-{env}     (app insights)
//   log-cadence-{env}      (log analytics)
//   sigr-cadence-{env}     (signalr)
//   func-cadence-{env}     (function app)
//   acs-cadence-{env}      (communication services)
//   email-cadence-{env}    (email service)
// ============================================================================

var resourceSuffix = '${appName}-${environment}'
var storageName = 'st${appName}${environment}'
var logAnalyticsName = 'log-${resourceSuffix}'
var appInsightsName = 'appi-${resourceSuffix}'
var sqlServerName = 'sql-${resourceSuffix}'
var appServicePlanName = 'asp-${resourceSuffix}'
var webAppName = 'app-${appName}-api-${environment}'
var signalRName = 'sigr-${resourceSuffix}'
var functionAppName = 'func-${resourceSuffix}'
var acsName = 'acs-${resourceSuffix}'
var emailServiceName = 'email-${resourceSuffix}'

var tags = {
  Environment: environment
  Application: appName
  ManagedBy: 'Bicep'
}

// ============================================================================
// Module Deployments
// ============================================================================

module storage 'modules/storage.bicep' = {
  name: 'storageDeploy'
  params: {
    location: location
    storageAccountName: storageName
    tags: tags
  }
}

module logAnalytics 'modules/loganalytics.bicep' = {
  name: 'logAnalyticsDeploy'
  params: {
    location: location
    workspaceName: logAnalyticsName
    tags: tags
  }
}

module appInsights 'modules/appinsights.bicep' = {
  name: 'appInsightsDeploy'
  params: {
    location: location
    appInsightsName: appInsightsName
    logAnalyticsWorkspaceId: logAnalytics.outputs.id
    tags: tags
  }
}

module database 'modules/database.bicep' = {
  name: 'databaseDeploy'
  params: {
    location: location
    serverName: sqlServerName
    databaseName: sqlDatabaseName
    adminLogin: sqlAdminLogin
    adminPassword: sqlAdminPassword
    entraAdminLogin: sqlEntraAdminLogin
    entraAdminObjectId: sqlEntraAdminObjectId
    tags: tags
  }
}

module appServicePlan 'modules/appserviceplan.bicep' = if (hostingModel == 'webapi' || hostingModel == 'both') {
  name: 'appServicePlanDeploy'
  params: {
    location: location
    planName: appServicePlanName
    tags: tags
  }
}

module signalR 'modules/signalr.bicep' = if (hostingModel == 'functions' || hostingModel == 'both') {
  name: 'signalRDeploy'
  params: {
    location: location
    signalRName: signalRName
    allowedOrigins: frontendUrl != '' ? [frontendUrl] : []
    tags: tags
  }
}

module webApp 'modules/webapp.bicep' = if (hostingModel == 'webapi' || hostingModel == 'both') {
  name: 'webAppDeploy'
  params: {
    location: location
    webAppName: webAppName
    appServicePlanId: appServicePlan.outputs.id!
    appInsightsConnectionString: appInsights.outputs.connectionString
    sqlConnectionString: database.outputs.connectionString
    storageConnectionString: storage.outputs.connectionString
    frontendUrl: frontendUrl
    emailConnectionString: emailConnectionString
    emailDefaultSenderAddress: deployCommunication ? communication.outputs.managedDomainSenderAddress! : emailSenderAddress
    jwtSecretKey: jwtSecretKey
    tags: tags
  }
}

module functionApp 'modules/functionapp.bicep' = if (hostingModel == 'functions' || hostingModel == 'both') {
  name: 'functionAppDeploy'
  params: {
    location: location
    functionAppName: functionAppName
    storageAccountName: storage.outputs.name
    appInsightsConnectionString: appInsights.outputs.connectionString
    signalRConnectionString: (hostingModel == 'functions' || hostingModel == 'both') ? signalR.outputs.connectionString! : ''
    sqlConnectionString: database.outputs.connectionString
    tags: tags
  }
}

module staticWebApp 'modules/staticwebapp.bicep' = {
  name: 'staticWebAppDeploy'
  params: {
    location: location
    staticWebAppName: staticWebAppName
    repositoryUrl: repositoryUrl
    tags: tags
  }
}

module communication 'modules/communication.bicep' = if (deployCommunication) {
  name: 'communicationDeploy'
  params: {
    acsName: acsName
    emailServiceName: emailServiceName
    emailCustomDomain: emailCustomDomain
    tags: tags
  }
}

// ============================================================================
// Defender for Cloud (subscription-scoped) — deploy separately:
//   az deployment sub create --location <location> \
//     --template-file modules/defender.bicep \
//     --parameters logAnalyticsWorkspaceId='<logAnalytics.outputs.id>' \
//                  securityContactEmail='security@dynamis.com'
// ============================================================================

// ============================================================================
// Outputs
// ============================================================================

output webAppName string = (hostingModel == 'webapi' || hostingModel == 'both') ? webApp.outputs.name! : ''
output webAppHostname string = (hostingModel == 'webapi' || hostingModel == 'both') ? webApp.outputs.defaultHostname! : ''
output functionAppName string = (hostingModel == 'functions' || hostingModel == 'both') ? functionApp.outputs.name! : ''
output staticWebAppName string = staticWebApp.outputs.name
output staticWebAppHostname string = staticWebApp.outputs.defaultHostname
output staticWebAppDeploymentToken string = staticWebApp.outputs.deploymentToken
output sqlServerFqdn string = database.outputs.serverFqdn
output logAnalyticsWorkspaceId string = logAnalytics.outputs.id
output acsHostName string = deployCommunication ? communication.outputs.acsHostName! : ''
output emailSenderAddress string = deployCommunication ? communication.outputs.managedDomainSenderAddress! : emailSenderAddress
