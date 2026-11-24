# Despliegue en Azure Container Apps

El job `deploy` de `.github/workflows/ci.yml` publica `pc-api` y `pc-portal` en
Azure Container Apps. Se ejecuta **solo** en un `push` a `main` (o manualmente
desde `main` con `deploy=true`) y siempre después de que pase el job obligatorio
`CI required`.

No hay secretos en el repositorio ni en el workflow. La autenticación con Azure
usa OIDC federado y la cadena de conexión de Azure SQL se lee de Key Vault en
tiempo de ejecución.

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

Se asume que ya existen:

- Un resource group, por ejemplo `rg-proyectocafe-prod`.
- Azure Container Registry, por ejemplo `acrproyectocafe`.
- Azure Key Vault, por ejemplo `kv-proyectocafe`.
- Azure SQL Database existente y su servidor.
- Un entorno de Azure Container Apps.
- Las apps `pc-api` y `pc-portal` creadas en ese entorno.
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

Cada app debe poder jalar su imagen de ACR. Habilita identidad asignada por el
sistema y asigna `AcrPull` en el ACR:

```powershell
foreach ($App in @("pc-api", "pc-portal")) {
  az containerapp identity assign --name $App --resource-group $Rg --system-assigned | Out-Null
  $PrincipalId = az containerapp identity show --name $App --resource-group $Rg --query principalId -o tsv
  az role assignment create --assignee-object-id $PrincipalId --assignee-principal-type ServicePrincipal --role AcrPull --scope $AcrId
}
```

La misma managed identity debe tener `Key Vault Secrets User` si las apps leen
secretos por referencia de Key Vault.

### 6. Secretos de runtime en Key Vault

El workflow solo lee el secreto de conexión para migrar. La API sigue leyendo sus
secretos en runtime mediante referencias de Key Vault. Nombres de ejemplo:

| Secreto de Key Vault | Uso |
| --- | --- |
| `DatabaseConnectionString` | Cadena de Azure SQL (migraciones y `Database__ConnectionString`). |
| `AuthJwtSigningKey` | Clave HS256 de la API (`Auth__Jwt__SigningKey`). |

Configura las referencias en la Container App (ejemplo; ajusta los nombres):

```powershell
$KvUri = az keyvault show --name $Kv --resource-group $Rg --query properties.vaultUri -o tsv

az containerapp secret set --name pc-api --resource-group $Rg --secrets `
  "db-conn=keyvaultref:${KvUri}secrets/DatabaseConnectionString,identityref:system" `
  "jwt-key=keyvaultref:${KvUri}secrets/AuthJwtSigningKey,identityref:system"

az containerapp update --name pc-api --resource-group $Rg --set-env-vars `
  "Database__ConnectionString=secretref:db-conn" `
  "Auth__Jwt__SigningKey=secretref:jwt-key" `
  "Auth__Cookie__Secure=true" `
  "Cors__AllowedOrigins__0=https://<portal-fqdn>"
```

Ajusta `Auth__Cookie__SameSite` según el dominio del Portal y de la API: `Lax`
si comparten sitio, `None` si son sitios distintos. En ambos casos `Secure` debe
ser `true` fuera de Development. Consulta la tabla de [`ENV.md`](ENV.md).

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
| `API_BASE_URL` | No | `https://api.example.com` | Base para el smoke test de la API. |
| `PORTAL_BASE_URL` | No | `https://portal.example.com` | Base para el smoke test del Portal. |

Si `API_BASE_URL` o `PORTAL_BASE_URL` no se definen, el workflow usa el FQDN del
ingress externo de la app. Si el ingress es interno, el smoke test falla con un
mensaje explícito: configura una URL alcanzable desde el runner o usa un runner
self-hosted dentro de la red.

## Probar con `workflow_dispatch`

1. Configura el environment `production`, sus variables y la app registration con
   el credential federado.
2. Verifica que el service principal tenga los roles de la sección 4 y que las
   Container Apps tengan `AcrPull`.
3. En GitHub, abre **Actions → CI → Run workflow**.
4. Selecciona la rama `main` y marca `deploy` (valor `true`).
5. Sin `deploy` marcado, el workflow ejecuta solo las validaciones de CI.
6. Sin credenciales u OIDC configurados, el job `deploy` fallará en
   `Validate deployment configuration` o en `Azure login (OIDC)`. Eso es lo
   esperado y no modifica Azure.

El `push` a `main` es el único disparador automático. El workflow no despliega
desde `master`, ramas de feature ni `pull_request`.

Validación local útil antes de subir cambios:

```powershell
bash -n scripts/aca-verify-revision.sh
```

La sintaxis del workflow se comprueba en el propio CI de GitHub. Si tienes
`actionlint` instalado:

```powershell
actionlint .github/workflows/ci.yml
```

## Rollback

El resumen del job imprime las imágenes anteriores. Para volver a la versión
previa, actualiza la imagen y espera la revisión:

```powershell
az containerapp update `
  --name pc-api `
  --resource-group "<resource-group>" `
  --image "<loginServer>/pc-api:<sha-anterior>"

az containerapp update `
  --name pc-portal `
  --resource-group "<resource-group>" `
  --image "<loginServer>/pc-portal:<sha-anterior>"
```

Las migraciones son forward-only. No hagas rollback de la imagen si el esquema
nuevo es incompatible con la versión anterior: aplica una migración correctiva o
restaura la base según el procedimiento de Azure SQL.

## Troubleshooting

| Síntoma | Causa probable | Solución |
| --- | --- | --- |
| `AADSTS70021` / no matching federated identity | El subject OIDC no coincide con el environment. | Crea el credential con `repo:<org>/<repo>:environment:production`. |
| `403` en `az acr build` | Falta `Container Registry Tasks Contributor`. | Asígnalo en el ACR; `AcrPush` solo no alcanza. |
| `403` al leer Key Vault | Falta `Key Vault Secrets User`. | Asígnalo en el Key Vault al service principal. |
| `Client with IP address ... is not allowed` | El runner no puede entrar a Azure SQL. | Concede `SQL Security Manager` para la regla temporal o usa un runner en la VNet. |
| La revisión queda `Failed` o `Unhealthy` | Imagen, variables, secretos, puerto o startup. | Revisa `az containerapp logs show`; el script imprime las últimas 100 líneas al fallar. |
| Smoke test `000` | URL inalcanzable o ingress interno. | Define `API_BASE_URL`/`PORTAL_BASE_URL` o usa un runner con acceso. |
| Smoke test `404` | Ruta, target port o ingress incorrectos. | Verifica `/health` en `8080` y `/login` en `3000`. |
| El Portal carga pero el login falla | CORS, cookie o URL de API. | `Cors:AllowedOrigins` debe ser el origen exacto del Portal; cookie `Secure` en HTTPS. |

## Checklist

- [ ] App registration creada y credential federado para `environment:production`.
- [ ] Environment `production` con las variables obligatorias.
- [ ] Roles del service principal: Reader, Container Apps Contributor, AcrPush,
      Container Registry Tasks Contributor, Key Vault Secrets User, SQL Security
      Manager.
- [ ] Managed identity de `pc-api` y `pc-portal` con `AcrPull`.
- [ ] Secreto `DatabaseConnectionString` en Key Vault y referencias de runtime
      configuradas en `pc-api`.
- [ ] Probes y target ports correctos.
- [ ] `CI required` marcado como required status check en la rama principal.
- [ ] `workflow_dispatch` probado primero sin `deploy` y luego con `deploy=true`
      en un entorno controlado.
