# Despliegue en Azure Container Apps

El job `deploy` de `.github/workflows/ci.yml` publica `pc-api` y `pc-portal` en
Azure Container Apps. Se ejecuta **solo** en un `push` a `main` (o manualmente
desde `main` con `deploy=true`) y siempre después de que pase el job obligatorio
`CI required`.

No hay secretos en el repositorio ni en el workflow. La autenticación con Azure
usa OIDC federado y la cadena de conexión de Azure SQL se lee de Key Vault en
tiempo de ejecución.

## Configuración final (resumen)

| Elemento | Valor de referencia |
| --- | --- |
| Portal | `https://app.tudominio.com` (Container App `pc-portal`, puerto `3000`). |
| API | `https://api.tudominio.com` (Container App `pc-api`, puerto `8080`). |
| Certificados | Gestionados por Container Apps; validación CNAME + TXT `asuid.<subdominio>`. |
| CORS API | `Cors__AllowedOrigins__0=https://app.tudominio.com` (origen exacto, sin barra final). |
| Cookies | `SameSite=Lax`, `Secure=true`, `Domain=tudominio.com` (mismo dominio registrable). |
| Secretos | `DatabaseConnectionString` y `AuthJwtSigningKey` en Key Vault, leídos con la identidad de `pc-api`. |
| Azure SQL | Private endpoint + VNet (recomendado) o regla `Allow Azure services` con el riesgo documentado. |
| Verificación | `pwsh -File scripts/smoke-azure.ps1 -ApiBaseUrl ... -PortalBaseUrl ...`. |

Los valores concretos de cada entorno se pasan por variables; no se versionan.

## Infraestructura como código

[`infra/`](../infra/README.md) contiene la plantilla Bicep que crea ACR, Log
Analytics, el Container Apps Environment, Key Vault, las identidades y las apps
`pc-api` y `pc-portal`. La plantilla **no** crea Azure SQL: la base y su
conectividad se gestionan aparte.

```powershell
az bicep build --file infra/main.bicep
az deployment group what-if `
  --resource-group "<resource-group>" `
  --template-file infra/main.bicep `
  --parameters @infra/main.parameters.example.json
```

El orden completo (fase base sin apps, imágenes, secretos, apps), las salidas
para el environment de GitHub y el rollback están en
[`infra/README.md`](../infra/README.md). El resto de este documento describe el
workflow de despliegue continuo y los recursos que ese workflow espera.

## Flujo del job

| Paso | Acción |
| --- | --- |
| 1 | Valida que existan las variables del environment `production`. |
| 2 | Inicia sesión en Azure con `azure/login@v2` y OIDC (sin secretos). |
| 3 | Resuelve ACR, las etiquetas de imagen y las imágenes actuales para rollback. |
| 4 | Construye y publica `pc-api` con `az acr build` usando `:<sha-corto>` y `:latest`. |
| 5 | Construye y publica `pc-portal` con `az acr build` usando `:<sha-corto>` y `:latest`. |
| 6 | Lee la cadena de Azure SQL desde Key Vault; el valor se enmascara en los logs. |
| 7 | Aplica `tools/sqlserver-migrate-current.sql` con `azure/sql-action` (go-sqlcmd). |
| 8 | Actualiza `pc-api` con `az containerapp update --image` y espera una revisión activa y saludable. |
| 9 | Hace `GET /health` contra `pc-api`; si no responde `200`, falla el job. |
| 10 | Actualiza `pc-portal`, espera su revisión y hace `GET /login`; si no responde `200`, falla el job. |
| 11 | Publica un resumen con las imágenes desplegadas y el comando de rollback. |

Las migraciones se aplican **después** de construir las imágenes y **antes** de
actualizar la API. El script es idempotente y el despliegue se detiene si falla.
No se ejecutan migraciones automáticas desde la API en `Production`.

Cada build publica dos tags: `:<sha-corto>` y `:latest`. El despliegue usa
siempre el tag con SHA; `latest` queda como referencia de la última imagen
construida y no se usa para actualizar las Container Apps.

## Prerequisites

### 1. Recursos existentes

Deben existir:

- Un resource group, por ejemplo `rg-proyectocafe-prod`.
- Azure SQL Database existente y su servidor.
- Un network path hacia Azure SQL (firewall o private endpoint). La plantilla de
  `infra/` no crea la base ni reglas de red de SQL.

La plantilla [`infra/`](../infra/README.md) crea, si no existen, ACR, Log
Analytics, el entorno de Azure Container Apps, Key Vault y las apps `pc-api` y
`pc-portal` con su configuración de ingress, probes, identidades y secretos
referenciados. Si prefieres recursos ya creados, conserva los mismos nombres y
ajusta los parámetros.

Configuración esperada en las apps:

- El Portal con ingress externo y target port `3000`.
- La API con target port `8080`. El navegador llama directamente a la API, así
  que `PROYECTOCAFE_API_URL` debe ser una URL pública alcanzable y coincidir con
  `Cors:AllowedOrigins`; si usas ingress interno, el smoke test necesita un
  runner self-hosted con acceso a la red y `API_BASE_URL` configurada.
- Modo de revisiones con la última revisión recibiendo tráfico (comportamiento
  por defecto de Container Apps). El smoke test espera que la revisión nueva esté
  activa y con `trafficWeight > 0`; si fijas el tráfico a una revisión concreta,
  actualiza también el tráfico en el workflow.

Probes recomendados (ya documentados en [`ENV.md`](ENV.md)):

| App | Target port | Startup / Liveness | Readiness |
| --- | --- | --- | --- |
| `pc-api` | `8080` | HTTP `GET /health` | HTTP `GET /health/ready` |
| `pc-portal` | `3000` | TCP `3000` | TCP `3000` |

### 2. App registration y OIDC federado

Crea una app registration para GitHub Actions y su service principal. El subject
del credential federado debe ser el del **environment** `production`, porque el
job declara `environment: production`.

```powershell
$AppName = "github-proyectocafe-deploy"
$Repo = "<organizacion>/<repositorio>"   # por ejemplo JFrank20th/ProyectoCafe

$AppId = az ad app create --display-name $AppName --query appId -o tsv
az ad sp create --id $AppId | Out-Null
$SpObjectId = az ad sp show --id $AppId --query id -o tsv

@"
{
  "name": "github-environment-production",
  "issuer": "https://token.actions.githubusercontent.com",
  "subject": "repo:$Repo:environment:production",
  "description": "GitHub Actions environment production",
  "audiences": ["api://AzureADTokenExchange"]
}
"@ | Set-Content -Path "$env:TEMP\github-production-fic.json" -Encoding ascii

az ad app federated-credential create `
  --id $AppId `
  --parameters "$env:TEMP\github-production-fic.json"

Remove-Item "$env:TEMP\github-production-fic.json"
```

Guarda `$AppId`, el tenant y la suscripción para las variables del environment.
No guardes contraseñas: OIDC no las necesita.

Si en lugar de un environment usaras una rama, el subject sería
`repo:<org>/<repo>:ref:refs/heads/main`. No mezcles ambos: el job usa el
environment.

### 3. Permisos de base de datos

La cadena de conexión debe usar un login con permisos DDL sobre la base
(`db_owner` o `db_ddladmin`) para aplicar las migraciones. Si usa autenticación
SQL, crea un login dedicado y no reutilices credenciales de administrador. Formato
de referencia, sin valores reales:

```text
Server=tcp:<servidor>.database.windows.net,1433;Database=<base>;
User ID=<usuario>;Password=<password>;Encrypt=True;TrustServerCertificate=False;
```

Si usa Microsoft Entra ID (`Authentication=Active Directory Default`), crea el
usuario contenido para el service principal de despliegue y concédele el rol DDL:

```sql
CREATE USER [<nombre-del-service-principal>] FROM EXTERNAL PROVIDER;
ALTER ROLE db_ddladmin ADD MEMBER [<nombre-del-service-principal>];
```

### 4. Roles del service principal de despliegue

Asigna los roles con el mínimo alcance posible. Sustituye los placeholders.

```powershell
$Rg = "<resource-group>"
$Acr = "<acr-name>"
$Kv = "<key-vault-name>"
$SqlServer = "<sql-server-name>"
$SpObjectId = "<object-id-del-service-principal>"

$RgId = az group show --name $Rg --query id -o tsv
$AcrId = az acr show --name $Acr --resource-group $Rg --query id -o tsv
$KvId = az keyvault show --name $Kv --resource-group $Rg --query id -o tsv
$SqlId = az sql server show --name $SqlServer --resource-group $Rg --query id -o tsv

# Leer apps, ACR, Key Vault y SQL.
az role assignment create --assignee-object-id $SpObjectId --assignee-principal-type ServicePrincipal --role Reader --scope $RgId

# Actualizar pc-api y pc-portal.
az role assignment create --assignee-object-id $SpObjectId --assignee-principal-type ServicePrincipal --role "Container Apps Contributor" --scope $RgId

# az acr build (quick tasks) + push de imágenes.
az role assignment create --assignee-object-id $SpObjectId --assignee-principal-type ServicePrincipal --role AcrPush --scope $AcrId
az role assignment create --assignee-object-id $SpObjectId --assignee-principal-type ServicePrincipal --role "Container Registry Tasks Contributor" --scope $AcrId

# Leer el secreto de conexión de Azure SQL.
az role assignment create --assignee-object-id $SpObjectId --assignee-principal-type ServicePrincipal --role "Key Vault Secrets User" --scope $KvId

# Regla de firewall temporal que agrega y quita azure/sql-action.
az role assignment create --assignee-object-id $SpObjectId --assignee-principal-type ServicePrincipal --role "SQL Security Manager" --scope $SqlId
```

`AcrPush` por sí solo no permite `az acr build`; también hace falta
`Container Registry Tasks Contributor` (o un rol equivalente que incluya
`Microsoft.ContainerRegistry/registries/scheduleRun/action`). Los cambios de RBAC
pueden tardar unos minutos en propagarse.

Si el servidor SQL no admite reglas de firewall temporales (por ejemplo, acceso
privado), ejecuta el workflow en un runner self-hosted dentro de la VNet o
preconfigura el acceso de red. En ese caso define
`SQL_SKIP_FIREWALL_CHECK=true` en el environment tras validar la conectividad.

### 5. Managed identity de las Container Apps

La plantilla de [`infra/`](../infra/README.md) crea cada app con identidad
`SystemAssigned` y asigna `AcrPull` en el ACR a `pc-api` y `pc-portal`, y
`Key Vault Secrets User` solo a `pc-api`. Si las apps se crearon fuera de la
plantilla, replica la configuración:

```powershell
foreach ($App in @("pc-api", "pc-portal")) {
  az containerapp identity assign --name $App --resource-group $Rg --system-assigned | Out-Null
  $PrincipalId = az containerapp identity show --name $App --resource-group $Rg --query principalId -o tsv
  az role assignment create --assignee-object-id $PrincipalId --assignee-principal-type ServicePrincipal --role AcrPull --scope $AcrId
}
```

Limitación importante: con identidad `system-assigned`, el `principalId` no
existe hasta crear la app. Bicep asigna el rol en un recurso separado de la misma
plantilla y el nombre del role assignment se deriva del nombre de la app, no del
`principalId`. El RBAC puede tardar varios minutos en propagarse; la primera
revisión puede fallar al jalar la imagen o leer Key Vault. Vuelve a desplegar o
reinicia la revisión cuando el rol esté activo.

### 6. Secretos de runtime en Key Vault

El workflow solo lee el secreto de conexión para migrar. La API sigue leyendo sus
secretos en runtime mediante referencias de Key Vault. Nombres de ejemplo:

| Secreto de Key Vault | Uso |
| --- | --- |
| `DatabaseConnectionString` | Cadena de Azure SQL (migraciones y `Database__ConnectionString`). |
| `AuthJwtSigningKey` | Clave HS256 de la API (`Auth__Jwt__SigningKey`). |

La plantilla de `infra/` ya crea la Key Vault con RBAC y configura en `pc-api`
las referencias de Key Vault con la identidad del sistema; no crea los secretos.
El usuario o service principal que los carga necesita `Key Vault Secrets Officer`
sobre el vault. Ver el orden de despliegue en [`infra/README.md`](../infra/README.md).

Configura las referencias en la Container App (ejemplo; ajusta los nombres) si
la app se creó fuera de la plantilla:

```powershell
$KvUri = az keyvault show --name $Kv --resource-group $Rg --query properties.vaultUri -o tsv

az containerapp secret set --name pc-api --resource-group $Rg --secrets `
  "db-conn=keyvaultref:${KvUri}secrets/DatabaseConnectionString,identityref:system" `
  "jwt-key=keyvaultref:${KvUri}secrets/AuthJwtSigningKey,identityref:system"

az containerapp update --name pc-api --resource-group $Rg --set-env-vars `
  "Database__ConnectionString=secretref:db-conn" `
  "Auth__Jwt__SigningKey=secretref:jwt-key" `
  "Auth__Cookie__Secure=true" `
  "Auth__Cookie__Domain=<dominio-comun>" `
  "Cors__AllowedOrigins__0=https://<portal-fqdn>"
```

Ajusta `Auth__Cookie__SameSite` según el dominio del Portal y de la API: `Lax`
si comparten sitio, `None` si son sitios distintos. En ambos casos `Secure` debe
ser `true` fuera de Development. Si Portal y API son subdominios del mismo dominio
registrable, configura además `Auth__Cookie__Domain` con ese dominio común; sin
él, el middleware del Portal no recibe `pc_at`. Consulta la tabla de
[`ENV.md`](ENV.md) y la sección de cookies de este documento.

El Portal no necesita secretos, pero sí las URLs de runtime:

```powershell
az containerapp update --name pc-portal --resource-group $Rg --set-env-vars `
  "PROYECTOCAFE_API_URL=https://<api-fqdn>" `
  "PROYECTOCAFE_API_URL_INTERNAL=https://<api-fqdn>" `
  "PROYECTOCAFE_AUTH_COOKIE_NAME=pc_at" `
  "APP_NAME=ProyectoCafe"
```

`PROYECTOCAFE_API_URL` es la URL que recibe el navegador; debe coincidir con el
origen permitido en `Cors:AllowedOrigins`. `PROYECTOCAFE_API_URL_INTERNAL` solo la
usa el servidor del Portal.

No agregues estos valores como variables del workflow ni como secrets de GitHub.

### 7. Variables del environment `production`

En GitHub: **Settings → Environments → production → Environment variables**.
Crea el environment si no existe (puedes añadir reviewers como protección).

| Variable | Obligatoria | Ejemplo | Descripción |
| --- | --- | --- | --- |
| `AZURE_CLIENT_ID` | Sí | `00000000-0000-0000-0000-000000000000` | Client ID de la app registration. |
| `AZURE_TENANT_ID` | Sí | `00000000-0000-0000-0000-000000000000` | Tenant de Microsoft Entra ID. |
| `AZURE_SUBSCRIPTION_ID` | Sí | `00000000-0000-0000-0000-000000000000` | Suscripción destino. |
| `AZURE_RESOURCE_GROUP` | Sí | `rg-proyectocafe-prod` | Resource group de las apps. |
| `ACR_NAME` | Sí | `acrproyectocafe` | Nombre del registro (sin `.azurecr.io`). |
| `KEY_VAULT_NAME` | Sí | `kv-proyectocafe` | Key Vault con la cadena de SQL. |
| `SQL_CONNECTION_SECRET_NAME` | Sí | `DatabaseConnectionString` | Nombre del secreto de conexión. |
| `API_APP_NAME` | Sí | `pc-api` | Container App de la API. |
| `PORTAL_APP_NAME` | Sí | `pc-portal` | Container App del Portal. |
| `API_IMAGE_NAME` | No | `pc-api` | Repositorio de imagen; por defecto `pc-api`. |
| `PORTAL_IMAGE_NAME` | No | `pc-portal` | Repositorio de imagen; por defecto `pc-portal`. |
| `API_CONTAINER_NAME` | No | vacío | Solo si la app tiene varios contenedores. |
| `PORTAL_CONTAINER_NAME` | No | vacío | Solo si la app tiene varios contenedores. |
| `SQL_SKIP_FIREWALL_CHECK` | No | `false` | `true` solo si el runner ya tiene acceso de red a Azure SQL. |
| `API_BASE_URL` | No | `https://api.tudominio.com` | URL pública de la API para el smoke test y el resumen. |
| `PORTAL_BASE_URL` | No | `https://app.tudominio.com` | URL pública del Portal para el smoke test. |
| `COOKIE_DOMAIN` | No | `tudominio.com` | Solo para el smoke test opcional: dominio esperado en `pc_at`/`pc_csrf`. |
| `SMOKE_TEST_ENABLED` | No | `false` | Activa el paso opcional de smoke test con credenciales. |
| `SMOKE_EMAIL` | No | `smoke-admin@tudominio.com` | Usuario dedicado para el smoke test. La contraseña va en el secret `SMOKE_PASSWORD`. |

Si `API_BASE_URL` o `PORTAL_BASE_URL` no se definen, el workflow usa el FQDN del
ingress externo de la app. Con dominios personalizados, defínelas con
`https://` para que el CI valide el mismo origen que usa el navegador. Si el
ingress es interno, el smoke test falla con un mensaje explícito: configura una
URL alcanzable desde el runner o usa un runner self-hosted dentro de la red.

## Dominios personalizados y certificados gestionados

Estos pasos se ejecutan **después** de que `infra/` haya creado las apps y antes
de actualizar CORS y cookies. Usa los mismos nombres en todo el documento:

```powershell
$Rg           = "rg-proyectocafe-prod"
$Environment  = "pc-cae"
$ApiApp       = "pc-api"
$PortalApp    = "pc-portal"
$Kv           = "kv-proyectocafe"
$SqlServer    = "sql-proyectocafe"

$PortalDomain = "app.tudominio.com"
$ApiDomain    = "api.tudominio.com"
$CookieDomain = "tudominio.com"   # dominio registrable común, sin punto inicial
```

### 1. Agregar los hostnames y obtener los valores DNS

El ingress debe ser HTTP externo (la plantilla de `infra/` ya lo configura). El
certificado gestionado exige un CNAME **directo** al FQDN generado de la app; un
CNAME intermedio (proxy, Traffic Manager, Cloudflare naranja) impide la emisión y
la renovación.

```powershell
$Domains = @(
    @{ App = $PortalApp; Host = $PortalDomain },
    @{ App = $ApiApp;    Host = $ApiDomain }
)

$Dns = foreach ($item in $Domains) {
    $existing = az containerapp hostname list -n $item.App -g $Rg `
        --query "[?name=='$($item.Host)'].name" -o tsv

    if ([string]::IsNullOrWhiteSpace($existing)) {
        az containerapp hostname add `
            --name $item.App `
            --resource-group $Rg `
            --hostname $item.Host `
            --output none
    }

    [pscustomobject]@{
        App         = $item.App
        Host        = $item.Host
        CnameTarget = az containerapp show -n $item.App -g $Rg --query "properties.configuration.ingress.fqdn" -o tsv
        TxtName     = "asuid.$($item.Host)"
        TxtValue    = az containerapp show -n $item.App -g $Rg --query "properties.customDomainVerificationId" -o tsv
    }
}

$Dns | Format-Table -AutoSize
```

### 2. Crear los registros DNS

| Tipo | Nombre | Valor | TTL |
| --- | --- | --- | --- |
| `CNAME` | `app` | FQDN de `pc-portal` (`pc-portal.<defaultDomain>`) | 300 |
| `TXT` | `asuid.app` | `customDomainVerificationId` de `pc-portal` | 300 |
| `CNAME` | `api` | FQDN de `pc-api` (`pc-api.<defaultDomain>`) | 300 |
| `TXT` | `asuid.api` | `customDomainVerificationId` de `pc-api` | 300 |

Si el proveedor DNS usa nombres completos, el TXT es
`asuid.app.tudominio.com` y `asuid.api.tudominio.com`. En Cloudflare usa **DNS
only** (nube gris) al menos hasta que el certificado esté `Secured`.

Con Azure DNS (ajusta el resource group de la zona):

```powershell
$DnsRg = "<resource-group-de-la-zona-dns>"
$Zone  = "tudominio.com"

foreach ($row in $Dns) {
    $ShortName = $row.Host.Substring(0, $row.Host.IndexOf('.'))
    az network dns record-set cname set-record `
        -g $DnsRg -z $Zone -n $ShortName -c $row.CnameTarget --ttl 300
    az network dns record-set txt add-record `
        -g $DnsRg -z $Zone -n "asuid.$ShortName" -v $row.TxtValue --ttl 300
}
```

Verifica la propagación antes de enlazar (puede tardar de minutos a una hora):

```powershell
Resolve-DnsName $PortalDomain -Type CNAME
Resolve-DnsName "asuid.$PortalDomain" -Type TXT
Resolve-DnsName $ApiDomain -Type CNAME
Resolve-DnsName "asuid.$ApiDomain" -Type TXT
```

### 3. Enlazar el certificado gestionado

```powershell
foreach ($row in $Dns) {
    az containerapp hostname bind `
        --name $row.App `
        --resource-group $Rg `
        --hostname $row.Host `
        --environment $Environment `
        --validation-method CNAME
}
```

La emisión puede tardar varios minutos. Comprueba el estado:

```powershell
az containerapp hostname list -n $PortalApp -g $Rg -o table
az containerapp hostname list -n $ApiApp -g $Rg -o table
az containerapp env certificate list -n $Environment -g $Rg -o table
```

Debe aparecer el hostname con certificado `Secured` y el certificado gestionado
en estado `Succeeded`. Prueba HTTPS antes de continuar:

```powershell
Invoke-WebRequest "https://$PortalDomain/login" -UseBasicParsing | Select-Object StatusCode
Invoke-WebRequest "https://$ApiDomain/health" -UseBasicParsing | Select-Object StatusCode
```

Los certificados gestionados se renuevan solos mientras el CNAME siga apuntando
directamente a la app y el ingress siga siendo público. El enlace del dominio es
configuración de ingress: sobrevive a las nuevas revisiones.

### 4. Actualizar las URLs de runtime del Portal

Si el Portal se creó con el FQDN por defecto, cámbialo a la API personalizada.
Esto crea una revisión nueva:

```powershell
az containerapp update -n $PortalApp -g $Rg --set-env-vars `
    "PROYECTOCAFE_API_URL=https://$ApiDomain" `
    "PROYECTOCAFE_API_URL_INTERNAL=https://$ApiDomain"
```

`PROYECTOCAFE_API_URL` es la URL que usa el navegador y debe coincidir con el
origen permitido en CORS. `PROYECTOCAFE_API_URL_INTERNAL` la usa solo el servidor
del Portal.

Si más adelante vuelves a aplicar `infra/main.bicep`, pasa
`portalPublicFqdn`, `apiPublicFqdn` y `cookieDomain` con estos mismos valores.
La plantilla declara las variables de entorno como estado deseado; si los omites,
las apps vuelven a los FQDN por defecto y la cookie a host-only.

## CORS exacto y nueva revisión

La API es la única autoridad CORS. La allowlist debe contener **solo** el origen
exacto del Portal, en minúsculas, con esquema y sin barra final.

```powershell
$PortalOrigin = "https://$PortalDomain"

az containerapp update -n $ApiApp -g $Rg --set-env-vars `
    "Cors__AllowedOrigins__0=$PortalOrigin" `
    "Auth__Cookie__SameSite=Lax" `
    "Auth__Cookie__Secure=true" `
    "Auth__Cookie__Domain=$CookieDomain"
```

Revisa si quedaron orígenes antiguos (por ejemplo, el FQDN
`*.azurecontainerapps.io` de una puesta en marcha anterior) y elimínalos:

```powershell
az containerapp show -n $ApiApp -g $Rg `
    --query "properties.template.containers[0].env[?starts_with(name, 'Cors__AllowedOrigins')].name" `
    -o tsv

# Solo si existe ese índice:
az containerapp update -n $ApiApp -g $Rg --remove-env-vars "Cors__AllowedOrigins__1"
```

La configuración CORS se resuelve al arrancar. Cualquier cambio de variables de
entorno crea una revisión; espera a que la nueva esté activa, saludable y con
tráfico antes de validar:

```powershell
az containerapp revision list -n $ApiApp -g $Rg -o table
```

Con `activeRevisionsMode: Single` (valor de la plantilla), la revisión nueva
sustituye a la anterior. Puedes reutilizar el script de espera del CI:

```powershell
$ApiImage = az containerapp show -n $ApiApp -g $Rg `
    --query "properties.template.containers[0].image" -o tsv

bash scripts/aca-verify-revision.sh `
    --app $ApiApp `
    --resource-group $Rg `
    --image $ApiImage `
    --label API `
    --path /health `
    --url "https://$ApiDomain"
```

Verifica el preflight y la respuesta real. El origen debe repetirse exactamente y
`Access-Control-Allow-Credentials` debe ser `true`:

```powershell
$Preflight = Invoke-WebRequest -Method Options `
    -Uri "https://$ApiDomain/api/v1/auth/login" `
    -Headers @{
        Origin = $PortalOrigin
        "Access-Control-Request-Method" = "POST"
        "Access-Control-Request-Headers" = "content-type"
    } `
    -UseBasicParsing

$Preflight.StatusCode
$Preflight.Headers["Access-Control-Allow-Origin"]
$Preflight.Headers["Access-Control-Allow-Credentials"]
$Preflight.Headers["Vary"]
```

Esperado: `204`, `https://app.tudominio.com`, `true` y `Origin` en `Vary`. Si el
preflight falla, no sigas: el navegador bloqueará el login aunque la API responda
por `curl`.

## Cookies: SameSite, Secure y dominio compartido

La decisión depende del dominio registrable, no solo del host:

> El dominio debe ser propio (no un sufijo público como `azurecontainerapps.io`,
> `azurewebsites.net` o `github.io`). En un sufijo público, dos subdominios se
> consideran sitios distintos y `Lax` no envía la cookie entre ellos.

| Escenario | `SameSite` | `Secure` | `Domain` | Resultado |
| --- | --- | --- | --- | --- |
| `app.tudominio.com` + `api.tudominio.com` (mismo dominio registrable) | `Lax` | `true` | `tudominio.com` | **Configuración elegida.** El navegador envía las cookies en las llamadas del Portal a la API y el middleware del Portal puede leer `pc_at`. |
| Hosts idénticos (`localhost`, desarrollo) | `Lax` | `false` en Development | vacío | Cookie host-only; funciona porque el host es el mismo. |
| Dominios registrables distintos | `None` | `true` | vacío | `None` permite el envío cross-site a la API, pero el middleware del Portal no puede leer una cookie de otro dominio: publica la API bajo el mismo origen del Portal (proxy inverso) o usa un dominio común. |

> Sin `Auth:Cookie:Domain`, `pc_at` es host-only en `api.tudominio.com`. El
> middleware del Portal (`app.tudominio.com`) no la recibe y redirige a `/login`
> en bucle aunque el login haya respondido `200`. Este es el error más frecuente
> al pasar de `localhost` a subdominios.

Configuración aplicada en la sección de CORS:

```text
Auth__Cookie__SameSite=Lax
Auth__Cookie__Secure=true
Auth__Cookie__Domain=tudominio.com
```

### Verificación en el navegador (obligatoria)

1. Abre `https://app.tudominio.com/login` y entra con un usuario real.
2. DevTools → **Application → Storage → Cookies → `https://api.tudominio.com`**:
   - `pc_at`: `HttpOnly` ✓, `Secure` ✓, `SameSite=Lax`, `Domain=.tudominio.com`
     (Chrome puede mostrar el punto inicial), `Path=/`.
   - `pc_csrf`: `HttpOnly` **desmarcado** (el Portal lo lee por JavaScript),
     `Secure` ✓, `SameSite=Lax`, mismo `Domain`.
3. DevTools → **Network → `login` → Response Headers**: aparecen dos
   `Set-Cookie`; el cuerpo JSON solo contiene `user` (nunca el JWT).
4. **Application → Cookies → `https://app.tudominio.com`**: también aparecen
   `pc_at` y `pc_csrf`; esto demuestra que `Domain` comparte la cookie con el
   Portal.
5. Navega a `/dashboard` y recarga con `F5`: debe seguir autenticado, sin
   redirección a `/login`.
6. Network → `me`: `200` con el usuario. Network → una petición `POST` del Portal
   (por ejemplo, refrescar sesión): incluye `X-CSRF` y responde `200`; sin la
   cabecera, la API responde `403`.
7. Si el navegador bloquea cookies de terceros (Safari ITP, modo estricto), usa
   el escenario de mismo dominio registrable; `SameSite=None` no garantiza que
   una cookie de otro sitio sea aceptada.

## Key Vault: secretos, permisos y rotación

Nombres usados por la plantilla:

| Secreto | Variable de la API | Uso |
| --- | --- | --- |
| `DatabaseConnectionString` | `Database__ConnectionString` | Cadena de Azure SQL. |
| `AuthJwtSigningKey` | `Auth__Jwt__SigningKey` | Clave HS256 de al menos 32 bytes. |

La API los lee con **referencias de Key Vault** y su identidad administrada; no
se copian a variables de entorno en texto plano ni al repositorio.

### Permisos

- La identidad de `pc-api` necesita `Key Vault Secrets User` sobre el vault. La
  plantilla de `infra/` ya lo asigna. Verifica:

```powershell
$ApiPrincipal = az containerapp identity show -n $ApiApp -g $Rg --query principalId -o tsv
$KvId = az keyvault show -n $Kv -g $Rg --query id -o tsv
az role assignment list --assignee $ApiPrincipal --scope $KvId -o table
```

- Quien carga o rota secretos necesita `Key Vault Secrets Officer` sobre el
  vault (el rol de plano de control no basta).
- El service principal de despliegue de GitHub solo necesita
  `Key Vault Secrets User` para leer la cadena durante la migración.
- RBAC puede tardar 5–10 minutos. Si la primera revisión falla con `403`, espera
  y crea una revisión nueva; no cambies la identidad ni el scope.

Comprueba que la app referencia los secretos correctos sin imprimir valores:

```powershell
az containerapp secret list -n $ApiApp -g $Rg -o table
az containerapp show -n $ApiApp -g $Rg `
    --query "properties.template.containers[0].env[?secretRef != null].[name, secretRef]" `
    -o table
```

### Rotación de la clave JWT

1. Genera una clave nueva y créala como **nueva versión** del secreto (no borres
   la anterior todavía):

```powershell
$JwtKey = [Convert]::ToBase64String(
    [System.Security.Cryptography.RandomNumberGenerator]::GetBytes(48))
az keyvault secret set --vault-name $Kv --name AuthJwtSigningKey --value $JwtKey
Remove-Variable JwtKey
```

2. Fuerza una revisión nueva para que la API resuelva la versión más reciente.
   Las referencias sin versión también se refrescan solas en un plazo de hasta 30
   minutos, pero la revisión nueva es determinista:

```powershell
$Current = az containerapp revision list -n $ApiApp -g $Rg `
    --query "[?properties.active].name | [0]" -o tsv
$Suffix  = "jwt$(Get-Date -Format yyyyMMddHHmm)"

az containerapp revision copy -n $ApiApp -g $Rg `
    --from-revision $Current `
    --revision-suffix $Suffix `
    --set-env-vars "AuthRotation=$Suffix"
```

3. Espera la revisión saludable y ejecuta el smoke test. Al cambiar la clave de
   firma, **todas las sesiones existentes dejan de ser válidas**; avisa a los
   usuarios. Si algo falla, vuelve a la versión anterior del secreto y crea otra
   revisión.
4. Cuando la nueva revisión esté validada, deshabilita la versión anterior si tu
   política lo exige (no la elimines de inmediato):

```powershell
az keyvault secret set-attributes --vault-name $Kv `
    --name AuthJwtSigningKey --version "<version-anterior>" --enabled false
```

### Rotación de la cadena de Azure SQL

1. Crea primero la credencial nueva en SQL (login/usuario o contraseña nueva) sin
   borrar la anterior.
2. Actualiza `DatabaseConnectionString` en Key Vault (nueva versión). No la
   escribas en el historial de la shell:

```powershell
$Secure = Read-Host "Nueva cadena de conexión de Azure SQL" -AsSecureString
$Conn   = [System.Net.NetworkCredential]::new('', $Secure).Password
az keyvault secret set --vault-name $Kv --name DatabaseConnectionString --value $Conn
Remove-Variable Secure, Conn
```

3. Crea una revisión nueva (mismo procedimiento que con JWT), espera
   `GET /health/ready` = `200` y ejecuta el smoke test.
4. Solo entonces elimina la credencial anterior en SQL.

No rotes la contraseña de SQL antes de que la revisión nueva esté lista si la
revisión anterior sigue recibiendo tráfico: sus conexiones agrupadas fallarían.

## Conectividad con Azure SQL

La plantilla de `infra/` **no** crea la base, la red ni reglas de firewall. Elige
una opción y déjala documentada en el runbook del entorno.

### Opción recomendada: private endpoint + VNet

Ventajas: el tráfico de la API a SQL no sale a Internet y se puede deshabilitar
el acceso público del servidor. Requisitos: el Container Apps Environment debe
estar integrado en una VNet **desde su creación**; no se puede convertir un
environment público existente en uno privado sin recrearlo.

1. Crea la VNet y las subredes. La subred del environment necesita delegación a
   `Microsoft.App/environments` y tamaño suficiente (`/23` es una base segura):

```powershell
$Vnet       = "vnet-proyectocafe"
$AcaSubnet  = "snet-aca"
$PeSubnet   = "snet-private-endpoints"

az network vnet create -g $Rg -n $Vnet --address-prefixes 10.40.0.0/16
az network vnet subnet create -g $Rg --vnet-name $Vnet -n $AcaSubnet `
    --address-prefixes 10.40.0.0/23 `
    --delegations Microsoft.App/environments
az network vnet subnet create -g $Rg --vnet-name $Vnet -n $PeSubnet `
    --address-prefixes 10.40.2.0/24
```

2. Crea el environment con esa subred. Si ya existe un environment público, usa un
   nombre nuevo (por ejemplo, `$Environment = "pc-cae-vnet"`) y migra las apps y
   el DNS después de validar; el nombre del environment no se puede reutilizar
   mientras el anterior exista y la integración de VNet no se puede cambiar in
   situ.

```powershell
$AcaSubnetId = az network vnet subnet show -g $Rg --vnet-name $Vnet -n $AcaSubnet --query id -o tsv

az containerapp env create -g $Rg -n $Environment `
    --location "<region>" `
    --infrastructure-subnet-resource-id $AcaSubnetId
```

3. Crea el private endpoint de SQL y la zona DNS privada:

```powershell
$SqlId = az sql server show -n $SqlServer -g $Rg --query id -o tsv

az network private-dns zone create -g $Rg -n privatelink.database.windows.net
az network private-dns link vnet create -g $Rg -n link-sql `
    -z privatelink.database.windows.net -v $Vnet --registration-enabled false

az network private-endpoint create -g $Rg -n pe-sql `
    --vnet-name $Vnet --subnet $PeSubnet `
    --private-connection-resource-id $SqlId `
    --group-id sqlServer `
    --connection-name sql-proyectocafe

az network private-endpoint dns-zone-group create -g $Rg `
    --endpoint-name pe-sql -n sql-dns `
    --private-dns-zone privatelink.database.windows.net `
    --zone-name privatelink.database.windows.net
```

4. Deshabilita el acceso público del servidor SQL (después de validar el private
   endpoint):

```powershell
az sql server update -n $SqlServer -g $Rg --set publicNetworkAccess=Disabled
```

5. Verifica desde la API: `GET /health/ready` debe responder `200`. El health
   check abre una conexión real a SQL; no expone la cadena.

6. **CI:** un runner alojado por GitHub no puede entrar a una base privada. Opciones:
   - runner self-hosted dentro de la VNet y `SQL_SKIP_FIREWALL_CHECK=true`;
   - ejecutar `tools/sqlserver-migrate-current.sql` en un Container Apps Job o
     contenedor dentro de la VNet;
   - mantener acceso público temporal solo durante la migración (no recomendado
     como estado final).

   Con SQL privado, el job actual que usa `azure/sql-action` desde un runner
   público fallará aunque las apps funcionen. Ajusta el entorno antes de
   desplegar.

### Alternativa: Allow Azure services (riesgo documentado)

```powershell
az sql server firewall-rule create -g $Rg -s $SqlServer `
    -n AllowAzureServices `
    --start-ip-address 0.0.0.0 --end-ip-address 0.0.0.0
```

La regla `0.0.0.0` es la marca especial de Azure, no una IP de Internet. Aun así,
permite intentos de conexión desde **cualquier** recurso de Azure, no solo desde
tu suscripción o tu environment. Mitiga con autenticación fuerte (idealmente
Microsoft Entra ID), contraseñas rotadas, auditoría, Microsoft Defender for SQL y
alertas. No es equivalente a un private endpoint.

**Nunca** agregues a la allowlist las IP de salida de Container Apps: no son
estables ni exclusivas de tu app y cambian sin aviso. Si necesitas egreso con IP
fija, usa un environment integrado en VNet con NAT Gateway.

## Smoke test automatizado

[`scripts/smoke-azure.ps1`](../scripts/smoke-azure.ps1) valida el flujo completo
contra las URLs públicas. No necesita Azure CLI; solo PowerShell 5.1+ y HTTPS.

```powershell
$env:PROYECTOCAFE_SMOKE_EMAIL    = "smoke-admin@tudominio.com"
$env:PROYECTOCAFE_SMOKE_PASSWORD = "<contraseña-del-usuario-de-smoke>"

pwsh -File .\scripts\smoke-azure.ps1 `
    -ApiBaseUrl "https://api.tudominio.com" `
    -PortalBaseUrl "https://app.tudominio.com" `
    -ExpectedCookieSameSite Lax `
    -ExpectedCookieDomain "tudominio.com"
```

El usuario debe ser un usuario real con permisos de lectura para administración,
ventas e inventario. No reutilices el administrador personal: crea un usuario de
smoke con contraseña fuerte y, si es posible, roles acotados. El script no imprime
la contraseña ni los valores de las cookies.

Comprueba, en orden:

1. `GET /health` de la API.
2. Preflight CORS del origen del Portal y credenciales.
3. `POST /api/v1/auth/login`; cookies `pc_at` y `pc_csrf` con `Secure`,
   `SameSite` y `Domain` esperados.
4. `GET /api/v1/auth/me` con la cookie.
5. CSRF: `POST /api/v1/auth/refresh` sin `X-CSRF` → `403`; con `X-CSRF` → `200`.
6. Administración: `GET /api/v1/admin/users` y la pantalla
   `/administracion/usuarios` del Portal.
7. Ventas: `GET /api/v1/ventas/ventas`, `/api/v1/ventas/precios` y la pantalla
   `/dashboard/ventas`.
8. Inventario: `GET /api/v1/inventario/lotes`, `/tipos-cafe` y la pantalla
   `/inventario/CafeTostado`.
9. Portal: `/login`, `/dashboard` y las pantallas protegidas sin redirección a
   `/login`/`acceso-denegado`.

Códigos de salida: `0` correcto, `2` argumentos o dependencias, `3` health,
`4` CORS, `5` login, `6` cookies, `7` sesión (`/me`), `8` CSRF, `9`
administración, `10` ventas, `11` inventario, `12` Portal y `13` error
inesperado. Cualquier valor distinto de `0` debe detener la promoción.

Para ejecutarlo desde GitHub Actions, guarda la contraseña como secret del
environment `production` (nunca en el repositorio) y añade un paso opcional tras
el despliegue:

```yaml
- name: Smoke test Azure
  if: ${{ vars.SMOKE_TEST_ENABLED == 'true' }}
  shell: pwsh
  env:
    PROYECTOCAFE_SMOKE_EMAIL: ${{ vars.SMOKE_EMAIL }}
    PROYECTOCAFE_SMOKE_PASSWORD: ${{ secrets.SMOKE_PASSWORD }}
  run: |
    ./scripts/smoke-azure.ps1 `
      -ApiBaseUrl "${{ vars.API_BASE_URL }}" `
      -PortalBaseUrl "${{ vars.PORTAL_BASE_URL }}" `
      -ExpectedCookieSameSite Lax `
      -ExpectedCookieDomain "${{ vars.COOKIE_DOMAIN }}"
```



## Probar con `workflow_dispatch`

1. Configura el environment `production`, sus variables y la app registration con
   el credential federado.
2. Verifica que el service principal tenga los roles de la sección 4 y que las
   Container Apps tengan `AcrPull`.
3. Enlaza los dominios personalizados y actualiza CORS/cookies según las secciones
   anteriores. Confirma `https://app.tudominio.com/login` y
   `https://api.tudominio.com/health` con `200`.
4. En GitHub, abre **Actions → CI → Run workflow**.
5. Selecciona la rama `main` y marca `deploy` (valor `true`).
6. Sin `deploy` marcado, el workflow ejecuta solo las validaciones de CI.
7. Sin credenciales u OIDC configurados, el job `deploy` fallará en
   `Validate deployment configuration` o en `Azure login (OIDC)`. Eso es lo
   esperado y no modifica Azure.
8. Tras el despliegue, ejecuta `scripts/smoke-azure.ps1` contra los dominios
   personalizados. Si devuelve un código distinto de `0`, no promociones y aplica
   el rollback.

El `push` a `main` es el único disparador automático. El workflow no despliega
desde `master`, ramas de feature ni `pull_request`.

Validación local útil antes de subir cambios:

```powershell
bash -n scripts/aca-verify-revision.sh
pwsh -NoProfile -Command "`$null = [ScriptBlock]::Create((Get-Content -Raw .\scripts\smoke-azure.ps1))"
```

La sintaxis del workflow se comprueba en el propio CI de GitHub. Si tienes
`actionlint` instalado:

```powershell
actionlint .github/workflows/ci.yml
```

## Rollback de revisiones y configuración

### 1. Guarda el estado anterior antes de desplegar

```powershell
$ApiImagePrevious = az containerapp show -n $ApiApp -g $Rg `
    --query "properties.template.containers[0].image" -o tsv
$PortalImagePrevious = az containerapp show -n $PortalApp -g $Rg `
    --query "properties.template.containers[0].image" -o tsv
$ApiRevisionPrevious = az containerapp revision list -n $ApiApp -g $Rg `
    --query "[?properties.active].name | [0]" -o tsv

Write-Host "API:    $ApiImagePrevious ($ApiRevisionPrevious)"
Write-Host "Portal: $PortalImagePrevious"
```

El resumen del job de CI también imprime las imágenes anteriores. Guárdalas en el
registro del despliegue.

### 2. Volver a la imagen anterior

La plantilla usa `activeRevisionsMode: Single`; al actualizar la imagen se crea
una revisión nueva que sustituye a la anterior:

```powershell
az containerapp update -n $ApiApp -g $Rg --image $ApiImagePrevious
az containerapp update -n $PortalApp -g $Rg --image $PortalImagePrevious
```

Espera a que la revisión esté `Provisioned`/`Running`/`Healthy` y repite el smoke
test. No hace falta tocar DNS ni certificados: el dominio personalizado es
configuración de ingress y sigue enlazado.

Si trabajas con `activeRevisionsMode: Multiple`, mueve el tráfico a la revisión
anterior en lugar de crear otra imagen:

```powershell
az containerapp revision activate -n $ApiApp -g $Rg --revision $ApiRevisionPrevious
az containerapp ingress traffic set -n $ApiApp -g $Rg `
    --revision-weight "$ApiRevisionPrevious=100"
```

### 3. Revertir CORS, cookies o URLs

Restaura las variables anteriores y crea una revisión nueva. Ejemplo para volver
al FQDN por defecto:

```powershell
az containerapp update -n $ApiApp -g $Rg --set-env-vars `
    "Cors__AllowedOrigins__0=https://<portal-fqdn-anterior>" `
    "Auth__Cookie__SameSite=Lax" `
    "Auth__Cookie__Secure=true" `
    "Auth__Cookie__Domain="

az containerapp update -n $PortalApp -g $Rg --set-env-vars `
    "PROYECTOCAFE_API_URL=https://<api-fqdn-anterior>" `
    "PROYECTOCAFE_API_URL_INTERNAL=https://<api-fqdn-anterior>"
```

Ojo: vaciar `Auth__Cookie__Domain` hace que `pc_at` vuelva a ser host-only; el
Portal en un subdominio distinto dejará de ver la sesión. Es un rollback válido
solo si también vuelves a un esquema de mismo host.

### 4. Quitar un dominio personalizado

El certificado gestionado puede quedar en el environment aunque el hostname ya no
esté enlazado. No borres un certificado en uso por otra app:

```powershell
az containerapp hostname delete -n $ApiApp -g $Rg --hostname $ApiDomain --yes
az containerapp hostname list -n $ApiApp -g $Rg -o table
```

### 5. Revertir secretos

No borres versiones antiguas hasta validar la nueva. Para volver atrás:

1. Reactiva o crea una versión con el valor anterior en Key Vault.
2. Crea una revisión nueva para que la API la resuelva.
3. Verifica `/health/ready` y el smoke test.

```powershell
az keyvault secret set-attributes --vault-name $Kv `
    --name AuthJwtSigningKey --version "<version-anterior>" --enabled true
```

### 6. Infraestructura y base de datos

Para revertir ACR, Key Vault, environment, apps y RBAC, aplica la versión anterior
de `infra/` en modo incremental. No uses `--mode Complete` sobre recursos con
datos. Procedimiento detallado: [`infra/README.md`](../infra/README.md#rollback).

Las migraciones son forward-only. No hagas rollback de la imagen si el esquema
nuevo es incompatible con la versión anterior: aplica una migración correctiva o
restaura la base según el procedimiento de Azure SQL.


## Troubleshooting

| Síntoma | Causa probable | Solución |
| --- | --- | --- |
| `AADSTS70021` / no matching federated identity | El subject OIDC no coincide con el environment. | Crea el credential con `repo:<org>/<repo>:environment:production`. |
| `403` en `az acr build` | Falta `Container Registry Tasks Contributor`. | Asígnalo en el ACR; `AcrPush` solo no alcanza. |
| `403` al leer Key Vault | Falta `Key Vault Secrets User` o RBAC aún no propagó. | Asígnalo en el Key Vault; espera 5–10 min y crea una revisión nueva. |
| `Client with IP address ... is not allowed` | El runner no puede entrar a Azure SQL. | Concede `SQL Security Manager` para la regla temporal, usa un runner en la VNet o un job dentro de ella. |
| La revisión queda `Failed` o `Unhealthy` | Imagen, variables, secretos, puerto o startup. | Revisa `az containerapp logs show`; el script imprime las últimas 100 líneas al fallar. |
| DNS no valida o el certificado queda `Pending`/`Failed` | Falta el TXT `asuid.<subdominio>`, el CNAME no es directo (proxy/Cloudflare naranja) o no propagó. | Verifica con `Resolve-DnsName`; usa CNAME directo al FQDN de la app y TXT con el `customDomainVerificationId` correcto. |
| El hostname aparece `Secured` pero HTTPS da `404`/`502` | El dominio se enlazó a la app equivocada o el target port no es el correcto. | Revisa `az containerapp hostname list` y el ingress; `pc-portal` → `3000`, `pc-api` → `8080`. |
| `No 'Access-Control-Allow-Origin' header` en el navegador | El origen no coincide exactamente (http/https, puerto, barra final) o la revisión con la nueva allowlist no está activa. | Compara `Cors__AllowedOrigins__0` con `window.location.origin`; espera la revisión y prueba el preflight. |
| Preflight `204` sin `Access-Control-Allow-Origin` | Origen no permitido o petición sin `Origin`. | Usa el origen exacto del Portal y `-Headers @{ Origin = ... }` en la prueba. |
| Login responde `200` pero el Portal vuelve a `/login` | `pc_at` es host-only y el Portal está en otro subdominio. | Configura `Auth__Cookie__Domain=tudominio.com` y verifica `Domain` en DevTools; crea una revisión nueva. |
| `pc_at` no aparece en las cookies del host del Portal | `Domain` ausente/incorrecto, `Secure` sobre HTTP o cookie de terceros bloqueada. | Usa HTTPS, `Domain` del dominio registrable común y el escenario de mismo sitio; evita `SameSite=None` entre sitios distintos. |
| `pc_csrf` no es legible por JavaScript | La cookie quedó `HttpOnly` o el navegador la bloqueó. | La API la emite sin `HttpOnly`; revisa que no haya un proxy que reescriba `Set-Cookie`. |
| `403 CSRF` en una operación del Portal | Falta `X-CSRF`, la cookie `pc_csrf` no está o no coincide. | Comprueba `document.cookie`, que el Portal envíe `X-CSRF` y que el token se haya refrescado tras `refresh`. |
| `401` en `/auth/me` justo después de rotar el JWT | La clave de firma cambió y las cookies antiguas ya no son válidas. | Es esperado: vuelve a iniciar sesión. Si no debe invalidarse, no rotes la clave sin ventana de mantenimiento. |
| `health/ready` responde `503` | SQL inaccesible: private DNS, firewall, credenciales o cadena obsoleta. | Prueba desde la app/VNet; revisa `az containerapp logs show` y la versión del secreto. |
| El CI falla al migrar con SQL privado | El runner alojado no tiene ruta a la VNet. | Usa runner self-hosted en la VNet con `SQL_SKIP_FIREWALL_CHECK=true` o un job dentro de la VNet. |
| Smoke test `000` | URL inalcanzable, DNS sin propagar o ingress interno. | Define `API_BASE_URL`/`PORTAL_BASE_URL`, espera el DNS o usa un runner con acceso. |
| Smoke test `404` | Ruta, target port o ingress incorrectos. | Verifica `/health` en `8080` y `/login` en `3000`. |
| Smoke test falla en cookies | `Domain`, `SameSite` o `Secure` no son los esperados. | Ejecuta con `-ExpectedCookieSameSite` y `-ExpectedCookieDomain`; revisa DevTools y la revisión activa. |
| El Portal carga pero las pantallas protegidas redirigen | El servidor del Portal no ve la cookie o la URL interna de la API es incorrecta. | Revisa `PROYECTOCAFE_API_URL_INTERNAL`, `Domain` de la cookie y `/api/v1/auth/me`. |

## Checklist de verificación

### Dominios y certificados

- [ ] `app.tudominio.com` y `api.tudominio.com` agregados con
      `az containerapp hostname add`.
- [ ] CNAME directo al FQDN de cada app (sin proxy ni CNAME intermedio).
- [ ] TXT `asuid.app`/`asuid.api` con el `customDomainVerificationId` correcto.
- [ ] Certificados gestionados en estado `Secured`/`Succeeded`.
- [ ] `https://app.tudominio.com/login` y `https://api.tudominio.com/health`
      responden `200`.
- [ ] `API_BASE_URL` y `PORTAL_BASE_URL` del environment `production` apuntan a
      los dominios personalizados.

### CORS y cookies

- [ ] `Cors__AllowedOrigins__0=https://app.tudominio.com` (exacto, sin barra
      final, sin comodines).
- [ ] No quedan orígenes antiguos en `Cors__AllowedOrigins__1...`.
- [ ] Preflight `OPTIONS` devuelve `204` con el origen exacto y
      `Access-Control-Allow-Credentials: true`.
- [ ] `Auth__Cookie__SameSite=Lax`, `Auth__Cookie__Secure=true` y
      `Auth__Cookie__Domain=tudominio.com`.
- [ ] `pc_at` es `HttpOnly`, `Secure`, `SameSite=Lax` y visible en el host del
      Portal; `pc_csrf` es legible por JavaScript.
- [ ] El cuerpo del login no contiene el JWT.
- [ ] `/dashboard` sobrevive a una recarga sin volver a `/login`.

### Key Vault y SQL

- [ ] `DatabaseConnectionString` y `AuthJwtSigningKey` existen en Key Vault.
- [ ] La identidad de `pc-api` tiene `Key Vault Secrets User` sobre el vault.
- [ ] El service principal de despliegue puede leer la cadena para migrar.
- [ ] Las referencias `keyvaultref` de `pc-api` usan `identityref:system`.
- [ ] Rotación de JWT y de cadena probada (o programada) sin exponer valores.
- [ ] Conectividad SQL resuelta: private endpoint + VNet (recomendado) o
      `Allow Azure services` con el riesgo aceptado y documentado.
- [ ] Nunca se agregaron IP de salida de ACA a la allowlist.
- [ ] `/health/ready` responde `200`.

### Despliegue y operación

- [ ] App registration con credential federado para `environment:production`.
- [ ] Environment `production` con las variables obligatorias.
- [ ] Roles del service principal: Reader, Container Apps Contributor, AcrPush,
      Container Registry Tasks Contributor, Key Vault Secrets User, SQL Security
      Manager.
- [ ] Managed identity de `pc-api` y `pc-portal` con `AcrPull`; `pc-api` además
      con `Key Vault Secrets User`.
- [ ] Probes y target ports correctos.
- [ ] `infra/` validado con `az bicep build` y `what-if`; `cookieDomain`
      configurado para los subdominios.
- [ ] Imagen y revisión anteriores guardadas antes de desplegar.
- [ ] `scripts/smoke-azure.ps1` devuelve `0` en API y Portal.
- [ ] `CI required` marcado como required status check en la rama principal.
- [ ] `workflow_dispatch` probado primero sin `deploy` y luego con `deploy=true`
      en un entorno controlado.
- [ ] Rollback de imagen, configuración y secretos probado o ensayado.
