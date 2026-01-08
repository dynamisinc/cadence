param location string
param webAppName string
param appInsightsConnectionString string
param signalRConnectionString string
param sqlConnectionString string
param tags object = {}

resource hostingPlan 'Microsoft.Web/serverfarms@2022-09-01' = {
  name: '${webAppName}-plan'
  location: location
  sku: {
    name: 'B1' // Basic tier for Web API
    tier: 'Basic'
  }
  properties: {}
  tags: tags
}

resource webApp 'Microsoft.Web/sites@2022-09-01' = {
  name: webAppName
  location: location
  kind: 'app'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: hostingPlan.id
    siteConfig: {
      netFrameworkVersion: 'v8.0'
      use32BitWorkerProcess: false
      appSettings: [
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsightsConnectionString
        }
        {
          name: 'ConnectionStrings__DefaultConnection'
          value: sqlConnectionString
        }
        {
          name: 'Azure__SignalR__ConnectionString'
          value: signalRConnectionString
        }
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: 'Production'
        }
      ]
    }
    httpsOnly: true
  }
  tags: tags
}

output name string = webApp.name
output defaultHostname string = webApp.properties.defaultHostName
