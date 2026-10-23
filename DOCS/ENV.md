# Variables de entorno (ProyectoCafe)

## API

- `Database:ConnectionString` — connection string a SQL Server (puede estar en `appsettings.Development.json` o env var).
- En desarrollo, la conexión por defecto apunta a `ProyectoCafe.Dev`.
- La estructura vigente se crea con `tools/sqlserver-create-current.sql`; el script crea la base desde cero y se detiene si ya existe para evitar mezclar esquemas heredados.

### Rate limiting de login

La API protege `POST /api/v1/auth/login` con ventanas deslizantes de intentos fallidos por IP y por email, además del bloqueo persistente existente (5 intentos fallidos → cuenta bloqueada 15 minutos). Los valores por defecto están en `appsettings.json`:

- `Auth:RateLimit:Enabled` — activa/desactiva el rate limiting en memoria (`true` por defecto).
- `Auth:RateLimit:IpMaxFailedAttempts` — intentos fallidos permitidos por IP (20).
- `Auth:RateLimit:IpWindowMinutes` — ventana de la IP en minutos (5).
- `Auth:RateLimit:UserMaxFailedAttempts` — intentos fallidos permitidos por email (10).
- `Auth:RateLimit:UserWindowMinutes` — ventana del email en minutos (15).
- `Auth:RateLimit:MaxTrackedKeys` — tope de claves en memoria para evitar crecimiento por emails aleatorios (10000).

Al superarse un límite se responde `429` con un mensaje genérico y cabecera `Retry-After`. Los intentos fallidos, bloqueos y rechazos por rate limit se registran con nivel `Warning` incluyendo IP y email enmascarado; nunca se registra la contraseña.

> El almacén es en memoria y por instancia. Si la API se despliega con varias réplicas, se debe sustituir `ILoginRateLimiter` por una implementación distribuida (por ejemplo Redis). Detrás de un proxy inverso, configura forwarded headers para que `RemoteIpAddress` refleje la IP real del cliente.

## Portal

- `NEXT_PUBLIC_PROYECTOCAFE_API_URL` - base URL del backend (ej: `http://localhost:5000`). Se usa en el cliente y como valor por defecto del servidor.
- `PROYECTOCAFE_API_URL` - base URL del backend usada en runtime por el servidor (middleware, Server Components y Route Handlers). Tiene prioridad sobre la variable pública. Úsala en Docker/QA/PROD para no recompilar la imagen al cambiar el destino.
- `PROYECTOCAFE_AUTH_COOKIE_NAME` - nombre de la cookie HttpOnly de sesión emitida por la API (`pc_at` por defecto).
- `NEXT_PUBLIC_APP_NAME` - nombre a mostrar.

### Protección de rutas en el servidor

El Portal incluye `src/middleware.ts` (runtime Node.js de Next.js 15) que valida la cookie de sesión contra `GET /api/v1/auth/me` antes de renderizar cualquier página protegida:

- `login` y `register` son públicos; el resto requiere sesión válida (deny-by-default).
- `/administracion/usuarios` y `/administracion/roles` requieren además `Administracion.Usuarios:Ver` y `Administracion.Roles:Ver` respectivamente.
- Sin sesión en una página: redirección a `/login?returnUrl=<ruta original>`.
- Con sesión pero sin permiso: redirección a `/acceso-denegado`.
- Las peticiones a `/api/*` del Portal responden `401`/`403` en JSON (nunca redirigen al login).
- Si la API no responde o devuelve un error, la sesión se considera inválida (fail closed) y se limpia la cookie.

El guard del cliente (`AppShell`/`SessionGuard`) se mantiene como capa adicional. La API sigue siendo la autoridad final: revalida sesión, permisos, CSRF y estado del usuario en cada operación.
