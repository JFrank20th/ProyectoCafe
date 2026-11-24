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
Set-Location -Path "c:\ProyectoCafe\Portal"; npm ci; npm run dev # para intalar nodeModules
Set-Location -Path "c:\ProyectoCafe\Portal"; npm run dev
```

## 5. Entrar

| Email | Contraseña | Rol |
| --- | --- | --- |
| `admin@local` | `Admin.Dev#2026!Cafe` | Admin |
| `user@local` | `User.Dev#2026!Cafe` | User |

Solo existen en Development.

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

Más detalle: [`DOCS/ENV.md`](ENV.md) · [`README.md`](../README.md)
