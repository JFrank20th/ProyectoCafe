# Arranque local (Windows)

Todo en PowerShell, desde cero.

## 1. Requisitos

.NET SDK 10, Node.js, SQL Server local.

```powershell
dotnet --version   # 10.x
node -v
sqlcmd -?
```

## 2. Clave JWT (solo la primera vez)

La API no arranca sin `Auth:Jwt:SigningKey`. Se guarda fuera del repo con
`user-secrets` (solo se carga en Development):

```powershell
Set-Location -Path "c:\ProyectoCafe\server\api\ProyectoCafe.API"
dotnet user-secrets set "Auth:Jwt:SigningKey" "$([Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(48)))"
dotnet user-secrets list   # debe mostrar Auth:Jwt:SigningKey
```

La clave debe tener **≥32 bytes**, no ser un placeholder (`CHANGE_ME`, `DEV_ONLY`, …)
y tener al menos 8 caracteres distintos. Nunca la pongas en `appsettings*.json`.

## 3. Base de datos

El esquema se aplica con el script idempotente generado por EF Core. La cadena
va en una variable de entorno, nunca en el repositorio:

```powershell
$env:Database__ConnectionString = "Server=(localdb)\MSSQLLocalDB;Database=ProyectoCafe.Dev;Trusted_Connection=True;TrustServerCertificate=True"

# Crea la base si falta y aplica el esquema vigente.
pwsh -File .\scripts\db-migrate.ps1 -CreateDatabaseIfMissing
```

Puedes ejecutar `scripts/db-migrate.ps1` de nuevo sin
`-CreateDatabaseIfMissing`: es idempotente y no duplica tablas, índices ni
migraciones. En Development la API también aplica migraciones al iniciar, pero
este script es el procedimiento oficial y el mismo que debes usar antes de
desplegar a Azure SQL. El antiguo `tools/sqlserver-create-current.sql` está
obsoleto.

## 4. Levantar API y Portal

```powershell
# Terminal 1 — API en http://localhost:5000 (Swagger en /swagger)
Set-Location -Path "c:\ProyectoCafe\server\api\ProyectoCafe.API"; dotnet run
```

```powershell
# Terminal 2 — Portal en http://localhost:3010
Set-Location -Path "c:\ProyectoCafe\Portal"; npm ci   # instala node_modules
Set-Location -Path "c:\ProyectoCafe\Portal"; npm run dev
```

## 5. Levantar API y Portal juntos con Docker Compose (Azure SQL)

Esta ruta valida las dos imágenes a la vez. La API se ejecuta con
`ASPNETCORE_ENVIRONMENT=Production` contra Azure SQL, sin sembrar usuarios de
prueba. Requiere Docker Desktop en marcha y una base Azure SQL ya existente.

### 5.1 Permitir la IP del desarrollador en Azure SQL

El contenedor sale a Internet con la IP pública de tu equipo. Añádela al
firewall del servidor (no de la base):

- Azure Portal → servidor SQL → **Networking** → **Add your client IPv4
  address** (o **Add current client IP address**).
- O con Azure CLI:

```powershell
$publicIp = (Invoke-RestMethod https://api.ipify.org)
az sql server firewall-rule create `
  --resource-group <grupo> `
  --server <servidor> `
  --name DevLocal `
  --start-ip-address $publicIp `
  --end-ip-address $publicIp
```

Comprueba la conectividad antes de levantar Compose:

```powershell
sqlcmd -S "<servidor>.database.windows.net" -d "<base>" -U "<usuario>" -P "<password>" -Q "SELECT 1"
```

Si cambias de red (oficina, VPN, casa) la IP cambia: repite este paso.

### 5.2 Crear `.env` (no se commitea)

```powershell
Set-Location -Path "c:\ProyectoCafe"
Copy-Item .env.example .env
notepad .env
```

Completa como mínimo:

- `DATABASE_CONNECTION_STRING`: cadena de Azure SQL, entre comillas simples.
- `JWT_SIGNING_KEY`: clave aleatoria de al menos 32 bytes:

```powershell
[Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(48))
```

`.env` está en `.gitignore`. No lo commitees ni lo copies a la imagen.
`compose.yaml` solo contiene referencias `${...}` y valores no sensibles.

### 5.3 Aplicar el esquema

En `Production` la API **no** aplica migraciones. Si la base ya está al día,
puedes saltar este paso; si no:

```powershell
$env:Database__ConnectionString = '<la misma cadena de .env>'
pwsh -File .\scripts\db-migrate.ps1
Remove-Item Env:Database__ConnectionString
```

El script es idempotente.

### 5.4 Levantar las dos imágenes

`compose.yaml` construye las dos imágenes con los mismos Dockerfiles que usa Azure
(`server/api/Dockerfile` y `Portal/Dockerfile`). Comprueba la configuración
resuelta antes de arrancar (no imprime los secretos en claro):

```powershell
Set-Location -Path "c:\ProyectoCafe"
docker compose config --quiet
docker compose up --build
```

Si quieres construir las imágenes por separado:

```powershell
docker build -t pc-api:local server/api
docker build -t pc-portal:local Portal
```

Para ejecutarlas coordinadas usa `docker compose up`; el ejemplo de `docker run`
de una sola imagen, con sus variables, está en
[`../server/api/README.md`](../server/api/README.md#run).

La imagen de la API escucha en `8080` y la del Portal en `3000`; Compose publica
`5000` y `3010` en el host. No cambies el puerto interno de la API: Azure Container
Apps usa el mismo target port `8080`.

| Servicio | Contenedor | Puerto host | Puerto interno | URL / comprobación |
| --- | --- | --- | --- | --- |
| Portal | `pc-portal` | `${PORTAL_PORT:-3010}` | `3000` | <http://localhost:3010>; `/login` responde `200` |
| API | `pc-api` | `${API_PORT:-5000}` | `8080` | <http://localhost:5000/health> → `{"status":"ok"}` |
| API readiness | `pc-api` | `${API_PORT:-5000}` | `8080` | <http://localhost:5000/health/ready> → `200` si Azure SQL responde |
| Azure SQL | externo | no aplica | `1433` | La API lo valida en readiness |

La topología y los puertos de Azure son los mismos, con ingress ACA en lugar de
puertos publicados: `pc-portal` `443` → `3000` y `pc-api` `443` → `8080`. Ver
[`../README.md#arquitectura-de-despliegue`](../README.md#arquitectura-de-despliegue).

- El navegador usa `PROYECTOCAFE_API_URL` (`http://localhost:5000`). Es la URL
  que `layout.tsx` publica en `window.__RUNTIME_ENV__`.
- El servidor del Portal (middleware y Server Components) usa
  `PROYECTOCAFE_API_URL_INTERNAL` (`http://api:8080`), un host que solo existe
  en la red privada de Compose.
- La API solo acepta el origen exacto `http://localhost:3010` (`Cors`).
- Para ver registros: `docker compose logs -f api` o `docker compose logs -f portal`.
- Para ver el estado de los servicios: `docker compose ps`.
- Para parar: `docker compose down`.

### 5.5 Iniciar sesión

1. Abre <http://localhost:3010/login>.
2. Usa un usuario **activo que ya exista en Azure SQL** y su contraseña.

La imagen corre en `Production`: no crea `admin@local` ni `user@local`, y
aborta el arranque si detecta esas cuentas con contraseñas predeterminadas. Si
no tienes un administrador activo, regístralo en `/register` y actívalo desde
la base de datos o con otro administrador existente; el registro deja la cuenta
en `PendienteActivacion`.

Tras entrar, el Portal renueva la sesión con `POST /api/v1/auth/refresh`
(incluyendo `X-CSRF`) y valida las rutas protegidas contra
`GET /api/v1/auth/me`. El middleware usa la URL interna, por lo que Login,
refresh y las pantallas protegidas funcionan sin que el navegador conozca
`api:8080`.

Errores habituales en esta ruta:

| Síntoma | Causa | Solución |
| --- | --- | --- |
| `api` reinicia y el log dice error de bootstrap/SQL | Firewall o cadena incorrecta | Revisa 5.1 y 5.2 |
| Login responde `401`/`403` | Cuenta inexistente, inactiva o contraseña incorrecta | Usa una cuenta activa de Azure SQL |
| El login “funciona” pero vuelve al login | Cookie `Secure` sobre HTTP | Mantén `Auth__Cookie__Secure=false` en Compose local |
| Error CORS en el navegador | Origen distinto de `http://localhost:3010` | Ajusta `PORTAL_PUBLIC_ORIGIN` y `PORTAL_PORT` |
| `DATABASE_CONNECTION_STRING` requerida | `.env` ausente o vacío | Repite 5.2 |

## 6. Entrar

| Email | Contraseña | Rol |
| --- | --- | --- |
| `admin@local` | `Admin.Dev#2026!Cafe` | Admin |
| `user@local` | `User.Dev#2026!Cafe` | User |

Solo existen en Development. No se crean en la ruta Docker Compose (paso 5),
que corre en `Production` contra Azure SQL.

---

## Error: `Auth:Jwt:SigningKey es requerido`

Es la validación de `Program.cs:19`, no un bug. Revisa, en orden:

1. ¿Falta la clave? → paso 2.
2. ¿Existe pero es corta, repetitiva o placeholder? → genera otra con el paso 2.
3. ¿Una variable de entorno la pisa? (tiene prioridad sobre `user-secrets`):

```powershell
Get-ChildItem Env:Auth__Jwt__SigningKey
Remove-Item Env:Auth__Jwt__SigningKey
```

Si se creó con `setx`, bórrala también del perfil:

```powershell
[Environment]::SetEnvironmentVariable("Auth__Jwt__SigningKey", $null, "User")
```

> Tras cambiar la clave, las sesiones anteriores dejan de ser válidas.

## Otros errores

| Síntoma | Causa | Solución |
| --- | --- | --- |
| `debe tener al menos 32 bytes` / `valor provisional` | Clave inválida | Generar una aleatoria (paso 2) |
| `Auth:Users no está permitido fuera de Development` | Usuarios de prueba en QA/PROD | Quitarlos de esa configuración |
| `CORS: no hay orígenes` / comodín | `Cors:AllowedOrigins` vacío o con `*` | Orígenes exactos, sin `*` |
| El Portal no conecta | URL/puerto incorrecto | API `:5000`, Portal `:3010` |
| Login `429` | Rate limit | Esperar lo que indique `Retry-After` |
| La API inicia pero auth falla | BD inexistente | Paso 3 |

## Secretos: comandos útiles

```powershell
dotnet user-secrets list                                # ver
dotnet user-secrets set "Auth:Jwt:SigningKey" "<valor>" # crear/reemplazar
dotnet user-secrets remove "Auth:Jwt:SigningKey"        # borrar
```

Ubicación: `%APPDATA%\Microsoft\UserSecrets\c853932b-aecf-4394-b660-490e286f2ea0\secrets.json`

## Checklist

- [ ] .NET 10 instalado
- [ ] `Auth:Jwt:SigningKey` configurada
- [ ] Base `ProyectoCafe.Dev` creada y esquema aplicado con `scripts/db-migrate.ps1`
- [ ] API en `:5000` y Portal en `:3010`
- [ ] Login con `admin@local` funciona
- [ ] Docker Desktop instalado y `.env` creado (ruta Compose)
- [ ] IP del desarrollador permitida en el firewall de Azure SQL
- [ ] `docker compose config --quiet` valida el archivo sin errores
- [ ] `docker compose up --build` levanta API y Portal contra Azure SQL
- [ ] Login con una cuenta activa de Azure SQL funciona en `http://localhost:3010`
- [ ] `/inventario/CafeTostado` y `/dashboard/ventas` cargan con los permisos adecuados

Más detalle: [`DOCS/ENV.md`](ENV.md) · [`README.md`](../README.md)
