@description('Nombre de la Container App.')
@minLength(2)
@maxLength(32)
param name string

@description('Region de la Container App.')
param location string

@description('Resource ID del Container Apps Environment.')
param managedEnvironmentId string

@description('Imagen completa del contenedor.')
@minLength(1)
param image string

@description('Login server del registro privado. Vacio desactiva la configuracion de registro.')
param registryServer string = ''

@description('Puerto interno del contenedor.')
param targetPort int

@description('Expone el ingress a Internet.')
param external bool = true

@description('Permite trafico HTTP ademas de HTTPS.')
param allowInsecure bool = false

@description('Minimo de replicas.')
@minValue(0)
param minReplicas int = 1

@description('Maximo de replicas.')
@minValue(1)
param maxReplicas int = 1

@description('CPU del contenedor, como valor JSON (ej. 0.5).')
param cpu string = '0.5'

@description('Memoria del contenedor (ej. 1Gi).')
param memory string = '1Gi'

@description('Variables de entorno del contenedor.')
param environmentVariables array = []

@description('Secretos de la Container App, incluidos keyVaultUrl e identity.')
param secrets array = []

@description('Probes de startup, liveness y readiness.')
param probes array = []

var registryConfiguration = empty(registryServer)
  ? []
  : [
      {
        server: registryServer
        identity: 'system'
      }
    ]

resource app 'Microsoft.App/containerApps@2024-03-01' = {
  name: name
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    managedEnvironmentId: managedEnvironmentId
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: external
        targetPort: targetPort
        allowInsecure: allowInsecure
        transport: 'auto'
        traffic: [
          {
            latestRevision: true
            weight: 100
          }
        ]
      }
      registries: registryConfiguration
      secrets: secrets
    }
    template: {
      containers: [
        {
          name: name
          image: image
          resources: {
            cpu: json(cpu)
            memory: memory
          }
          env: environmentVariables
          probes: probes
        }
      ]
      scale: {
        minReplicas: minReplicas
        maxReplicas: maxReplicas
      }
    }
  }
}

@description('Resource ID de la Container App.')
output id string = app.id

@description('FQDN del ingress de la Container App.')
output fqdn string = app.properties.configuration.ingress.fqdn

@description('Principal ID de la identidad system-assigned.')
output principalId string = app.identity.principalId
