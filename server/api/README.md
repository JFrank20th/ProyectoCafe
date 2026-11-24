# ProyectoCafe Backend (server/api)

Estructura por capas inspirada en Next-RPS. Cada capa solo depende de las capas
interiores y la persistencia queda aislada en Infrastructure.

Requiere .NET SDK 10; todos los proyectos apuntan a `net10.0`.

- `ProyectoCafe.Domain` — Entidades y estados del dominio. No depende de ningún
  otro proyecto.
- `ProyectoCafe.Application` — DTOs, casos de uso y puertos de salida. Contiene
  `AuthenticationService` y `AdministrationService`, además de las interfaces
  `IUsuarioRepository`, `IRolRepository`, `IUnitOfWork`, `IPermissionCatalog`,
  `ILoginRateLimiter` e `ISecurityBootstrapStore`. No referencia EF Core ni
  ASP.NET Core.
- `ProyectoCafe.Infrastructure` — Implementación de persistencia: `DbContext`,
  configuraciones de EF Core, migraciones y repositorios. Depende de Application
  y Domain.
- `ProyectoCafe.API` — Minimal APIs, composición de dependencias, autenticación
  JWT por cookie, CSRF, CORS y el catálogo de permisos construido a partir de
  los endpoints. Depende de Application (casos de uso) e Infrastructure (solo
  para registrar implementaciones).

## Reglas de dependencias

```text
API ──────────────► Application ──────────────► Domain
 │                        ▲
 └──────────────► Infrastructure ─────────────┘
```

- Los endpoints **no** acceden a `ProyectoCafeDbContext`; invocan servicios de
  Application y traducen sus resultados a respuestas HTTP.
- La lógica de negocio (validaciones, reglas de login, asignación de roles y
  permisos) vive en `ProyectoCafe.Application/Services`.
- Los DTOs de entrada y salida viven en `ProyectoCafe.Application/DTOs`.
- Las consultas y escrituras EF Core viven en
  `ProyectoCafe.Infrastructure/Repositories`.
- `DbContext`, migraciones y configuración de tablas permanecen en
  `ProyectoCafe.Infrastructure`.
- La API conserva únicamente detalles de transporte y seguridad perimetral
  (JWT, cookies HttpOnly, CSRF, CORS, autorización por políticas y catálogo de
  permisos de los endpoints).

## Build y pruebas

```powershell
Set-Location -Path "c:\ProyectoCafe\server\api"
dotnet build ProyectoCafe.sln --configuration Release -warnaserror
dotnet test ProyectoCafe.sln --configuration Release
```

## Imagen Docker

`Dockerfile` es multi-etapa y usa el contexto `server/api`:

- Build: `mcr.microsoft.com/dotnet/sdk:10.0`.
- Runtime: `mcr.microsoft.com/dotnet/aspnet:10.0`.
- El `restore` ocupa una capa propia que solo copia los `.csproj`.
- `publish` en `Release`, salida a `/app/publish` y `/p:UseAppHost=false`.
- El runtime corre como usuario no-root `app` (`$APP_UID`) en el puerto `8080`.
- `appsettings.Development.json` se elimina del artefacto publicado y
  `.dockerignore` excluye `bin`, `obj`, `.artifacts` (incluidos respaldos de BD),
  `.git`, `TestResults`, `.vs`, `*.user` y `.env*`. La imagen no contiene
  secretos ni cadenas de conexión.

### Build

```powershell
Set-Location -Path "c:\ProyectoCafe"
docker build -t pc-api:local server/api
```

La imagen local ocupa aproximadamente **459 MB** (`docker images`), sin contar
la caché de build. Si se compila en una máquina ARM para Azure Container Apps,
añade `--platform linux/amd64`.

### Run

```powershell
$key = [Convert]::ToBase64String(
    [System.Security.Cryptography.RandomNumberGenerator]::GetBytes(48))

docker run --rm --name pc-api-local -p 8080:8080 `
  -e "Database__ConnectionString=$env:Database__ConnectionString" `
  -e "Auth__Jwt__SigningKey=$key" `
  -e "Cors__AllowedOrigins__0=http://localhost:3010" `
  pc-api:local
```

Health checks:

```powershell
# Liveness: no consulta la base de datos
curl.exe -i http://localhost:8080/health
# HTTP/1.1 200 OK
# {"status":"ok"}

# Readiness: comprueba la conexión con SQL Server
curl.exe -i http://localhost:8080/health/ready
# HTTP/1.1 200 OK
# {"status":"Healthy"}
# Si la base de datos no responde: HTTP 503 {"status":"Unhealthy"}
```

En Azure Container Apps configura el *target port* `8080` y estos probes:

| Probe | App `pc-api` | App `pc-portal` |
| --- | --- | --- |
| Startup / Liveness | HTTP `GET /health` en `8080` | TCP en `3000` |
| Readiness | HTTP `GET /health/ready` en `8080` | TCP en `3000` |

`/health` es liveness y no depende de SQL; `/health/ready` solo responde `200` cuando
la API puede abrir conexión con la base de datos. La salida no incluye descripciones,
excepciones ni cadenas de conexión.

### Variables obligatorias

| Variable | Obligatoria | Descripción |
| --- | --- | --- |
| `Database__ConnectionString` | Sí | Cadena de conexión a SQL Server/Azure SQL. No está incluida en la imagen. |
| `Auth__Jwt__SigningKey` | Sí | Clave HS256 de al menos 32 bytes. En Azure Container Apps usa una referencia a Key Vault. |
| `Cors__AllowedOrigins__0` | Sí | Origen exacto del Portal (`esquema://host:puerto`). Para más orígenes usa `__1`, `__2`, ... No se admite `*`. |

Opcionales: `ASPNETCORE_ENVIRONMENT` (por defecto `Production`),
`Auth__Jwt__Issuer`, `Auth__Jwt__Audience`,
`Auth__Jwt__AccessTokenLifetimeMinutes`, `Auth__Cookie__SameSite`
(`Lax`/`Strict`/`None`), `Auth__Cookie__Secure` y el resto de claves documentadas en
[`DOCS/ENV.md`](../../DOCS/ENV.md). `ASPNETCORE_URLS` ya vale
`http://+:8080`; en Azure Container Apps configura el *target port* `8080`.

La API procesa `X-Forwarded-For` y `X-Forwarded-Proto` del ingress (solo el valor
derecho de `X-Forwarded-For`). No expongas `pc-api` directamente sin proxy: consulta
el riesgo de spoofing en [`DOCS/ENV.md`](../../DOCS/ENV.md).

En `Production` no se permite `Auth:Users` y la API **no aplica migraciones**:
las migraciones automáticas solo ocurren en `Development`. El job `deploy` de
CI aplica el script idempotente contra Azure SQL antes de actualizar la API. Para
un despliegue manual, aplica el mismo esquema con:

```powershell
Set-Location -Path "c:\ProyectoCafe"
$env:Database__ConnectionString = "<cadena de Azure SQL>"
pwsh -File .\scripts\db-migrate.ps1
```

`tools/sqlserver-migrate-current.sql` se genera con
`dotnet ef migrations script --idempotent` y puede aplicarse dos veces sin
error. Los detalles para regenerarlo están en
[`tools/README.md`](../../tools/README.md), el procedimiento manual en
[`DOCS/ENV.md`](../../DOCS/ENV.md) y el despliegue automatizado en
[`DOCS/DESPLIEGUE-AZURE.md`](../../DOCS/DESPLIEGUE-AZURE.md).

El bootstrap de seguridad reintenta la siembra de roles y permisos si dos
instancias chocan con un índice único. Aun así, configura `maxReplicas=1` en
`pc-api` como precaución durante el primer despliegue o una actualización de
esquema; el rate limiting de login también es en memoria y por instancia.

### Fail-fast al arrancar

La API valida la configuración antes de escuchar tráfico. Si algo falta, el
contenedor termina con código distinto de cero y `GET /health` no responde:

- Sin cadena de conexión:
  `Missing required config: Database:ConnectionString`.
- Sin clave JWT o con una clave corta/provisional: error de
  `Auth:Jwt:SigningKey` con la indicación de configurarla por variable de
  entorno o gestor de secretos.
- Sin orígenes CORS, con `*` o con una URL inválida: error de `Cors:AllowedOrigins`.
- Base de datos inaccesible o insegura (por ejemplo, firewall de Azure SQL o
  credenciales por defecto): `Security bootstrap no pudo ejecutarse. Se aborta
  el arranque de la API.` acompañado de la excepción original.

> Validación local de esta tarea: el build de la imagen fue correcto, se
> comprobó el fail-fast sin cadena de conexión y, con la IP del host autorizada
> en el firewall de Azure SQL, el comando de `docker run` anterior respondió
> `HTTP 200 {"status":"ok"}` en `GET /health`.
