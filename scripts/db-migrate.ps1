#requires -Version 5.1
<#
.SYNOPSIS
    Aplica el script idempotente de migraciones de EF Core a SQL Server/Azure SQL.

.DESCRIPTION
    Lee la cadena de conexión de una variable de entorno (por defecto
    Database__ConnectionString, la misma que usa la API), ejecuta
    tools/sqlserver-migrate-current.sql y devuelve un código de salida claro.

    El script es idempotente: puede ejecutarse varias veces sobre la misma base
    sin duplicar tablas, índices ni filas en dbo.__EFMigrationsHistory.

    La cadena de conexión debe usar autenticación SQL (User ID/Password) o
    autenticación integrada de Windows. Para Microsoft Entra ID usa sqlcmd con
    --authentication-method y -i tools/sqlserver-migrate-current.sql.

.PARAMETER ConnectionStringVariable
    Nombre de la variable de entorno que contiene la cadena de conexión.
    Por defecto: Database__ConnectionString.

.PARAMETER ScriptPath
    Ruta al script SQL. Por defecto: <repo>/tools/sqlserver-migrate-current.sql.

.PARAMETER CreateDatabaseIfMissing
    Crea la base de datos si no existe. Útil en desarrollo con LocalDB o una
    instancia local. No lo uses en Azure SQL: la base de datos ya debe existir.

.EXAMPLE
    $env:Database__ConnectionString = "Server=(localdb)\MSSQLLocalDB;Database=ProyectoCafe.Dev;Trusted_Connection=True;TrustServerCertificate=True"
    pwsh -File .\scripts\db-migrate.ps1 -CreateDatabaseIfMissing

.EXAMPLE
    $env:Database__ConnectionString = $secretConnectionString
    pwsh -File .\scripts\db-migrate.ps1

.NOTES
    Códigos de salida:
      0 = script aplicado correctamente (o ya estaba al día)
      2 = variable de entorno ausente o cadena de conexión inválida
      3 = script SQL no encontrado o vacío
      4 = falta el proveedor System.Data.SqlClient
      5 = error al ejecutar SQL
      6 = error inesperado
#>
[CmdletBinding()]
param(
    [string]$ConnectionStringVariable = 'Database__ConnectionString',
    [string]$ScriptPath,
    [switch]$CreateDatabaseIfMissing
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Stop-WithCode {
    param(
        [Parameter(Mandatory = $true)][int]$Code,
        [Parameter(Mandatory = $true)][string]$Message
    )

    [Console]::Error.WriteLine($Message)
    exit $Code
}

if ([string]::IsNullOrWhiteSpace($ScriptPath)) {
    $repositoryRoot = Split-Path -Parent $PSScriptRoot
    $ScriptPath = Join-Path $repositoryRoot 'tools\sqlserver-migrate-current.sql'
}

$ScriptPath = [System.IO.Path]::GetFullPath($ScriptPath)

$connectionString = [Environment]::GetEnvironmentVariable($ConnectionStringVariable)
if ([string]::IsNullOrWhiteSpace($connectionString)) {
    Stop-WithCode -Code 2 -Message @"
La variable de entorno '$ConnectionStringVariable' no está definida.
Define la cadena de conexión (sin commitearla) y vuelve a ejecutar el script:

  `$env:$ConnectionStringVariable = "<cadena de conexión>"
  pwsh -File .\scripts\db-migrate.ps1
"@
}

try {
    Add-Type -AssemblyName System.Data -ErrorAction Stop
}
catch {
    Stop-WithCode -Code 4 -Message "No se pudo cargar System.Data.SqlClient: $($_.Exception.Message)"
}

if (-not ('System.Data.SqlClient.SqlConnection' -as [type])) {
    Stop-WithCode -Code 4 -Message "System.Data.SqlClient no está disponible en este PowerShell."
}

if (-not (Test-Path -LiteralPath $ScriptPath -PathType Leaf)) {
    Stop-WithCode -Code 3 -Message "No se encontró el script SQL: $ScriptPath"
}

try {
    $sql = Get-Content -LiteralPath $ScriptPath -Raw -Encoding UTF8
}
catch {
    Stop-WithCode -Code 3 -Message "No se pudo leer el script SQL '$ScriptPath': $($_.Exception.Message)"
}

if ([string]::IsNullOrWhiteSpace($sql)) {
    Stop-WithCode -Code 3 -Message "El script SQL está vacío: $ScriptPath"
}

try {
    $builder = [System.Data.SqlClient.SqlConnectionStringBuilder]::new($connectionString)
}
catch {
    if ($connectionString -match '(?i)(^|;)\s*(Authentication|Access Token)\s*=') {
        Stop-WithCode -Code 2 -Message @"
Esta cadena usa autenticación de Microsoft Entra ID, no soportada por
System.Data.SqlClient. Usa sqlcmd con el script idempotente:

  sqlcmd -S <servidor> -d <base> --authentication-method <método> -b -i "$ScriptPath"
"@
    }

    Stop-WithCode -Code 2 -Message "La cadena de conexión de '$ConnectionStringVariable' no es válida."
}

$server = $builder.DataSource
$database = $builder.InitialCatalog

if ([string]::IsNullOrWhiteSpace($server) -or [string]::IsNullOrWhiteSpace($database)) {
    Stop-WithCode -Code 2 -Message "La cadena de conexión debe incluir 'Data Source' y 'Initial Catalog'."
}

if ($CreateDatabaseIfMissing) {
    $masterBuilder = [System.Data.SqlClient.SqlConnectionStringBuilder]::new($connectionString)
    $masterBuilder['Initial Catalog'] = 'master'

    $masterConnection = New-Object System.Data.SqlClient.SqlConnection
    $masterConnection.ConnectionString = $masterBuilder.ConnectionString

    $databaseLiteral = $database.Replace("'", "''")
    $databaseIdentifier = $database.Replace(']', ']]')

    try {
        $masterConnection.Open()
        $createCommand = $masterConnection.CreateCommand()
        $createCommand.CommandText =
            "IF DB_ID(N'$databaseLiteral') IS NULL CREATE DATABASE [$databaseIdentifier];"
        $createCommand.CommandTimeout = 300
        [void]$createCommand.ExecuteNonQuery()
        $createCommand.Dispose()
    }
    catch {
        Stop-WithCode -Code 5 -Message "No se pudo crear/verificar la base '$database' en '$server': $($_.Exception.Message)"
    }
    finally {
        $masterConnection.Dispose()
    }
}

Write-Host "Aplicando script idempotente de migraciones..."
Write-Host "  Servidor: $server"
Write-Host "  Base de datos: $database"
Write-Host "  Script: $ScriptPath"

# EF Core separa los lotes con GO en su propia línea. No se usa sqlcmd para
# poder ejecutar el mismo archivo contra LocalDB y Azure SQL sin dependencias
# externas adicionales.
$batches = [regex]::Split($sql, '(?im)^[ \t]*GO[ \t]*(?:--[^\r\n]*)?[ \t]*\r?$')

$connection = New-Object System.Data.SqlClient.SqlConnection
$connection.ConnectionString = $connectionString

try {
    $connection.Open()
}
catch {
    Stop-WithCode -Code 5 -Message "No se pudo conectar con '$server/$database': $($_.Exception.Message)"
}

$batchNumber = 0
try {
    foreach ($batch in $batches) {
        if ([string]::IsNullOrWhiteSpace($batch)) {
            continue
        }

        $batchNumber++
        $command = $connection.CreateCommand()
        $command.CommandText = $batch
        $command.CommandTimeout = 300

        try {
            [void]$command.ExecuteNonQuery()
        }
        catch [System.Data.SqlClient.SqlException] {
            Stop-WithCode -Code 5 -Message "Error SQL en el lote $batchNumber de $($batches.Count): $($_.Exception.Message)"
        }
        finally {
            $command.Dispose()
        }
    }
}
catch {
    Stop-WithCode -Code 6 -Message "Error inesperado al aplicar el script: $($_.Exception.Message)"
}
finally {
    $connection.Dispose()
}

Write-Host "Esquema aplicado correctamente. El script puede ejecutarse de nuevo sin cambios."
exit 0
