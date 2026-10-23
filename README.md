# ProyectoCafe

ProyectoCafe migra la funcionalidad de berry-free-react-admin-template a una arquitectura limpia alineada a Next-RPS.

- Backend: `ProyectoCafe/server/api` (.NET, capas Domain/Application/Infrastructure/API)
- Frontend: `ProyectoCafe/Portal` (Next.js)

> Este repo está en construcción: migración por módulos, evitando copiar la estructura legacy.
>
> Alcance funcional actual: **Administración** (usuarios, roles y permisos). El resto de módulos operativos fue retirado. La pantalla **Dashboard** se mantiene vacía, sin funcionalidad.

# Iniciar Back y front
Set-Location -Path "c:\ProyectoCafe\Portal"; npm run dev
Set-Location -Path "c:\ProyectoCafe\server\api\ProyectoCafe.API"; dotnet run

# Base de datos (SQL Server)
# Crea la base ProyectoCafe.Dev con el esquema vigente (Administración).
sqlcmd -S localhost -E -i "c:\ProyectoCafe\tools\sqlserver-create-current.sql" -b

# Si la base ya existe, el script se detiene. Para recrearla de forma explícita:
# 1. Respalda la base actual.
# 2. Cambia @RecreateExisting a 1 dentro del script y vuelve a ejecutarlo.
