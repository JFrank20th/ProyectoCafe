# ProyectoCafe Backend (server/api)

Estructura por capas inspirada en Next-RPS. Cada capa solo depende de las capas
interiores y la persistencia queda aislada en Infrastructure.

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
