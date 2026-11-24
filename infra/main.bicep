// =============================================================================
// ProyectoCafe - infraestructura base en Azure Container Apps
// =============================================================================
// Alcance: resource group. Crea ACR, Log Analytics, Container Apps Environment,
// Key Vault y las Container Apps pc-api / pc-portal. NO crea Azure SQL: la base
// ya existe y se configura fuera de esta plantilla (firewall o private endpoint).
//
// No hay secretos en esta plantilla ni en los parametros. Las apps leen
// DatabaseConnectionString y AuthJwtSigningKey de Key Vault con su identidad
// administrada asignada por el sistema.
//
// Despliegue y orden recomendado: infra/README.md
// =============================================================================

targetScope = 'resourceGroup'

@description('Region de Azure para todos los recursos.')
param location string = resourceGroup().location

@description('Prefijo corto para nombres generados (minusculas, numeros y guiones).')
@minLength(2)
@maxLength(6)
param namePrefix string = 'pc'

@description('Sufijo unico para ACR y Key Vault. Por defecto se deriva del resource group.')
@minLength(3)
@maxLength(13)
param nameSuffix string = uniqueString(resourceGroup().id)

@description('Nombre explicito del ACR. Vacio usa acr<prefijo><sufijo>.')
param acrName string = ''

@description('Nombre explicito del Key Vault. Vacio usa <prefijo>-kv-<sufijo>.')
param keyVaultName string = ''

@description('Nombre explicito del workspace de Log Analytics. Vacio usa <prefijo>-logs.')
param logAnalyticsName string = ''

@description('Nombre explicito del Container Apps Environment. Vacio usa <prefijo>-cae.')
param containerAppsEnvironmentName string = ''

@description('Nombre de la Container App de la API.')
@minLength(2)
@maxLength(30)
param apiAppName string = 'pc-api'

@description('Nombre de la Container App del Portal.')
@minLength(2)
@maxLength(30)
param portalAppName string = 'pc-portal'

@description('Imagen completa de la API (registro/repositorio:tag). Vacia usa el ACR creado y imageTag.')
param apiImage string = ''

@description('Imagen completa del Portal (registro/repositorio:tag). Vacia usa el ACR creado y imageTag.')
param portalImage string = ''

@description('Tag usado cuando apiImage o portalImage estan vacios.')
@minLength(1)
@maxLength(128)
param imageTag string = 'latest'

@description('FQDN publico del Portal para CORS, sin esquema (ej. portal.example.com). Vacio usa el FQDN del ingress.')
param portalPublicFqdn string = ''

@description('FQDN publico de la API para las variables del Portal, sin esquema. Vacio usa el FQDN del ingress.')
param apiPublicFqdn string = ''

@description('SKU del Azure Container Registry.')
@allowed([
  'Basic'
  'Standard'
  'Premium'
])
param acrSku string = 'Basic'

@description('SKU del workspace de Log Analytics.')
@allowed([
  'PerGB2018'
])
param logAnalyticsSku string = 'PerGB2018'

@description('Retencion de logs en dias (30-730).')
@minValue(30)
@maxValue(730)
param logAnalyticsRetentionInDays int = 30

@description('SKU del Key Vault.')
@allowed([
  'standard'
  'premium'
])
param keyVaultSku string = 'standard'

@description('Activa purge protection del Key Vault. No se puede desactivar despues.')
param keyVaultPurgeProtection bool = true

@description('Acceso publico al Key Vault. Disabled requiere private endpoint para las apps.')
@allowed([
  'Enabled'
  'Disabled'
])
param keyVaultPublicNetworkAccess string = 'Enabled'

@description('SameSite de las cookies de sesion. Usa None solo si Portal y API estan en sitios distintos.')
@allowed([
  'Lax'
  'Strict'
  'None'
])
param cookieSameSite string = 'Lax'

@description('Dominio de las cookies (atributo Domain), sin esquema ni punto inicial (ej. example.com). Vacio = host-only.')
param cookieDomain string = ''

@description('Nombre del secreto de Key Vault con la cadena de Azure SQL.')
@minLength(1)
@maxLength(127)
param databaseSecretName string = 'DatabaseConnectionString'

@description('Nombre del secreto de Key Vault con la clave HS256 de la API.')
@minLength(1)
@maxLength(127)
param jwtSecretName string = 'AuthJwtSigningKey'

@description('Crea las Container Apps. Usa false en la primera fase, antes de cargar imagenes y secretos.')
param deployApps bool = true

// -----------------------------------------------------------------------------
// Nombres resueltos
// -----------------------------------------------------------------------------

var resolvedAcrName = toLower(empty(acrName) ? 'acr${replace(namePrefix, '-', '')}${nameSuffix}' : acrName)
var resolvedKeyVaultName = toLower(empty(keyVaultName) ? '${namePrefix}-kv-${nameSuffix}' : keyVaultName)
var resolvedLogAnalyticsName = empty(logAnalyticsName) ? '${namePrefix}-logs' : logAnalyticsName
var resolvedEnvironmentName = empty(containerAppsEnvironmentName) ? '${namePrefix}-cae' : containerAppsEnvironmentName

var resolvedApiImage = empty(apiImage) ? '${resolvedAcrName}.azurecr.io/pc-api:${imageTag}' : apiImage
var resolvedPortalImage = empty(portalImage) ? '${resolvedAcrName}.azurecr.io/pc-portal:${imageTag}' : portalImage

// -----------------------------------------------------------------------------
// Recursos base
// -----------------------------------------------------------------------------

module logAnalytics 'modules/log-analytics.bicep' = {
  name: 'proyectocafe-log-analytics'
  params: {
    name: resolvedLogAnalyticsName
    location: location
    sku: logAnalyticsSku
    retentionInDays: logAnalyticsRetentionInDays
  }
}

resource registry 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: resolvedAcrName
  location: location
  sku: {
    name: acrSku
  }
  properties: {
    adminUserEnabled: false
    publicNetworkAccess: 'Enabled'
  }
}

resource vault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: resolvedKeyVaultName
  location: location
  properties: {
    tenantId: tenant().tenantId
    sku: {
      family: 'A'
      name: keyVaultSku
    }
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 90
    enablePurgeProtection: keyVaultPurgeProtection
    publicNetworkAccess: keyVaultPublicNetworkAccess
    networkAcls: {
      bypass: 'AzureServices'
      defaultAction: 'Allow'
      ipRules: []
      virtualNetworkRules: []
    }
  }
}

module environment 'modules/container-apps-environment.bicep' = {
  name: 'proyectocafe-container-apps-environment'
  params: {
    name: resolvedEnvironmentName
    location: location
    logAnalyticsWorkspaceName: logAnalytics.outputs.name
  }
}

// -----------------------------------------------------------------------------
// FQDNs
// -----------------------------------------------------------------------------
// El FQDN por defecto de una Container App es <nombre>.<defaultDomain> del
// environment. Se calcula sin referenciar el recurso de la app para evitar una
// dependencia circular entre la CORS de la API y la URL de la API en el Portal.

var defaultDomain = environment.outputs.defaultDomain
var apiFqdn = empty(apiPublicFqdn) ? '${apiAppName}.${defaultDomain}' : apiPublicFqdn
var portalFqdn = empty(portalPublicFqdn) ? '${portalAppName}.${defaultDomain}' : portalPublicFqdn
var portalOrigin = 'https://${portalFqdn}'
var apiOrigin = 'https://${apiFqdn}'

// -----------------------------------------------------------------------------
// Variables de entorno
// -----------------------------------------------------------------------------

var apiEnvironmentVariables = [
  {
    name: 'ASPNETCORE_ENVIRONMENT'
    value: 'Production'
  }
  {
    name: 'ASPNETCORE_URLS'
    value: 'http://+:8080'
  }
  {
    name: 'Database__ConnectionString'
    secretRef: 'db-conn'
  }
  {
    name: 'Auth__Jwt__SigningKey'
    secretRef: 'jwt-key'
  }
  {
    name: 'Auth__Cookie__Secure'
    value: 'true'
  }
  {
    name: 'Auth__Cookie__SameSite'
    value: cookieSameSite
  }
  {
    name: 'Auth__Cookie__Domain'
    value: cookieDomain
  }
  {
    name: 'Cors__AllowedOrigins__0'
    value: portalOrigin
  }
  {
    name: 'Logging__LogLevel__Default'
    value: 'Information'
  }
]

var portalEnvironmentVariables = [
  {
    name: 'PROYECTOCAFE_API_URL'
    value: apiOrigin
  }
  {
    name: 'PROYECTOCAFE_API_URL_INTERNAL'
    value: apiOrigin
  }
  {
    name: 'PROYECTOCAFE_AUTH_COOKIE_NAME'
    value: 'pc_at'
  }
  {
    name: 'APP_NAME'
    value: 'ProyectoCafe'
  }
  {
    name: 'NEXT_TELEMETRY_DISABLED'
    value: '1'
  }
]

var apiSecrets = [
  {
    name: 'db-conn'
    keyVaultUrl: '${vault.properties.vaultUri}secrets/${databaseSecretName}'
    identity: 'system'
  }
  {
    name: 'jwt-key'
    keyVaultUrl: '${vault.properties.vaultUri}secrets/${jwtSecretName}'
    identity: 'system'
  }
]

// -----------------------------------------------------------------------------
// Probes
// -----------------------------------------------------------------------------

var apiProbes = [
  {
    type: 'Startup'
    httpGet: {
      path: '/health'
      port: 8080
      scheme: 'HTTP'
    }
    initialDelaySeconds: 5
    periodSeconds: 10
    timeoutSeconds: 3
    failureThreshold: 30
    successThreshold: 1
  }
  {
    type: 'Liveness'
    httpGet: {
      path: '/health'
      port: 8080
      scheme: 'HTTP'
    }
    initialDelaySeconds: 15
    periodSeconds: 20
    timeoutSeconds: 3
    failureThreshold: 3
    successThreshold: 1
  }
  {
    type: 'Readiness'
    httpGet: {
      path: '/health/ready'
      port: 8080
      scheme: 'HTTP'
    }
    initialDelaySeconds: 10
    periodSeconds: 10
    timeoutSeconds: 3
    failureThreshold: 3
    successThreshold: 1
  }
]

var portalProbes = [
  {
    type: 'Startup'
    tcpSocket: {
      port: 3000
    }
    initialDelaySeconds: 5
    periodSeconds: 5
    timeoutSeconds: 3
    failureThreshold: 24
    successThreshold: 1
  }
  {
    type: 'Liveness'
    tcpSocket: {
      port: 3000
    }
    initialDelaySeconds: 15
    periodSeconds: 20
    timeoutSeconds: 3
    failureThreshold: 3
    successThreshold: 1
  }
  {
    type: 'Readiness'
    tcpSocket: {
      port: 3000
    }
    initialDelaySeconds: 10
    periodSeconds: 10
    timeoutSeconds: 3
    failureThreshold: 3
    successThreshold: 1
  }
]

// -----------------------------------------------------------------------------
// Container Apps
// -----------------------------------------------------------------------------

module apiApp 'modules/container-app.bicep' = if (deployApps) {
  name: 'proyectocafe-container-app-api'
  params: {
    name: apiAppName
    location: location
    managedEnvironmentId: environment.outputs.id
    image: resolvedApiImage
    registryServer: registry.properties.loginServer
    targetPort: 8080
    minReplicas: 1
    maxReplicas: 1
    cpu: '0.5'
    memory: '1Gi'
    environmentVariables: apiEnvironmentVariables
    secrets: apiSecrets
    probes: apiProbes
  }
}

module portalApp 'modules/container-app.bicep' = if (deployApps) {
  name: 'proyectocafe-container-app-portal'
  params: {
    name: portalAppName
    location: location
    managedEnvironmentId: environment.outputs.id
    image: resolvedPortalImage
    registryServer: registry.properties.loginServer
    targetPort: 3000
    minReplicas: 1
    maxReplicas: 2
    cpu: '0.5'
    memory: '1Gi'
    environmentVariables: portalEnvironmentVariables
    probes: portalProbes
  }
}

// -----------------------------------------------------------------------------
// RBAC
// -----------------------------------------------------------------------------
// Las identidades son system-assigned: el principalId solo existe despues de
// crear la app. Por eso el rol se asigna en un recurso separado, en la misma
// plantilla, y puede tardar unos minutos en propagarse.

var acrPullRoleDefinitionId = subscriptionResourceId(
  'Microsoft.Authorization/roleDefinitions',
  '7f951dda-4ed3-4680-a7ca-43fe172d538d'
)
var keyVaultSecretsUserRoleDefinitionId = subscriptionResourceId(
  'Microsoft.Authorization/roleDefinitions',
  '4633458b-17de-408a-b874-0445c86b69e6'
)

resource apiAcrPull 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (deployApps) {
  name: guid(resourceGroup().id, resolvedAcrName, apiAppName, acrPullRoleDefinitionId)
  scope: registry
  properties: {
    roleDefinitionId: acrPullRoleDefinitionId
    principalId: apiApp!.outputs.principalId
    principalType: 'ServicePrincipal'
  }
}

resource portalAcrPull 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (deployApps) {
  name: guid(resourceGroup().id, resolvedAcrName, portalAppName, acrPullRoleDefinitionId)
  scope: registry
  properties: {
    roleDefinitionId: acrPullRoleDefinitionId
    principalId: portalApp!.outputs.principalId
    principalType: 'ServicePrincipal'
  }
}

// Solo la API lee secretos de Key Vault. El Portal no necesita este rol.
resource apiKeyVaultSecretsUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (deployApps) {
  name: guid(resourceGroup().id, resolvedKeyVaultName, apiAppName, keyVaultSecretsUserRoleDefinitionId)
  scope: vault
  properties: {
    roleDefinitionId: keyVaultSecretsUserRoleDefinitionId
    principalId: apiApp!.outputs.principalId
    principalType: 'ServicePrincipal'
  }
}

// -----------------------------------------------------------------------------
// Salidas
// -----------------------------------------------------------------------------

@description('Nombre del ACR creado o referenciado.')
output acrName string = resolvedAcrName

@description('Login server del ACR, usado por el workflow de despliegue.')
output acrLoginServer string = registry.properties.loginServer

@description('Nombre del Key Vault.')
output keyVaultName string = resolvedKeyVaultName

@description('URI del Key Vault para las referencias de secretos.')
output keyVaultUri string = vault.properties.vaultUri

@description('Nombre del workspace de Log Analytics.')
output logAnalyticsName string = logAnalytics.outputs.name

@description('Nombre del Container Apps Environment.')
output containerAppsEnvironmentName string = environment.outputs.name

@description('Dominio por defecto del Container Apps Environment.')
output containerAppsDefaultDomain string = environment.outputs.defaultDomain

@description('Nombre de la Container App de la API.')
output apiAppName string = apiAppName

@description('Nombre de la Container App del Portal.')
output portalAppName string = portalAppName

@description('FQDN publico de la API (vacio si deployApps=false).')
output apiFqdn string = deployApps ? apiFqdn : ''

@description('FQDN publico del Portal (vacio si deployApps=false).')
output portalFqdn string = deployApps ? portalFqdn : ''

@description('FQDN real del ingress de pc-api, para verificar el valor calculado.')
output apiIngressFqdn string = deployApps ? apiApp!.outputs.fqdn : ''

@description('FQDN real del ingress de pc-portal, para verificar el valor calculado.')
output portalIngressFqdn string = deployApps ? portalApp!.outputs.fqdn : ''

@description('URL publica de la API (vacia si deployApps=false).')
output apiUrl string = deployApps ? apiOrigin : ''

@description('URL publica del Portal (vacia si deployApps=false).')
output portalUrl string = deployApps ? portalOrigin : ''

@description('Imagen de la API configurada en la plantilla.')
output apiImage string = resolvedApiImage

@description('Imagen del Portal configurada en la plantilla.')
output portalImage string = resolvedPortalImage

@description('Principal ID de la identidad de pc-api (vacio si deployApps=false).')
output apiPrincipalId string = deployApps ? apiApp!.outputs.principalId : ''

@description('Principal ID de la identidad de pc-portal (vacio si deployApps=false).')
output portalPrincipalId string = deployApps ? portalApp!.outputs.principalId : ''
