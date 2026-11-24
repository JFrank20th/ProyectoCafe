# Variables de entorno (ProyectoCafe)

## API

- `Database:ConnectionString` — connection string a SQL Server/Azure SQL. **No se commitea con credenciales**: en QA/PROD se configura mediante la variable de entorno `Database__ConnectionString` (Azure SQL Database). En Development puede definirse localmente con `dotnet user-secrets` o la misma variable de entorno. `appsettings.json` y `appsettings.Development.json` dejan el valor vacío para no versionar secretos.

### Secretos y credenciales

La API **no** incluye una `SigningKey` ni usuarios con contraseña por defecto. Si falta
la clave de firma, la clave es provisional o hay usuarios configurados fuera de
Development, la aplicación **falla al arrancar** antes de aceptar tráfico.

- `Auth:Jwt:SigningKey` — obligatoria, mínimo 32 bytes (256 bits) para HS256. No debe
  commitearse. Configúrala con la variable de entorno `Auth__Jwt__SigningKey` o con un
  gestor de secretos (Azure Key Vault, AWS Secrets Manager, Docker secrets, etc.).

Generar una clave aleatoria (PowerShell):

```powershell
$key = [Convert]::ToBase64String(
    [System.Security.Cryptography.RandomNumberGenerator]::GetBytes(48))
$env:Auth__Jwt__SigningKey = $key
```

En local (Development) se recomienda `dotnet user-secrets`, que guarda el valor fuera
del repositorio:

```powershell
Set-Location -Path "c:\ProyectoCafe\server\api\ProyectoCafe.API"
dotnet user-secrets set "Auth:Jwt:SigningKey" "$([Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(48)))"
```

La sección `Auth:Users` solo se admite en Development y almacena exclusivamente
`PasswordHash` (PBKDF2-HMAC-SHA256). Nunca uses la propiedad `Password`: el arranque
falla si detecta contraseñas en texto plano. Las contraseñas se hashean con
`PasswordHasher` y los hashes antiguos se rehashean automáticamente al iniciar sesión.

Usuarios de prueba incluidos en `appsettings.Development.json` (solo Development, con
contraseñas fuertes y hasheadas):

| Email | Contraseña de desarrollo | Rol |
| --- | --- | --- |
| `admin@local` | `Admin.Dev#2026!Cafe` | Admin |
| `user@local` | `User.Dev#2026!Cafe` | User |

En QA/PROD no se siembra ningún usuario. Crea el primer administrador por un canal
seguro (por ejemplo, registrando la cuenta y activándola directamente en la base de
datos) y no reutilices las credenciales de Development. Al arrancar fuera de Development
también se revisa la base de datos y se aborta el inicio si `admin@local` o `user@local`
conservan las contraseñas históricas `admin`/`user`.

### CORS

La API es la **única autoridad CORS** del sistema. El Portal (Next.js) no debe emitir
cabeceras `Access-Control-*`; hacerlo duplicaría o contradiría la respuesta de la API y
la autenticación por cookies dejaría de funcionar.

- La política usa una allowlist de orígenes exactos (`esquema://host:puerto`). No se
  admite `*`: `Access-Control-Allow-Origin: *` es incompatible con
  `Access-Control-Allow-Credentials: true`.
- La API falla al arrancar si no hay orígenes configurados, si alguno contiene `*`, o
  si no es una URL absoluta `http`/`https` sin ruta, query ni fragmento.
- La configuración se resuelve una sola vez al iniciar. Los orígenes se normalizan
  (minúsculas y sin barra final) y se eliminan duplicados.

Desarrollo local (`appsettings.Development.json`):

```json
"Cors": {
  "AllowedOrigins": ["http://localhost:3010"]
}
```

QA/PROD (variables de entorno, sin recompilar la imagen):

```powershell
# Formato estándar de .NET (recomendado para varios orígenes)
$env:Cors__AllowedOrigins__0 = "https://portal.example.com"
$env:Cors__AllowedOrigins__1 = "https://admin.example.com"

# Alternativa: lista separada por comas o punto y coma
$env:CORS_ALLOWED_ORIGINS = "https://portal.example.com,https://admin.example.com"
```

> Si el Portal y la API están en sitios distintos (no solo subdominios del mismo
> dominio registrable), además de CORS hay que configurar la cookie de sesión con
> `SameSite=None` y `Secure`; el valor por defecto `Lax` solo se envía en peticiones
> del mismo sitio. Consulta la sección «Cookies de sesión y SameSite».

### Cookies de sesión y SameSite

La API emite `pc_at` (JWT, `HttpOnly`) y `pc_csrf` (legible por JS) con la misma
política de sitio y seguridad:

- `Auth:Cookie:SameSite` — `Lax` (por defecto), `Strict` o `None`. Un valor distinto
  hace fallar el arranque.
- `Auth:Cookie:Secure` — booleano opcional. Si no se define, es `true` fuera de
  Development y `false` en Development. En QA/PROD debe permanecer `true`.
- `SameSite=None` exige `Secure=true` (explícito o por el valor por defecto del
  entorno); si no, la API falla al arrancar porque los navegadores rechazan esa
  combinación.

Decisión según el dominio del Portal y de la API:

| Escenario | `SameSite` | `Secure` | Notas |
| --- | --- | --- | --- |
| Mismo sitio (mismo dominio registrable, aunque sean subdominios distintos: `portal.example.com` y `api.example.com`) | `Lax` | `true` (QA/PROD) | Configuración recomendada. La cookie se envía porque el sitio es el mismo. |
| Sitios distintos (p. ej. dominios sin relación o `*.azurecontainerapps.io` separados) | `None` | `true` | HTTPS obligatorio y origen exacto en `Cors:AllowedOrigins`. Algunos navegadores bloquean cookies de terceros; valida el flujo en los navegadores objetivo. |
| Aislamiento máximo sin enlaces externos que deban conservar la sesión | `Strict` | `true` (QA/PROD) | La cookie no se envía en navegaciones provenientes de otros sitios. |

```powershell
# Mismo sitio (por defecto)
$env:Auth__Cookie__SameSite = "Lax"
$env:Auth__Cookie__Secure = "true"

# Portal y API en sitios distintos
$env:Auth__Cookie__SameSite = "None"
$env:Auth__Cookie__Secure = "true"
```

### Forwarded headers (ingress/proxy)

La API procesa `X-Forwarded-For` y `X-Forwarded-Proto` antes de autenticación y CORS
(`UseForwardedHeaders`). Así `RemoteIpAddress` y `Request.Scheme` reflejan al cliente
y no al proxy. La configuración usa `ForwardLimit = 1`: solo se acepta el valor
**más a la derecha** de `X-Forwarded-For`.

- Azure Container Apps garantiza únicamente el valor derecho de `X-Forwarded-For`;
  los valores que añada el cliente a la izquierda no son confiables y se ignoran.
- Como el ingress de ACA no es loopback, la API confía en el proxy de la plataforma
  (`KnownProxies`/`KnownIPNetworks` vacíos). **Riesgo de spoofing:** si el contenedor
  es accesible directamente sin pasar por el ingress, un cliente podría falsificar
  esas cabeceras e influir en la IP usada por el rate limiting y en el esquema
  percibido. Publica solo el Portal con ingress externo y mantén `pc-api` con
  ingress interno o sin exposición pública.
- `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` es una alternativa soportada. Si está
  activo, la API no registra su middleware por segunda vez (evita aplicar el límite
  dos veces). No combines ambas opciones con proxies que no sanitizen las cabeceras.

### Health checks y probes de Azure Container Apps

- `GET /health` — **liveness**. Anónimo, no consulta la base de datos. Responde
  `200 {"status":"ok"}`.
- `GET /health/ready` — **readiness**. Anónimo, comprueba que la API puede abrir
  conexión con SQL Server. Responde `200 {"status":"Healthy"}` o
  `503 {"status":"Unhealthy"}`. No incluye descripciones, excepciones ni cadenas de
  conexión.

Probes recomendados en Azure Container Apps:

| App | Target port | Startup / Liveness | Readiness |
| --- | --- | --- | --- |
| `pc-api` | `8080` | HTTP `GET /health` en `8080` | HTTP `GET /health/ready` en `8080` |
| `pc-portal` | `3000` | TCP en `3000` | TCP en `3000` |

La API usa `ASPNETCORE_URLS=http://+:8080`. El Portal escucha en `3000`. Mantén la
readiness de la API separada de la liveness para que un fallo temporal de SQL no
reinicie el contenedor.

### Resiliencia SQL

`UseSqlServer` usa `EnableRetryOnFailure()`: EF Core reintenta automáticamente los
errores transitorios (cortes de red, timeouts, bloqueos) con espera exponencial. Las
transacciones explícitas se ejecutan dentro de `CreateExecutionStrategy()` para ser
compatibles con los reintentos. Un error no transitorio (por ejemplo, una validación
de negocio o una restricción única) no se reintenta.

- En desarrollo, la conexión se resuelve desde `Database__ConnectionString` (Azure SQL) o `dotnet user-secrets`; las pruebas siguen usando LocalDB y no dependen de Azure.
- El esquema vigente se aplica con `tools/sqlserver-migrate-current.sql` mediante
  `scripts/db-migrate.ps1`. El script se genera con EF Core, es idempotente y
  sustituye al obsoleto `tools/sqlserver-create-current.sql`.

### Migrar el esquema antes de desplegar

En `Production` la API **no** aplica migraciones (`MigrateAsync` solo se ejecuta
en Development). El esquema debe estar al día antes de desplegar una nueva
versión de `pc-api`.

El job `deploy` de `.github/workflows/ci.yml` aplica automáticamente el script
idempotente antes de actualizar la API, leyendo la cadena de Azure SQL desde Key
Vault. El procedimiento manual siguiente se mantiene para la primera puesta en
marcha, entornos locales o despliegues fuera del workflow. Configuración completa:
[`DESPLIEGUE-AZURE.md`](DESPLIEGUE-AZURE.md).

1. Genera el script oficial si cambió alguna migración (ya está versionado en
   `tools/sqlserver-migrate-current.sql`):

   ```powershell
   Set-Location -Path "c:\ProyectoCafe\server\api"
   dotnet ef migrations script --idempotent `
     --project ProyectoCafe.Infrastructure `
     --startup-project ProyectoCafe.API `
     --output ..\..\tools\sqlserver-migrate-current.sql
   ```

2. Define la cadena en la variable de entorno de la sesión. No la escribas en
   archivos versionados ni la pases como argumento de línea de comandos:

   ```powershell
   $env:Database__ConnectionString = "<cadena de Azure SQL>"
   ```

3. Aplica el script contra la base existente:

   ```powershell
   pwsh -File .\scripts\db-migrate.ps1
   ```

   El script crea/actualiza las tablas y los índices, y registra cada migración
   en `dbo.__EFMigrationsHistory`. Es idempotente: ejecutarlo dos veces no
   duplica objetos ni historial. En desarrollo, para una base LocalDB que aún
   no existe, añade `-CreateDatabaseIfMissing`.

4. Verifica el resultado y solo entonces despliega la nueva revisión de
   `pc-api`. Si el script falla, el despliegue debe detenerse.

Códigos de salida de `db-migrate.ps1`: `0` correcto, `2` cadena ausente o
inválida, `3` script no encontrado, `4` proveedor SQL no disponible, `5` error
SQL y `6` error inesperado. La cadena no se imprime en ningún caso.

> `db-migrate.ps1` usa autenticación SQL o integrada de Windows. Si tu cadena
> usa Microsoft Entra ID, aplica el mismo script con
> `sqlcmd -S <servidor> -d <base> --authentication-method <método> -b -i tools/sqlserver-migrate-current.sql`.

### Bootstrap de seguridad y réplicas

Al arrancar, la API asegura los roles base (`Admin`, `User`) y los permisos del
rol `Admin`. Esas siembras son idempotentes y toleran concurrencia: si otra
instancia inserta el mismo rol o permiso entre la lectura y la escritura, se
captura el error de índice único de SQL Server (`2601`/`2627`), se descarta el
estado local y se vuelve a leer (hasta tres intentos).

Como precaución adicional durante el primer despliegue o una actualización de
esquema, configura `pc-api` con `maxReplicas=1` en Azure Container Apps. Varias
réplicas no corrompen el bootstrap, pero el rate limiting de login es en memoria
y está pensado por instancia; hasta migrarlo a un almacén distribuido, una sola
réplica mantiene el comportamiento esperado. Puedes aumentarlas después de
validar el arranque.

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
- `PROYECTOCAFE_API_URL` - **URL pública** del backend. Se publica al cliente en `window.__RUNTIME_ENV__` y es la que usa el navegador para llamar a la API. Tiene prioridad sobre la variable pública. En Docker/QA/PROD permite cambiar el destino sin recompilar la imagen.
- `PROYECTOCAFE_API_URL_INTERNAL` - **solo servidor**. URL que usan el middleware, los Server Components y los Route Handlers del Portal para validar la sesión. Si no se define, se usa `PROYECTOCAFE_API_URL`. No se inyecta en `window.__RUNTIME_ENV__`.
- `PROYECTOCAFE_AUTH_COOKIE_NAME` - nombre de la cookie HttpOnly de sesión emitida por la API (`pc_at` por defecto).
- `NEXT_PUBLIC_APP_NAME` - nombre a mostrar.

### URL dual en Docker Compose

En una red de Compose, el navegador y el servidor del Portal no resuelven los
mismos nombres. Configura las dos variables con el mismo contenedor:

```yaml
environment:
  PROYECTOCAFE_API_URL: http://localhost:5000          # navegador
  PROYECTOCAFE_API_URL_INTERNAL: http://api:8080       # servidor del Portal
```

- El navegador solo conoce `PROYECTOCAFE_API_URL`; debe ser alcanzable desde el
  host y coincidir con el origen permitido en `Cors:AllowedOrigins`
  (`http://localhost:3010` en local).
- El servidor del Portal usa `http://api:8080`, el nombre del servicio en la red
  interna. Nunca publiques esta URL al cliente.
- Si faltan ambas, el servidor cae a `NEXT_PUBLIC_PROYECTOCAFE_API_URL` y, por
  último, a `http://localhost:5000`.

El archivo `compose.yaml` de la raíz ya aplica este reparto. Ver
[`DOCS/ARRANQUE-LOCAL.md`](ARRANQUE-LOCAL.md#5-levantar-api-y-portal-juntos-con-docker-compose-azure-sql).

### Sesión por cookie HttpOnly

- `POST /api/v1/auth/login` responde únicamente la información del usuario (`user`: email, nombre, roles y permisos) y entrega el JWT en la cookie `pc_at` con `HttpOnly`, `Secure` fuera de Development (configurable con `Auth:Cookie:Secure`) y `SameSite=Lax` por defecto (configurable con `Auth:Cookie:SameSite`).
- El token CSRF se entrega en la cookie legible `pc_csrf`; el Portal lo reenvía en la cabecera `X-CSRF` en cada petición no idempotente.
- `POST /api/v1/auth/refresh` renueva el JWT y rota el token CSRF. Requiere sesión válida y `X-CSRF`.
- `POST /api/v1/auth/logout` elimina ambas cookies, incluso si la sesión ya caducó.
- El navegador envía la cookie automáticamente (`credentials: 'include'`). El JWT nunca se incluye en el body ni se guarda en `localStorage` o `sessionStorage`.

### Protección de rutas en el servidor

El Portal incluye `src/middleware.ts` (runtime Node.js de Next.js 15) que valida la cookie de sesión contra `GET /api/v1/auth/me` antes de renderizar cualquier página protegida:

- `login` y `register` son públicos; el resto requiere sesión válida (deny-by-default).
- `/administracion/usuarios` y `/administracion/roles` requieren además `Administracion.Usuarios:Ver` y `Administracion.Roles:Ver` respectivamente.
- Sin sesión en una página: redirección a `/login?returnUrl=<ruta original>`.
- Con sesión pero sin permiso: redirección a `/acceso-denegado`.
- Las peticiones a `/api/*` del Portal responden `401`/`403` en JSON (nunca redirigen al login).
- Si la API no responde o devuelve un error, la sesión se considera inválida (fail closed) y se limpia la cookie.

El guard del cliente (`AppShell`/`SessionGuard`) se mantiene como capa adicional. La API sigue siendo la autoridad final: revalida sesión, permisos, CSRF y estado del usuario en cada operación.
