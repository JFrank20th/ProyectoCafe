@description('Nombre del workspace de Log Analytics.')
@minLength(4)
@maxLength(63)
param name string

@description('Region del workspace.')
param location string

@description('SKU del workspace.')
param sku string = 'PerGB2018'

@description('Retencion de datos en dias.')
@minValue(30)
@maxValue(730)
param retentionInDays int = 30

resource workspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: name
  location: location
  properties: {
    sku: {
      name: sku
    }
    retentionInDays: retentionInDays
    features: {
      enableLogAccessUsingOnlyResourcePermissions: true
    }
  }
}

@description('Resource ID del workspace.')
output id string = workspace.id

@description('Nombre del workspace.')
output name string = workspace.name
