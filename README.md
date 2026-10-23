# ProyectoCafe

ProyectoCafe migra la funcionalidad de berry-free-react-admin-template a una arquitectura limpia alineada a Next-RPS.

- Backend: `ProyectoCafe/server/api` (.NET, capas Domain/Application/Infrastructure/API)
- Frontend: `ProyectoCafe/Portal` (Next.js)

> Este repo está en construcción: migración por módulos, evitando copiar la estructura legacy.

# Iniciar Back y front
Set-Location -Path "c:\ProyectoCafe\Portal"; npm run dev
Set-Location -Path "c:\ProyectoCafe\server\api\ProyectoCafe.API"; dotnet run

# Limpieza de base de datos.
sqlcmd -S localhost -d ProyectoCafe.Dev -E -i "c:\ProyectoCafe\tools\sqlserver-reset-operacion-real.sql" -b
