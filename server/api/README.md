# ProyectoCafe Backend (server/api)

Estructura por capas inspirada en Next-RPS:

- `ProyectoCafe.API` — Minimal APIs, bootstrap, Swagger
- `ProyectoCafe.Application` — DTOs, interfaces, servicios de aplicación
- `ProyectoCafe.Domain` — Entidades y reglas de dominio
- `ProyectoCafe.Infrastructure` — EF Core, repositorios, implementaciones

## Build

`dotnet build .\ProyectoCafe.sln`
