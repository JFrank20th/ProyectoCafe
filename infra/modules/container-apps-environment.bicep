@description('Nombre del Container Apps Environment.')
@minLength(2)
@maxLength(32)
param name string

@description('Region del environment.')
param location string

@description('Nombre del workspace de Log Analytics que recibe los logs.')
param logAnalyticsWorkspaceName string

resource workspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' existing = {
  name: logAnalyticsWorkspaceName
}

resource environment 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: name
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: workspace.properties.customerId
        sharedKey: workspace.listKeys().primarySharedKey
      }
    }
  }
}

@description('Resource ID del environment.')
output id string = environment.id

@description('Nombre del environment.')
output name string = environment.name

@description('Dominio por defecto del environment.')
output defaultDomain string = environment.properties.defaultDomain
