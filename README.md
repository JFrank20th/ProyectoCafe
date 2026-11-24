# ProyectoCafe

ProyectoCafe migra la funcionalidad de berry-free-react-admin-template a una arquitectura limpia alineada a Next-RPS.

- Backend: `ProyectoCafe/server/api` (.NET, capas Domain/Application/Infrastructure/API)
- Frontend: `ProyectoCafe/Portal` (Next.js)

> Este repo está en construcción: migración por módulos, evitando copiar la estructura legacy.
>
> Alcance funcional actual: **Administración** (usuarios, roles y permisos),
> **Inventario** de café terminado y **Ventas**. La pantalla **Dashboard** se
> mantiene vacía, sin funcionalidad.

## Arquitectura del backend

El backend respeta una arquitectura por capas real:

- `Domain`: entidades y reglas de dominio, sin dependencias externas.
- `Application`: DTOs, casos de uso y puertos de persistencia/seguridad.
- `Infrastructure`: EF Core, migraciones y repositorios.
- `API`: Minimal APIs y detalles de transporte/seguridad perimetral.

Los endpoints no acceden directamente a `ProyectoCafeDbContext`: invocan los
servicios de Application, que a su vez usan las abstracciones implementadas por
Infrastructure. El detalle de reglas y estructura está en
[`server/api/README.md`](server/api/README.md).

# Configurar la clave de firma JWT (solo la primera vez; se guarda fuera del repo)
Set-Location -Path "c:\ProyectoCafe\server\api\ProyectoCafe.API"
dotnet user-secrets set "Auth:Jwt:SigningKey" "$([Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(48)))"

# Iniciar Back y front
Set-Location -Path "c:\ProyectoCafe\Portal"; npm run dev
Set-Location -Path "c:\ProyectoCafe\server\api\ProyectoCafe.API"; dotnet run

> Guía detallada de arranque local, credenciales de Development y solución de
> errores: [`DOCS/ARRANQUE-LOCAL.md`](DOCS/ARRANQUE-LOCAL.md).

# Base de datos (SQL Server / Azure SQL)

El esquema vigente se aplica con el script idempotente generado por EF Core
(`tools/sqlserver-migrate-current.sql`). El antiguo
`tools/sqlserver-create-current.sql` está **obsoleto**: no incluye Ventas ni los
catálogos de lote.

```powershell
# Cadena de conexión solo en el entorno; nunca en el repositorio.
$env:Database__ConnectionString = "Server=(localdb)\MSSQLLocalDB;Database=ProyectoCafe.Dev;Trusted_Connection=True;TrustServerCertificate=True"

# Aplica el esquema. Es idempotente: repetirlo no duplica tablas ni migraciones.
pwsh -File .\scripts\db-migrate.ps1 -CreateDatabaseIfMissing
```

Antes del primer despliegue manual, ejecuta `scripts/db-migrate.ps1` contra la
base existente (sin `-CreateDatabaseIfMissing`). En `Production` la API no
aplica migraciones; el workflow de despliegue aplica el mismo script idempotente
antes de actualizar `pc-api`. Procedimiento completo:
[`DOCS/ENV.md`](DOCS/ENV.md) y [`DOCS/DESPLIEGUE-AZURE.md`](DOCS/DESPLIEGUE-AZURE.md).

# Validación local

El build del Portal ya **no** ignora errores de TypeScript ni de ESLint. Para validar
todo antes de abrir un cambio:

```powershell
# Backend
Set-Location -Path "c:\ProyectoCafe\server\api"
dotnet build ProyectoCafe.sln --configuration Release -warnaserror
dotnet test ProyectoCafe.sln --configuration Release

# Frontend
Set-Location -Path "c:\ProyectoCafe\Portal"
npm ci
npm run lint
npm run type-check
npm test
npm run build
```

# Las dos imágenes juntas (Docker Compose)

`compose.yaml` levanta `pc-api` y `pc-portal` contra Azure SQL, con red interna,
healthchecks y variables tomadas de un `.env` local (gitignored):

```powershell
Copy-Item .env.example .env   # completa la cadena y la clave JWT
docker compose up --build
```

- Portal: <http://localhost:3010>
- API: <http://localhost:5000/health>

El navegador usa `PROYECTOCAFE_API_URL` y el servidor del Portal usa
`PROYECTOCAFE_API_URL_INTERNAL` (`http://api:8080`). Guía completa, firewall de
Azure SQL y login:
[`DOCS/ARRANQUE-LOCAL.md`](DOCS/ARRANQUE-LOCAL.md#5-levantar-api-y-portal-juntos-con-docker-compose-azure-sql).

# Infraestructura Azure (Bicep)

[`infra/`](infra/README.md) contiene la plantilla reproducible que crea ACR, Log
Analytics, el Container Apps Environment, Key Vault, las identidades y las apps
`pc-api`/`pc-portal` (ingress externo, probes, réplicas, CORS y referencias de
secretos). No crea Azure SQL: la base ya existe y su conectividad se configura
aparte.

```powershell
az bicep build --file infra/main.bicep
az deployment group what-if `
  --resource-group "<resource-group>" `
  --template-file infra/main.bicep `
  --parameters @infra/main.parameters.example.json
```

El orden de despliegue (fase base, imágenes, secretos y apps), las salidas para
el environment de GitHub y el rollback están en [`infra/README.md`](infra/README.md).

# Integración continua y despliegue

`.github/workflows/ci.yml` ejecuta las validaciones del backend y del frontend en cada
`push` a `main`/`master` y en cada `pull_request`. Cualquier fallo de compilación,
lint, TypeScript o pruebas marca el workflow como fallido (incluido el job agregador
`CI required`).

El job `deploy` se ejecuta después de `CI required`, solo en `push` a `main` (o
manualmente desde `main` con `deploy=true`), con el environment `production`. Usa
OIDC para autenticarse en Azure, construye y publica las imágenes de `pc-api` y
`pc-portal` en ACR, aplica las migraciones idempotentes desde Key Vault y actualiza
ambas Container Apps esperando una revisión saludable. El smoke test comprueba
`GET /health` de la API y `GET /login` del Portal. Guía de configuración y rollback:
[`DOCS/DESPLIEGUE-AZURE.md`](DOCS/DESPLIEGUE-AZURE.md).

Para bloquear la fusión de cambios, configura en GitHub la protección de la rama
principal y marca **`CI required`** como *required status check*.

