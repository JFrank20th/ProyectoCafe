# ProyectoCafe

ProyectoCafe migra la funcionalidad de berry-free-react-admin-template a una arquitectura limpia alineada a Next-RPS.

- Backend: `ProyectoCafe/server/api` (.NET, capas Domain/Application/Infrastructure/API)
- Frontend: `ProyectoCafe/Portal` (Next.js)

> Este repo está en construcción: migración por módulos, evitando copiar la estructura legacy.
>
> Alcance funcional actual: **Administración** (usuarios, roles y permisos). El resto de módulos operativos fue retirado. La pantalla **Dashboard** se mantiene vacía, sin funcionalidad.

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

# Base de datos (SQL Server)
# Crea la base ProyectoCafe.Dev con el esquema vigente (Administración).
sqlcmd -S localhost -E -i "c:\ProyectoCafe\tools\sqlserver-create-current.sql" -b

# Si la base ya existe, el script se detiene. Para recrearla de forma explícita:
# 1. Respalda la base actual.
# 2. Cambia @RecreateExisting a 1 dentro del script y vuelve a ejecutarlo.

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

# Integración continua

`.github/workflows/ci.yml` ejecuta las validaciones del backend y del frontend en cada
`push` a `main`/`master` y en cada `pull_request`. Cualquier fallo de compilación,
lint, TypeScript o pruebas marca el workflow como fallido (incluido el job agregador
`CI required`).

Para bloquear la fusión de cambios, configura en GitHub la protección de la rama
principal y marca **`CI required`** como *required status check*.

