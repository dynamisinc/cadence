targetScope = 'resourceGroup'

param environment string = 'dev'
param appName string = 'refapp'
param location string = resourceGroup().location
param sqlAdminLogin string
@secure()
param sqlAdminPassword string
@allowed([
  'functions'
  'webapi'
  'both'
])
param hostingModel string = 'webapi'

var resourceSuffix = '${appName}-${environment}'
var uniqueSuffix = uniqueString(resourceGroup().id)
var shortSuffix = substring(uniqueSuffix, 0, 4)

// Resource Names
var storageName = 'st${replace(resourceSuffix, '-', '')}${shortSuffix}'
var appInsightsName = 'appi-${resourceSuffix}'
var sqlServerName = 'sql-${resourceSuffix}-${shortSuffix}'
var sqlDbName = 'sqldb-${resourceSuffix}'
var signalRName = 'sigr-${resourceSuffix}'
var functionAppName = 'func-${resourceSuffix}'
var webAppName = 'web-${resourceSuffix}'
var staticWebAppName = 'stapp-${resourceSuffix}'

var tags = {
  Environment: environment
  Application: appName
  ManagedBy: 'Bicep'
}

module storage 'modules/storage.bicep' = {
  name: 'storageDeploy'
  params: {
    location: location
    storageAccountName: storageName
    tags: tags
  }
}

module appInsights 'modules/appinsights.bicep' = {
  name: 'appInsightsDeploy'
  params: {
    location: location
    appInsightsName: appInsightsName
    tags: tags
  }
}

module database 'modules/database.bicep' = {
  name: 'databaseDeploy'
  params: {
    location: location
    serverName: sqlServerName
    databaseName: sqlDbName
    adminLogin: sqlAdminLogin
    adminPassword: sqlAdminPassword
    tags: tags
  }
}

module signalR 'modules/signalr.bicep' = {
  name: 'signalRDeploy'
  params: {
    location: location
    signalRName: signalRName
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
    signalRConnectionString: signalR.outputs.connectionString
    sqlConnectionString: database.outputs.connectionString
    tags: tags
  }
}

module webApp 'modules/webapp.bicep' = if (hostingModel == 'webapi' || hostingModel == 'both') {
  name: 'webAppDeploy'
  params: {
    location: location
    webAppName: webAppName
    appInsightsConnectionString: appInsights.outputs.connectionString
    signalRConnectionString: signalR.outputs.connectionString
    sqlConnectionString: database.outputs.connectionString
    tags: tags
  }
}

module staticWebApp 'modules/staticwebapp.bicep' = {
  name: 'staticWebAppDeploy'
  params: {
    location: 'eastus2' // Static Web Apps are global but resource needs a region. EastUS2 is common.
    staticWebAppName: staticWebAppName
    tags: tags
  }
}

output functionAppName string = (hostingModel == 'functions' || hostingModel == 'both') ? functionApp.outputs.name : ''
output webAppName string = (hostingModel == 'webapi' || hostingModel == 'both') ? webApp.outputs.name : ''
output staticWebAppName string = staticWebApp.outputs.name
output staticWebAppHostname string = staticWebApp.outputs.defaultHostname
