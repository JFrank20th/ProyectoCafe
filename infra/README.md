# Infraestructura Azure (Bicep)

Plantilla reproducible para ProyectoCafe en Azure Container Apps. Crea la base
de ejecución, pero **no crea Azure SQL**: la base ya existe y su conectividad se
configura aparte (firewall o private endpoint).

## Recursos

| Recurso | Nombre por defecto | Notas |
| --- | --- | --- |
| Azure Container Registry | `acr<prefijo><sufijo>` | SKU `Basic` por defecto, usuario admin deshabilitado. |
| Log Analytics | `<prefijo>-logs` | SKU `PerGB2018`, retención 30 días. |
| Container Apps Environment | `<prefijo>-cae` | Logs a Log Analytics, ingress público. |
| Key Vault | `<prefijo>-kv-<sufijo>` | RBAC, soft delete 90 días, purge protection. |
| Container App API | `pc-api` | Ingress externo, target port `8080`, réplicas 1/1. |
| Container App Portal | `pc-portal` | Ingress externo, target port `3000`, réplicas 1/2. |

La API usa probes HTTP `GET /health` (startup/liveness) y `GET /health/ready`
(readiness). El Portal usa probes TCP en `3000`.

La plantilla **no** crea: Azure SQL (servidor/base), redes o private endpoints,
zonas DNS privadas, dominios personalizados/certificados, secretos en Key Vault
ni la configuración de GitHub. Esos pasos se documentan abajo o quedan fuera del
alcance del repositorio.

```powershell
# Validar sintaxis y linter de Bicep (no debe imprimir nada)
az bicep build --file infra/main.bicep

# Ver el cambio sin aplicar nada
$Rg = "<resource-group>"
az deployment group what-if `
  --resource-group $Rg `
  --template-file infra/main.bicep `
  --parameters @infra/main.parameters.example.json
```

`what-if` no crea recursos. Los valores que solo existen en runtime (URI del
Key Vault, claves de Log Analytics, `principalId` de las identidades) aparecen
como desconocidos o calculados; eso es normal. Si aparece un error de validación,
corrige la plantilla o los parámetros antes de continuar.

## Parámetros

Copia `main.parameters.example.json` a un archivo local no versionado y ajusta
los valores del entorno. No hay secretos en los parámetros.

| Parámetro | Por defecto | Uso |
| --- | --- | --- |
| `location` | resource group | Región de todos los recursos. |
| `namePrefix` | `pc` | Prefijo de nombres generados (máx. 6). |
| `nameSuffix` | `uniqueString(resourceGroup().id)` | Sufijo de ACR/Key Vault. |
| `acrName` | derivado | Nombre explícito del ACR. |
| `keyVaultName` | derivado | Nombre explícito del Key Vault. |
| `logAnalyticsName` | derivado | Nombre explícito de Log Analytics. |
| `containerAppsEnvironmentName` | derivado | Nombre explícito del environment. |
| `apiAppName` / `portalAppName` | `pc-api` / `pc-portal` | Nombres de las apps. |
| `apiImage` / `portalImage` | vacío | Imagen completa; vacío usa ACR + `imageTag`. |
| `imageTag` | `latest` | Tag por defecto de las imágenes del ACR. |
| `portalPublicFqdn` | vacío | FQDN del Portal para CORS, **sin** `https://`. |
| `apiPublicFqdn` | vacío | FQDN de la API para el Portal, **sin** `https://`. |
| `acrSku` | `Basic` | `Basic`, `Standard` o `Premium`. |
| `logAnalyticsSku` | `PerGB2018` | SKU del workspace. |
| `logAnalyticsRetentionInDays` | `30` | Entre 30 y 730. |
| `keyVaultSku` | `standard` | `standard` o `premium`. |
| `keyVaultPurgeProtection` | `true` | No se puede desactivar después. |
| `keyVaultPublicNetworkAccess` | `Enabled` | `Disabled` exige private endpoint. |
| `cookieSameSite` | `Lax` | `None` solo si Portal y API son sitios distintos. |
| `cookieDomain` | vacío | Dominio del atributo `Domain` de las cookies (`example.com`), sin esquema ni punto inicial. Necesario para compartir `pc_at`/`pc_csrf` entre `app.example.com` y `api.example.com`. |
| `databaseSecretName` | `DatabaseConnectionString` | Nombre del secreto de SQL. |
| `jwtSecretName` | `AuthJwtSigningKey` | Nombre de la clave HS256. |
| `deployApps` | `true` | `false` en la primera fase, antes de imágenes y secretos. |

`portalPublicFqdn` y `apiPublicFqdn` son solo el host (por ejemplo
`portal.example.com`). Si se omiten, la plantilla calcula
`<nombre-app>.<defaultDomain>` del environment. La plantilla **no** crea dominios
personalizados ni certificados; si usas dominios propios, bíndalos antes a las
apps y luego pasa los FQDN.

Si usas dominios personalizados, pasa también `cookieDomain` (el dominio
registrable común) en **cada** despliegue de la plantilla. La plantilla declara
las variables de entorno como estado deseado: si vuelves a aplicarla sin
`portalPublicFqdn`, `apiPublicFqdn` y `cookieDomain`, las apps recuperan los FQDN
por defecto y la cookie vuelve a ser host-only.

## Orden de despliegue

### 1. Preparación

Requisitos:

- `az login` con permisos de `Contributor` y `User Access Administrator` (o
  `Owner`) en el resource group para crear recursos y asignar roles.
- Proveedores registrados: `Microsoft.App`, `Microsoft.ContainerRegistry`,
  `Microsoft.KeyVault`, `Microsoft.OperationalInsights`.
- Resource group existente y Azure SQL existente (servidor y base).
- Imágenes `pc-api` y `pc-portal` en el ACR antes de la fase 5 (crear las apps),
  o al menos antes de que la revisión quede saludable.

```powershell
$Rg = "<resource-group>"
$Location = "<region>"

az group show --name $Rg --query id -o tsv
az provider register --namespace Microsoft.App --wait
az provider register --namespace Microsoft.ContainerRegistry --wait
az provider register --namespace Microsoft.KeyVault --wait
az provider register --namespace Microsoft.OperationalInsights --wait
```

### 2. Fase base: ACR, Log Analytics, environment y Key Vault

La primera fase no crea las apps porque todavía no hay imágenes ni secretos:

```powershell
$BaseDeployment = "pc-infra-base"

az deployment group create `
  --resource-group $Rg `
  --name $BaseDeployment `
  --template-file infra/main.bicep `
  --parameters @infra/main.parameters.example.json `
  --parameters location=$Location `
  --parameters deployApps=false

$Outputs = az deployment group show `
  --resource-group $Rg `
  --name $BaseDeployment `
  --query properties.outputs `
  -o json | ConvertFrom-Json

$Acr = $Outputs.acrName.value
$Kv = $Outputs.keyVaultName.value
$AcrLoginServer = $Outputs.acrLoginServer.value
```

### 3. Construir y publicar las imágenes

Con el ACR ya creado, publica al menos una imagen inicial de cada app. Usa el
mismo contexto y Dockerfile que el CI (`server/api` y `Portal`):

```powershell
$Tag = "bootstrap-$(Get-Date -Format yyyyMMddHHmm)"

az acr build `
  --registry $Acr `
  --image "pc-api:$Tag" `
  --image pc-api:latest `
  --file Dockerfile `
  --platform linux/amd64 `
  server/api

az acr build `
  --registry $Acr `
  --image "pc-portal:$Tag" `
  --image pc-portal:latest `
  --file Dockerfile `
  --platform linux/amd64 `
  Portal
```

Después del primer despliegue, el job `deploy` de `.github/workflows/ci.yml`
construye y publica las imágenes con el tag del commit.

### 4. Cargar los secretos en Key Vault

El Key Vault usa RBAC. El usuario o service principal que carga secretos necesita
`Key Vault Secrets Officer` sobre el vault (el rol de plano de control no alcanza):

```powershell
$KvId = az keyvault show --name $Kv --resource-group $Rg --query id -o tsv
$MyObjectId = az ad signed-in-user show --query id -o tsv

az role assignment create `
  --assignee-object-id $MyObjectId `
  --assignee-principal-type User `
  --role "Key Vault Secrets Officer" `
  --scope $KvId
```

Espera unos minutos a que propague el rol y crea los dos secretos. No pegues
valores reales en archivos del repositorio ni en el historial de la shell.

```powershell
# La cadena de Azure SQL se escribe sin mostrarla.
$SecureConnection = Read-Host "Cadena de conexion de Azure SQL" -AsSecureString
$ConnectionString = [System.Net.NetworkCredential]::new('', $SecureConnection).Password
az keyvault secret set `
  --vault-name $Kv `
  --name DatabaseConnectionString `
  --value $ConnectionString
Remove-Variable SecureConnection, ConnectionString

# Clave HS256 aleatoria de 48 bytes (base64).
$JwtKey = [Convert]::ToBase64String(
  [System.Security.Cryptography.RandomNumberGenerator]::GetBytes(48))
az keyvault secret set `
  --vault-name $Kv `
  --name AuthJwtSigningKey `
  --value $JwtKey
Remove-Variable JwtKey
```

La plantilla **no** crea secretos: solo configura en `pc-api` las referencias
`keyVaultUrl` con `identity: system` de su identidad administrada.

### 5. Crear las Container Apps

Con imágenes y secretos listos, aplica la plantilla completa:

```powershell
$AppDeployment = "pc-infra-apps"

az deployment group create `
  --resource-group $Rg `
  --name $AppDeployment `
  --template-file infra/main.bicep `
  --parameters @infra/main.parameters.example.json `
  --parameters location=$Location `
  --parameters imageTag=$Tag `
  --parameters deployApps=true

az deployment group show `
  --resource-group $Rg `
  --name $AppDeployment `
  --query properties.outputs `
  -o json
```

Salidas principales: `apiFqdn`, `portalFqdn`, `apiUrl`, `portalUrl`,
`acrLoginServer`, `keyVaultUri`, `apiIngressFqdn`, `portalIngressFqdn`,
`apiPrincipalId` y `portalPrincipalId`.

Verifica que `apiFqdn`/`portalFqdn` coincidan con los FQDN reales del ingress. Si
usaste FQDN personalizados, confirma que el DNS y el certificado ya estén
enlazados a las apps.

```powershell
$Api = az deployment group show -g $Rg -n $AppDeployment `
  --query properties.outputs.apiFqdn.value -o tsv
$Portal = az deployment group show -g $Rg -n $AppDeployment `
  --query properties.outputs.portalFqdn.value -o tsv

Invoke-WebRequest "https://$Api/health" -UseBasicParsing | Select-Object StatusCode
Invoke-WebRequest "https://$Portal/login" -UseBasicParsing | Select-Object StatusCode
```

### 6. Conectar Azure SQL (fuera de esta plantilla)

La plantilla no crea la base ni reglas de red de SQL. Elige una opción:

- **Private endpoint (recomendado en producción):** private endpoint de Azure
  SQL en una VNet, zona DNS privada `privatelink.database.windows.net` y un
  environment de Container Apps integrado en esa VNet. Esta plantilla usa un
  environment público; añade `vnetConfiguration` y el private endpoint en una
  plantilla de red separada.
- **Firewall público acotado:** agrega solo las IP de salida necesarias. Las IP
  de salida de un environment de consumo no son una IP fija por app; para tener
  egreso estable usa NAT Gateway con el environment integrado en VNet. No abras
  `0.0.0.0` a todo Internet en producción.
- **Acceso desde el runner del CI:** el job de despliegue ya documenta la regla
  temporal de firewall o un runner self-hosted. Ver
  [`../DOCS/DESPLIEGUE-AZURE.md`](../DOCS/DESPLIEGUE-AZURE.md).

La cadena debe usar `Encrypt=True;TrustServerCertificate=False`. Si usas
Microsoft Entra ID, crea el usuario contenido en SQL y usa
`Authentication=Active Directory Default`; no guardes contraseñas en la cadena.

## CORS y cookies

La API es la única autoridad CORS. La plantilla configura:

```text
Cors__AllowedOrigins__0=https://<portalFqdn>
Auth__Cookie__Secure=true
Auth__Cookie__SameSite=<cookieSameSite>
Auth__Cookie__Domain=<cookieDomain>
```

- Si Portal y API comparten el dominio registrable (aunque sean subdominios
  distintos), `Lax` es correcto y `cookieDomain` debe ser ese dominio común
  (por ejemplo, `example.com`). Sin `Domain`, `pc_at` queda host-only en
  `api.example.com` y el middleware del Portal no la ve.
- Si están en dominios sin relación, usa `None`; `Secure` ya está en `true`. En
  ese caso no hay un `Domain` común: el Portal necesita consumir la API bajo su
  mismo origen (proxy inverso) para poder leer la sesión en el servidor.
- El Portal recibe `PROYECTOCAFE_API_URL` y `PROYECTOCAFE_API_URL_INTERNAL` con
  la URL pública de la API. Ambos valores se calculan con `apiFqdn`.
- No configures CORS en el ingress de Container Apps: duplicaría o contradiría
  al middleware de la API.

## Identidades administradas y RBAC

Cada app se crea con identidad `SystemAssigned`. La plantilla asigna:

- `AcrPull` sobre el ACR para `pc-api` y `pc-portal`.
- `Key Vault Secrets User` sobre el Key Vault solo para `pc-api` (es la única app
  que lee secretos).

Limitación de `system-assigned`: el `principalId` no existe hasta que la app se
crea. Por eso el rol se asigna en un recurso separado de la misma plantilla, con
un nombre derivado del nombre de la app (no del `principalId`, que no está
disponible al inicio del despliegue). Consecuencias:

- La asignación de roles puede tardar varios minutos en propagarse.
- La primera revisión puede fallar al jalar la imagen o al leer Key Vault si se
  crea antes de que propague RBAC. Vuelve a ejecutar el despliegue o reinicia la
  revisión cuando el rol esté activo.
- Si conviertes apps existentes, asigna la identidad y vuelve a desplegar; la
  identidad no se puede crear antes que la app (a diferencia de una
  user-assigned identity).

## CI: variables que salen de la plantilla

Después del despliegue, mapea las salidas al environment `production` de GitHub:

| Salida Bicep | Variable de GitHub |
| --- | --- |
| `acrName` | `ACR_NAME` |
| `keyVaultName` | `KEY_VAULT_NAME` |
| `apiAppName` | `API_APP_NAME` |
| `portalAppName` | `PORTAL_APP_NAME` |
| `apiFqdn` | `API_BASE_URL` (opcional, con `https://`) |
| `portalFqdn` | `PORTAL_BASE_URL` (opcional, con `https://`) |

`AZURE_RESOURCE_GROUP` es el resource group usado en el despliegue. El workflow
no usa `acrLoginServer` porque lo resuelve con `az acr show`.

> El job `deploy` actualiza las apps con el tag del commit. La plantilla, en
> cambio, declara la imagen como estado deseado (`apiImage`/`portalImage` o
> `imageTag`). Si vuelves a ejecutar Bicep sin pasar el tag actual, las apps
> volverán a `latest` u otro tag indicado. Para cambios de infraestructura
> posteriores al CI, pasa explícitamente las imágenes vigentes.

## Rollback

### Imágenes

Antes de desplegar, guarda la imagen actual de cada app. Si la revisión nueva
falla o el smoke test no pasa:

```powershell
$ApiPrevious = az containerapp show -n pc-api -g $Rg `
  --query properties.template.containers[0].image -o tsv
$PortalPrevious = az containerapp show -n pc-portal -g $Rg `
  --query properties.template.containers[0].image -o tsv

az containerapp update -n pc-api -g $Rg --image $ApiPrevious
az containerapp update -n pc-portal -g $Rg --image $PortalPrevious
```

Las apps usan `activeRevisionsMode: Single`; no hay tráfico dividido entre
revisiones. El rollback se hace con la imagen anterior.

### Infraestructura

1. Vuelve a la versión anterior de `infra/` (`git revert` o `git checkout`).
2. Ejecuta `what-if` y confirma que no se eliminarán recursos con datos.
3. Aplica con el modo incremental por defecto:

```powershell
az deployment group create `
  --resource-group $Rg `
  --name "pc-infra-rollback" `
  --template-file infra/main.bicep `
  --parameters @infra/main.parameters.example.json
```

El modo incremental no borra recursos que ya no están en la plantilla; para
eliminar recursos usa `--mode Complete` solo con revisión explícita y nunca sobre
Key Vault, ACR o SQL con datos. El Key Vault tiene purge protection: tras
borrarlo, el nombre no se puede reutilizar hasta que termine la retención.

### Base de datos

Las migraciones son forward-only. No hagas rollback de la imagen si el esquema
nuevo es incompatible; aplica una migración correctiva o restaura Azure SQL
según el procedimiento de la base. Ver [`../DOCS/ENV.md`](../DOCS/ENV.md).

## Validación local

```powershell
az bicep build --file infra/main.bicep
az deployment group what-if `
  --resource-group $Rg `
  --template-file infra/main.bicep `
  --parameters @infra/main.parameters.example.json
```

`az bicep build` debe terminar sin errores ni warnings. El archivo generado
`infra/main.json` está ignorado por git.
