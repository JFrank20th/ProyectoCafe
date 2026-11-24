#requires -Version 5.1
<#
.SYNOPSIS
    Smoke test de extremo a extremo de ProyectoCafe en Azure.

.DESCRIPTION
    Valida la configuración final de Azure sin Azure CLI:
      1. GET /health de la API.
      2. Preflight CORS del origen del Portal.
      3. POST /api/v1/auth/login y flags de las cookies pc_at/pc_csrf.
      4. GET /api/v1/auth/me con la cookie de sesión.
      5. CSRF: POST /api/v1/auth/refresh sin X-CSRF (403) y con X-CSRF (200).
      6. Administración: /api/v1/admin/users y /administracion/usuarios.
      7. Ventas: /api/v1/ventas/ventas, /precios y /dashboard/ventas.
      8. Inventario: /api/v1/inventario/lotes, /tipos-cafe y
         /inventario/CafeTostado.
      9. Portal: /login, /dashboard y pantallas protegidas sin redirección.

    No imprime la contraseña ni el valor de las cookies. Devuelve un código de
    salida distinto de 0 en el primer fallo.

.PARAMETER ApiBaseUrl
    URL pública de la API, por ejemplo https://api.tudominio.com. Si se omite,
    usa la variable de entorno API_BASE_URL.

.PARAMETER PortalBaseUrl
    URL pública del Portal, por ejemplo https://app.tudominio.com. Si se omite,
    usa PORTAL_BASE_URL. Obligatoria salvo con -SkipPortal.

.PARAMETER Email
    Usuario real con permisos de lectura de Administración, Ventas e Inventario.
    Si se omite, usa PROYECTOCAFE_SMOKE_EMAIL.

.PARAMETER Password
    Contraseña del usuario. Si se omite, usa PROYECTOCAFE_SMOKE_PASSWORD. No la
    pases por la línea de comandos en entornos compartidos: el historial de la
    shell puede conservarla.

.PARAMETER ExpectedCookieSameSite
    SameSite esperado en pc_at y pc_csrf: Lax (por defecto), Strict o None.

.PARAMETER ExpectedCookieDomain
    Dominio esperado en el atributo Domain (por ejemplo tudominio.com). Si no se
    indica, no se valida el dominio.

.PARAMETER SkipPortal
    Omite CORS y las comprobaciones del Portal. Útil para validar solo la API.

.PARAMETER TimeoutSec
    Timeout de cada petición HTTP en segundos (por defecto 30).

.EXAMPLE
    $env:PROYECTOCAFE_SMOKE_EMAIL = "smoke-admin@tudominio.com"
    $env:PROYECTOCAFE_SMOKE_PASSWORD = "<contraseña>"
    pwsh -File .\scripts\smoke-azure.ps1 `
        -ApiBaseUrl "https://api.tudominio.com" `
        -PortalBaseUrl "https://app.tudominio.com" `
        -ExpectedCookieSameSite Lax `
        -ExpectedCookieDomain "tudominio.com"

.EXAMPLE
    $env:API_BASE_URL = "https://api.tudominio.com"
    $env:PROYECTOCAFE_SMOKE_EMAIL = "smoke-admin@tudominio.com"
    $env:PROYECTOCAFE_SMOKE_PASSWORD = "<contraseña>"
    pwsh -File .\scripts\smoke-azure.ps1 -SkipPortal

.NOTES
    Códigos de salida:
      0  = todo correcto
      2  = argumentos, URL o dependencias inválidas
      3  = /health falló
      4  = CORS/preflight falló
      5  = login falló
      6  = cookies pc_at/pc_csrf ausentes o con flags incorrectos
      7  = /api/v1/auth/me falló
      8  = CSRF falló (sin cabecera no devolvió 403 o con cabecera no devolvió 200)
      9  = administración falló
      10 = ventas falló
      11 = inventario falló
      12 = Portal falló (login, dashboard o pantalla protegida)
      13 = error inesperado
#>
[CmdletBinding()]
param(
    [string]$ApiBaseUrl = $env:API_BASE_URL,
    [string]$PortalBaseUrl = $env:PORTAL_BASE_URL,
    [string]$Email = $env:PROYECTOCAFE_SMOKE_EMAIL,
    [string]$Password = $env:PROYECTOCAFE_SMOKE_PASSWORD,
    [ValidateSet('Lax', 'Strict', 'None')]
    [string]$ExpectedCookieSameSite = 'Lax',
    [string]$ExpectedCookieDomain = $env:PROYECTOCAFE_COOKIE_DOMAIN,
    [switch]$SkipPortal,
    [int]$TimeoutSec = 30
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$script:SupportsSkipHttpError = $PSVersionTable.PSVersion.Major -ge 7

# Windows PowerShell 5.1 puede negociar TLS 1.0 por defecto en instalaciones
# antiguas. Azure Container Apps exige TLS 1.2 o superior.
if ($PSVersionTable.PSVersion.Major -lt 6) {
    try {
        [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    }
    catch {
        # Si la plataforma no lo permite, la petición fallará con un error claro.
    }
}

function Stop-Smoke {
    param(
        [Parameter(Mandatory = $true)][int]$Code,
        [Parameter(Mandatory = $true)][string]$Message
    )

    [Console]::Error.WriteLine("FALLO [$Code]: $Message")
    exit $Code
}

function Write-Step {
    param([Parameter(Mandatory = $true)][string]$Message)
    Write-Host "==> $Message"
}

function Invoke-SmokeRequest {
    param(
        [Parameter(Mandatory = $true)][string]$Method,
        [Parameter(Mandatory = $true)][string]$Url,
        [hashtable]$Headers,
        [string]$Body,
        [Microsoft.PowerShell.Commands.WebRequestSession]$Session
    )

    $params = @{
        Method             = $Method
        Uri                = $Url
        UseBasicParsing    = $true
        TimeoutSec         = $TimeoutSec
        ErrorAction        = 'Stop'
        MaximumRedirection = 10
    }

    if ($Headers) { $params.Headers = $Headers }
    if ($Body) {
        $params.Body = $Body
        $params.ContentType = 'application/json'
    }
    if ($Session) { $params.WebSession = $Session }
    if ($script:SupportsSkipHttpError) { $params.SkipHttpErrorCheck = $true }

    try {
        return Invoke-WebRequest @params
    }
    catch {
        # PowerShell 5.1 lanza una excepción en 4xx/5xx. Se normaliza la
        # respuesta para poder comprobar los códigos esperados (403 de CSRF) y
        # para que un fallo de red se reporte con el código del paso concreto.
        $errorResponse = $null
        if ($_.Exception.PSObject.Properties['Response']) {
            $errorResponse = $_.Exception.Response
        }
        if ($null -eq $errorResponse) {
            return [pscustomobject]@{
                StatusCode   = 0
                Headers      = @{}
                Content      = ''
                BaseResponse = $null
                IsSynthetic  = $true
            }
        }

        $status = 0
        try { $status = [int]$errorResponse.StatusCode } catch { $status = 0 }

        return [pscustomobject]@{
            StatusCode   = $status
            Headers      = @{}
            Content      = ''
            BaseResponse = $errorResponse
            IsSynthetic  = $true
        }
    }
}

function Get-HeaderValues {
    param(
        $Response,
        [Parameter(Mandatory = $true)][string]$Name
    )

    if ($null -eq $Response) { return @() }

    $headers = $null
    try { $headers = $Response.Headers } catch { $headers = $null }
    if ($null -eq $headers) { return @() }

    try {
        $values = $headers.GetValues($Name)
        if ($null -ne $values) { return @($values) }
    }
    catch {
        # La colección no tiene el encabezado o no expone GetValues().
    }

    try {
        $value = $headers[$Name]
        if ($null -ne $value) { return @($value) }
    }
    catch {
        # Encabezado ausente.
    }

    return @()
}

function Get-ResponseStatusCode {
    param($Response)

    if ($null -eq $Response) { return 0 }
    try { return [int]$Response.StatusCode } catch { return 0 }
}

function Assert-Status {
    param(
        $Response,
        [int[]]$Expected,
        [int]$Code,
        [string]$Label
    )

    $actual = Get-ResponseStatusCode -Response $Response
    if ($Expected -notcontains $actual) {
        Stop-Smoke -Code $Code -Message "$Label devolvió HTTP $actual (esperado $($Expected -join '/'))."
    }
}

function Get-FinalUri {
    param($Response)

    if ($null -eq $Response) { return $null }

    $base = $null
    try { $base = $Response.BaseResponse } catch { $base = $null }
    if ($null -eq $base) { return $null }

    try {
        if ($base.PSObject.Properties['RequestMessage'] -and
            $null -ne $base.RequestMessage -and
            $null -ne $base.RequestMessage.RequestUri) {
            return $base.RequestMessage.RequestUri
        }
    }
    catch {
        # HttpResponseMessage no disponible.
    }

    try {
        if ($base.PSObject.Properties['ResponseUri'] -and $null -ne $base.ResponseUri) {
            return $base.ResponseUri
        }
    }
    catch {
        # HttpWebResponse no disponible.
    }

    return $null
}

function Get-SetCookieHeader {
    param(
        $Response,
        [Parameter(Mandatory = $true)][string]$Name
    )

    $attributePattern = "(?i)^" + [regex]::Escape($Name) + "="

    foreach ($header in (Get-HeaderValues -Response $Response -Name 'Set-Cookie')) {
        # Windows PowerShell 5.1 puede devolver varias cookies Set-Cookie en un
        # solo valor separado por comas. Se separan solo cuando el fragmento
        # siguiente parece otro par nombre=valor (no una fecha de Expires).
        foreach ($candidate in [regex]::Split([string]$header, ',\s*(?=[A-Za-z0-9_-]+=)')) {
            if ($candidate -match $attributePattern) {
                return $candidate
            }
        }
    }

    return $null
}

function Get-CookieAttributeValue {
    param(
        [string]$Header,
        [Parameter(Mandatory = $true)][string]$Name
    )

    if ([string]::IsNullOrWhiteSpace($Header)) { return $null }

    $pattern = "(?i)(^|;)\s*" + [regex]::Escape($Name) + "\s*=\s*([^;]+)"
    if ($Header -match $pattern) {
        return $Matches[2].Trim()
    }

    return $null
}

function Get-SessionCookie {
    param(
        [Microsoft.PowerShell.Commands.WebRequestSession]$Session,
        [Parameter(Mandatory = $true)][string]$Name
    )

    if ($null -eq $Session) { return $null }

    try {
        $cookies = $Session.Cookies.GetCookies([Uri]$ApiBaseUrl)
        return ($cookies | Where-Object { $_.Name -eq $Name } | Select-Object -First 1)
    }
    catch {
        return $null
    }
}

function Assert-Cookie {
    param(
        [string]$Header,
        $Cookie,
        [string]$Name,
        [int]$Code,
        [bool]$RequireHttpOnly,
        [bool]$RequireSecure,
        [string]$ExpectedSameSite,
        [string]$ExpectedDomain
    )

    if ($null -eq $Header -or $null -eq $Cookie) {
        Stop-Smoke -Code $Code -Message "La cookie '$Name' no se emitió o el navegador no la almacenó."
    }

    if ([string]::IsNullOrWhiteSpace($Cookie.Value)) {
        Stop-Smoke -Code $Code -Message "La cookie '$Name' está vacía."
    }

    if ($Cookie.HttpOnly -ne $RequireHttpOnly) {
        $expected = if ($RequireHttpOnly) { 'HttpOnly' } else { 'legible por JavaScript' }
        Stop-Smoke -Code $Code -Message "La cookie '$Name' no es $expected."
    }

    if ($RequireSecure -and -not $Cookie.Secure) {
        Stop-Smoke -Code $Code -Message "La cookie '$Name' no tiene Secure sobre HTTPS."
    }

    $sameSite = Get-CookieAttributeValue -Header $Header -Name 'SameSite'
    if ([string]::IsNullOrWhiteSpace($sameSite)) {
        Stop-Smoke -Code $Code -Message "La cookie '$Name' no incluye SameSite."
    }
    if (-not [string]::Equals($sameSite, $ExpectedSameSite, [StringComparison]::OrdinalIgnoreCase)) {
        Stop-Smoke -Code $Code -Message "La cookie '$Name' tiene SameSite=$sameSite (esperado $ExpectedSameSite)."
    }

    if (-not [string]::IsNullOrWhiteSpace($ExpectedDomain)) {
        $domain = Get-CookieAttributeValue -Header $Header -Name 'Domain'
        if ([string]::IsNullOrWhiteSpace($domain)) {
            Stop-Smoke -Code $Code -Message "La cookie '$Name' no incluye Domain (esperado $ExpectedDomain)."
        }

        $normalizedDomain = $domain.Trim().Trim('.')
        $normalizedExpected = $ExpectedDomain.Trim().Trim('.')
        if (-not [string]::Equals($normalizedDomain, $normalizedExpected, [StringComparison]::OrdinalIgnoreCase)) {
            Stop-Smoke -Code $Code -Message "La cookie '$Name' tiene Domain=$domain (esperado $ExpectedDomain)."
        }
    }
}

try {
    if ([string]::IsNullOrWhiteSpace($ApiBaseUrl)) {
        Stop-Smoke -Code 2 -Message "Define -ApiBaseUrl o la variable API_BASE_URL."
    }
    if ([string]::IsNullOrWhiteSpace($Email)) {
        Stop-Smoke -Code 2 -Message "Define -Email o la variable PROYECTOCAFE_SMOKE_EMAIL."
    }
    if ([string]::IsNullOrWhiteSpace($Password)) {
        Stop-Smoke -Code 2 -Message "Define -Password o la variable PROYECTOCAFE_SMOKE_PASSWORD."
    }
    if (-not $SkipPortal -and [string]::IsNullOrWhiteSpace($PortalBaseUrl)) {
        Stop-Smoke -Code 2 -Message "Define -PortalBaseUrl o la variable PORTAL_BASE_URL (o usa -SkipPortal)."
    }
    if ($TimeoutSec -lt 1) {
        Stop-Smoke -Code 2 -Message "-TimeoutSec debe ser mayor que 0."
    }

    $ApiBaseUrl = $ApiBaseUrl.Trim().TrimEnd('/')
    if ($ApiBaseUrl -notmatch '^https?://[^/]+$') {
        Stop-Smoke -Code 2 -Message "-ApiBaseUrl debe ser una URL absoluta http/https sin ruta ni barra final."
    }

    if (-not $SkipPortal) {
        $PortalBaseUrl = $PortalBaseUrl.Trim().TrimEnd('/')
        if ($PortalBaseUrl -notmatch '^https?://[^/]+$') {
            Stop-Smoke -Code 2 -Message "-PortalBaseUrl debe ser una URL absoluta http/https sin ruta ni barra final."
        }
    }

    $ExpectedCookieDomain = if ([string]::IsNullOrWhiteSpace($ExpectedCookieDomain)) {
        $null
    }
    else {
        $ExpectedCookieDomain.Trim().Trim('.')
    }

    $expectSecure = $ApiBaseUrl.StartsWith('https://', [StringComparison]::OrdinalIgnoreCase)

    Write-Step "Versión de PowerShell: $($PSVersionTable.PSVersion)"
    Write-Host "    API:    $ApiBaseUrl"
    if (-not $SkipPortal) { Write-Host "    Portal: $PortalBaseUrl" }

    Write-Step "GET $ApiBaseUrl/health"
    $health = Invoke-SmokeRequest -Method Get -Url "$ApiBaseUrl/health"
    Assert-Status -Response $health -Expected @(200) -Code 3 -Label '/health'
    Write-Host "    OK (200)"

    if (-not $SkipPortal) {
        Write-Step "Preflight CORS desde $PortalBaseUrl"
        $preflight = Invoke-SmokeRequest -Method Options `
            -Url "$ApiBaseUrl/api/v1/auth/login" `
            -Headers @{
                Origin                         = $PortalBaseUrl
                'Access-Control-Request-Method'  = 'POST'
                'Access-Control-Request-Headers' = 'content-type'
            }

        Assert-Status -Response $preflight -Expected @(200, 204) -Code 4 -Label 'Preflight CORS'

        $allowOrigin = Get-HeaderValues -Response $preflight -Name 'Access-Control-Allow-Origin' |
            Select-Object -First 1
        if ([string]::IsNullOrWhiteSpace($allowOrigin)) {
            Stop-Smoke -Code 4 -Message 'El preflight no devolvió Access-Control-Allow-Origin. Revisa Cors__AllowedOrigins__0.'
        }
        if (-not [string]::Equals($allowOrigin, $PortalBaseUrl, [StringComparison]::Ordinal)) {
            Stop-Smoke -Code 4 -Message "CORS devolvió Allow-Origin=$allowOrigin (esperado $PortalBaseUrl)."
        }

        $allowCredentials = Get-HeaderValues -Response $preflight -Name 'Access-Control-Allow-Credentials' |
            Select-Object -First 1
        if (-not [string]::Equals($allowCredentials, 'true', [StringComparison]::OrdinalIgnoreCase)) {
            Stop-Smoke -Code 4 -Message 'El preflight no devolvió Access-Control-Allow-Credentials: true.'
        }

        Write-Host "    OK (204/200, origen exacto y credenciales)"
    }

    Write-Step "POST $ApiBaseUrl/api/v1/auth/login"
    $session = New-Object Microsoft.PowerShell.Commands.WebRequestSession
    $loginHeaders = @{ Accept = 'application/json' }
    if (-not $SkipPortal) { $loginHeaders['Origin'] = $PortalBaseUrl }

    $loginBody = @{
        email    = $Email
        password = $Password
    } | ConvertTo-Json -Compress

    $login = Invoke-SmokeRequest -Method Post `
        -Url "$ApiBaseUrl/api/v1/auth/login" `
        -Headers $loginHeaders `
        -Body $loginBody `
        -Session $session

    Assert-Status -Response $login -Expected @(200) -Code 5 -Label 'Login'
    $Password = $null
    Write-Host "    OK (200)"

    if (-not $SkipPortal) {
        $loginAllowOrigin = Get-HeaderValues -Response $login -Name 'Access-Control-Allow-Origin' |
            Select-Object -First 1
        if (-not [string]::Equals($loginAllowOrigin, $PortalBaseUrl, [StringComparison]::Ordinal)) {
            Stop-Smoke -Code 4 -Message "El login no devolvió el origen CORS exacto (recibido: '$loginAllowOrigin')."
        }
    }

    Write-Step 'Cookies de sesión'
    $setAt = Get-SetCookieHeader -Response $login -Name 'pc_at'
    $setCsrf = Get-SetCookieHeader -Response $login -Name 'pc_csrf'
    $pcAt = Get-SessionCookie -Session $session -Name 'pc_at'
    $pcCsrf = Get-SessionCookie -Session $session -Name 'pc_csrf'

    Assert-Cookie -Header $setAt -Cookie $pcAt -Name 'pc_at' -Code 6 `
        -RequireHttpOnly $true -RequireSecure $expectSecure `
        -ExpectedSameSite $ExpectedCookieSameSite -ExpectedDomain $ExpectedCookieDomain

    Assert-Cookie -Header $setCsrf -Cookie $pcCsrf -Name 'pc_csrf' -Code 6 `
        -RequireHttpOnly $false -RequireSecure $expectSecure `
        -ExpectedSameSite $ExpectedCookieSameSite -ExpectedDomain $ExpectedCookieDomain

    $domainLabel = if ($ExpectedCookieDomain) { $ExpectedCookieDomain } else { '(sin validar)' }
    Write-Host "    OK (pc_at HttpOnly; pc_csrf legible; Secure=$expectSecure; SameSite=$ExpectedCookieSameSite; Domain=$domainLabel)"

    Write-Step "GET $ApiBaseUrl/api/v1/auth/me"
    $me = Invoke-SmokeRequest -Method Get -Url "$ApiBaseUrl/api/v1/auth/me" -Session $session
    Assert-Status -Response $me -Expected @(200) -Code 7 -Label '/api/v1/auth/me'

    try {
        $meData = $me.Content | ConvertFrom-Json
    }
    catch {
        Stop-Smoke -Code 7 -Message '/api/v1/auth/me no devolvió JSON válido.'
    }

    if ($null -eq $meData -or
        $null -eq $meData.PSObject.Properties['email'] -or
        [string]::IsNullOrWhiteSpace([string]$meData.email)) {
        Stop-Smoke -Code 7 -Message '/api/v1/auth/me no incluyó el email del usuario.'
    }
    if (-not [string]::Equals([string]$meData.email, $Email, [StringComparison]::OrdinalIgnoreCase)) {
        Stop-Smoke -Code 7 -Message "/api/v1/auth/me devolvió un usuario distinto de $Email."
    }
    Write-Host "    OK (200, usuario esperado)"

    Write-Step 'CSRF: refresh sin y con X-CSRF'
    $refreshWithoutCsrf = Invoke-SmokeRequest -Method Post `
        -Url "$ApiBaseUrl/api/v1/auth/refresh" `
        -Session $session
    Assert-Status -Response $refreshWithoutCsrf -Expected @(403) -Code 8 -Label 'Refresh sin X-CSRF'

    $csrf = $pcCsrf.Value
    $refreshWithCsrf = Invoke-SmokeRequest -Method Post `
        -Url "$ApiBaseUrl/api/v1/auth/refresh" `
        -Headers @{ 'X-CSRF' = $csrf; Accept = 'application/json' } `
        -Session $session
    Assert-Status -Response $refreshWithCsrf -Expected @(200) -Code 8 -Label 'Refresh con X-CSRF'
    Write-Host "    OK (sin cabecera 403; con cabecera 200)"

    Write-Step "GET $ApiBaseUrl/api/v1/admin/users"
    $admin = Invoke-SmokeRequest -Method Get -Url "$ApiBaseUrl/api/v1/admin/users" -Session $session
    if ((Get-ResponseStatusCode -Response $admin) -eq 403) {
        Stop-Smoke -Code 9 -Message 'El usuario de smoke no tiene permiso Administracion.Usuarios:Ver.'
    }
    Assert-Status -Response $admin -Expected @(200) -Code 9 -Label '/api/v1/admin/users'
    Write-Host "    OK (200)"

    Write-Step 'Ventas'
    foreach ($path in @('/api/v1/ventas/ventas', '/api/v1/ventas/precios', '/api/v1/ventas/clientes')) {
        $response = Invoke-SmokeRequest -Method Get -Url "$ApiBaseUrl$path" -Session $session
        if ((Get-ResponseStatusCode -Response $response) -eq 403) {
            Stop-Smoke -Code 10 -Message "El usuario de smoke no tiene permiso de lectura para $path."
        }
        Assert-Status -Response $response -Expected @(200) -Code 10 -Label $path
        Write-Host "    OK $path"
    }

    Write-Step 'Inventario'
    foreach ($path in @('/api/v1/inventario/lotes', '/api/v1/inventario/tipos-cafe', '/api/v1/inventario/presentaciones')) {
        $response = Invoke-SmokeRequest -Method Get -Url "$ApiBaseUrl$path" -Session $session
        if ((Get-ResponseStatusCode -Response $response) -eq 403) {
            Stop-Smoke -Code 11 -Message "El usuario de smoke no tiene permiso de lectura para $path."
        }
        Assert-Status -Response $response -Expected @(200) -Code 11 -Label $path
        Write-Host "    OK $path"
    }

    if (-not $SkipPortal) {
        Write-Step 'Portal: páginas públicas y protegidas'
        $portalLogin = Invoke-SmokeRequest -Method Get -Url "$PortalBaseUrl/login"
        Assert-Status -Response $portalLogin -Expected @(200) -Code 12 -Label '/login del Portal'

        $portalPaths = @(
            '/dashboard',
            '/administracion/usuarios',
            '/dashboard/ventas',
            '/inventario/CafeTostado'
        )

        foreach ($path in $portalPaths) {
            $page = Invoke-SmokeRequest -Method Get -Url "$PortalBaseUrl$path" -Session $session
            Assert-Status -Response $page -Expected @(200) -Code 12 -Label "$path del Portal"

            $finalUri = Get-FinalUri -Response $page
            if ($null -ne $finalUri) {
                $finalPath = $finalUri.AbsolutePath
                if ($finalPath -eq '/login' -or $finalPath.StartsWith('/login?', [StringComparison]::Ordinal)) {
                    Stop-Smoke -Code 12 -Message "$path redirigió a /login. Revisa Auth__Cookie__Domain y la sesión del Portal."
                }
                if ($finalPath -eq '/acceso-denegado' -or $finalPath.StartsWith('/acceso-denegado?', [StringComparison]::Ordinal)) {
                    Stop-Smoke -Code 12 -Message "$path redirigió a /acceso-denegado. El usuario de smoke no tiene el permiso de esa pantalla."
                }
            }

            Write-Host "    OK $path"
        }
    }

    Write-Host ''
    Write-Host 'SMOKE TEST OK'
    Write-Host "  API:    $ApiBaseUrl"
    if (-not $SkipPortal) { Write-Host "  Portal: $PortalBaseUrl" }
    Write-Host "  Cookies: pc_at (HttpOnly, SameSite=$ExpectedCookieSameSite), pc_csrf (legible), CSRF validado."
    exit 0
}
catch {
    Stop-Smoke -Code 13 -Message "Error inesperado: $($_.Exception.Message)"
}
