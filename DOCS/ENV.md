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

- `NEXT_PUBLIC_PROYECTOCAFE_API_URL` — base URL del backend (ej: `http://localhost:5000`).
- `NEXT_PUBLIC_APP_NAME` — nombre a mostrar.
