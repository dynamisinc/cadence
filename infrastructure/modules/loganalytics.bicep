// Log Analytics Workspace for security monitoring
// Used by: Defender for Cloud, Application Insights, diagnostic settings
// Cost: Free tier (500 MB/day ingestion, 7-day retention)

param location string
param workspaceName string
param tags object = {}

resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: workspaceName
  location: location
  properties: {
    sku: {
      name: 'Free'
    }
    retentionInDays: 7
  }
  tags: tags
}

output id string = logAnalyticsWorkspace.id
output name string = logAnalyticsWorkspace.name
