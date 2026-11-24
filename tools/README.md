# Scripts de base de datos

## Vigente

- `sqlserver-migrate-current.sql` — **script oficial del esquema**. Se genera con
  EF Core a partir de las migraciones y es idempotente: puede aplicarse sobre una
  base vacía o ya migrada sin duplicar objetos ni filas del historial.
- `../scripts/db-migrate.ps1` — aplica el script anterior usando la cadena de
  conexión de la variable de entorno `Database__ConnectionString`.
- `.github/workflows/ci.yml` (job `deploy`) — aplica el mismo script con
  `azure/sql-action` antes de actualizar `pc-api`; no requiere Docker ni `sqlcmd`
  local.

```powershell
$env:Database__ConnectionString = "<cadena de conexión>"
pwsh -File .\scripts\db-migrate.ps1
```

Para desarrollo local, si la base todavía no existe:

```powershell
$env:Database__ConnectionString = "Server=(localdb)\MSSQLLocalDB;Database=ProyectoCafe.Dev;Trusted_Connection=True;TrustServerCertificate=True"
pwsh -File .\scripts\db-migrate.ps1 -CreateDatabaseIfMissing
```

## Obsoletos (solo referencia histórica)

- `sqlserver-create-current.sql` — creación manual antigua; no incluye el esquema
  `Ventas` ni los catálogos de lote.
- `sqlserver-reset-operacion-real.sql` — limpieza con un baseline de migraciones
  anterior a Ventas.

No uses estos dos scripts para preparar Azure SQL ni una base nueva.

## Regenerar el script oficial

Cada vez que se agrega o modifica una migración:

```powershell
Set-Location -Path "c:\ProyectoCafe\server\api"
dotnet ef migrations script --idempotent `
  --project ProyectoCafe.Infrastructure `
  --startup-project ProyectoCafe.API `
  --output ..\..\tools\sqlserver-migrate-current.sql
```

No edites el archivo generado a mano. Si tu `dotnet-ef` local es más antiguo que
los paquetes de EF Core del proyecto, actualízalo (`dotnet tool update --local
dotnet-ef`) para eliminar la advertencia de versión.
