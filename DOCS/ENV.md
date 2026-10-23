# Variables de entorno (ProyectoCafe)

## API

- `Database:ConnectionString` — connection string a SQL Server (puede estar en `appsettings.Development.json` o env var).
- En desarrollo, la conexión por defecto apunta a `ProyectoCafe.Dev`.
- La estructura vigente se crea con `tools/sqlserver-create-current.sql`; el script crea la base desde cero y se detiene si ya existe para evitar mezclar esquemas heredados.

## Portal

- `NEXT_PUBLIC_PROYECTOCAFE_API_URL` — base URL del backend (ej: `http://localhost:5000`).
- `NEXT_PUBLIC_APP_NAME` — nombre a mostrar.
