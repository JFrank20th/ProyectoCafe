# Windows env vars (ProyectoCafe)

Recomendado: configurar secrets como variables de entorno de usuario/máquina en lugar de commitear valores.

Ejemplos (PowerShell):

- `setx Database__ConnectionString "Server=...;Database=...;..."`
- `setx Auth__Jwt__SigningKey "<clave aleatoria de al menos 32 bytes>"`
- `setx NEXT_PUBLIC_PROYECTOCAFE_API_URL "http://localhost:5000"`

Generar una clave de firma aleatoria:

```powershell
$key = [Convert]::ToBase64String(
    [System.Security.Cryptography.RandomNumberGenerator]::GetBytes(48))
setx Auth__Jwt__SigningKey $key
```

En desarrollo también puedes usar `dotnet user-secrets` (no requiere reiniciar la sesión):

```powershell
Set-Location -Path "c:\ProyectoCafe\server\api\ProyectoCafe.API"
dotnet user-secrets set "Auth:Jwt:SigningKey" "$([Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(48)))"
```

La API no arranca si `Auth:Jwt:SigningKey` falta, es un valor provisional o si
`Auth:Users` está configurado fuera de Development.
