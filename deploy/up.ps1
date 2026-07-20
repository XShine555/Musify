#requires -Version 5.1
<#
.SYNOPSIS
    Brings up the Musify development environment.

.DESCRIPTION
    Starts the infrastructure stack, generates the stream-ticket keys, applies
    the EF Core migrations and provisions Zitadel (project + OIDC apps). The
    .NET apps and the web player then run from the IDE; add -Apps to run them
    in Docker instead.

.EXAMPLE
    ./up.ps1                    # Infrastructure only (apps from the IDE)
    ./up.ps1 -Apps              # Also build and run api, worker, gateway, web player
    ./up.ps1 -Tools             # Also start pgAdmin
    ./up.ps1 -SkipMigrations    # Skip the database migration step
    ./up.ps1 -Down              # Stop and remove containers (keep volumes)
    ./up.ps1 -Destroy           # Stop and remove containers + volumes (wipe data)
#>
[CmdletBinding()]
param(
    [switch]$Apps,
    [switch]$Tools,
    [switch]$SkipMigrations,
    [switch]$Down,
    [switch]$Destroy
)

$ErrorActionPreference = "Stop"
$deployDir = $PSScriptRoot
$compose   = @(
    "-f", (Join-Path $deployDir "compose.yml"),
    "-f", (Join-Path $deployDir "compose.dev.yml")
)
$profiles = @("--profile", "apps", "--profile", "tools")

. (Join-Path $deployDir "scripts\common.ps1")

# Teardown -------------------------------------------------------------------
if ($Down -or $Destroy) {
    if ($Destroy) {
        Write-Step "Stopping the stack and REMOVING volumes (data is wiped)"
        docker compose @compose @profiles down -v --remove-orphans
        # Drop the Zitadel init secrets so the next start re-initializes cleanly.
        Get-ChildItem (Join-Path $deployDir "zitadel\.output") -File -ErrorAction SilentlyContinue | Remove-Item -Force
    } else {
        Write-Step "Stopping the stack (volumes are kept)"
        docker compose @compose @profiles down --remove-orphans
    }
    return
}

# Config ---------------------------------------------------------------------
$envFile = Join-Path $deployDir ".env"
if (-not (Test-Path $envFile)) {
    Write-Step "Creating .env from .env.example"
    Copy-Item (Join-Path $deployDir ".env.example") $envFile
}
$cfg = Read-DotEnv $envFile

# Host directories the apps expect: RS256 keys (mounted into the api/gateway
# containers, read directly by the IDE runs) and the Worker scratch space.
& (Join-Path $deployDir "scripts\stream-keys.ps1") -KeysDir (Join-Path $deployDir "keys")
$tempDir = Join-Path $deployDir ".tmp"
if (-not (Test-Path $tempDir)) { New-Item -ItemType Directory -Path $tempDir | Out-Null }

# Infrastructure -------------------------------------------------------------
Write-Step "Starting the infrastructure stack"
$upArgs = @("up", "-d")
if ($Tools) { $upArgs = @("--profile", "tools") + $upArgs }
docker compose @compose @upArgs
if ($LASTEXITCODE -ne 0) { throw "docker compose up failed" }

Write-Step "Waiting for Postgres to become healthy"
$deadline = (Get-Date).AddMinutes(2)
do {
    Start-Sleep -Seconds 2
    $status = docker inspect --format '{{.State.Health.Status}}' musify-postgres 2>$null
} until ($status -eq "healthy" -or (Get-Date) -gt $deadline)
if ($status -ne "healthy") { throw "Postgres did not become healthy in time" }

# Database migrations --------------------------------------------------------
if ($SkipMigrations) {
    Write-Host "`nSkipping migrations (-SkipMigrations)."
} else {
    Write-Step "Applying EF Core migrations"
    $conn = "Host=localhost;Port=$($cfg['POSTGRES_HOST_PORT']);Database=$($cfg['POSTGRES_DB']);Username=$($cfg['POSTGRES_USER']);Password=$($cfg['POSTGRES_PASSWORD'])"
    & (Join-Path $deployDir "scripts\migrate.ps1") -ConnectionString $conn
}

# Zitadel provisioning -------------------------------------------------------
# Writes the client ids into .env, Musify.Api user-secrets and web-player/.env,
# so it has to run before the app containers start.
& (Join-Path $deployDir "zitadel\provision.ps1")

# Applications ---------------------------------------------------------------
if ($Apps) {
    Write-Step "Building and starting the applications"
    docker compose @compose --profile apps up -d --build
    if ($LASTEXITCODE -ne 0) { throw "docker compose up (apps) failed" }
}

# Summary --------------------------------------------------------------------
$cfg = Read-DotEnv $envFile
$authUrl = $cfg['PUBLIC_AUTH_URL']
$appLines = if ($Apps) {
@"
  Musify API          $($cfg['PUBLIC_API_URL'])/scalar/v1
  Streaming gateway   $($cfg['PUBLIC_STREAM_URL'])/health
  Web player          $($cfg['PUBLIC_WEB_URL'])
"@
} else {
@"
  Run the apps from the IDE (or ./up.ps1 -Apps to run them in Docker):
    dotnet run --project backend/Musify.Api               # :5111
    dotnet run --project backend/Musify.StreamingGateway  # :8081
    dotnet run --project backend/Musify.Worker            # background
    npm --prefix web-player run dev                       # :5173
"@
}

Write-Host @"

============================================================
  Musify development environment READY
============================================================

  Service             URL / port
  ------------------  --------------------------------------
  PostgreSQL          localhost:$($cfg['POSTGRES_HOST_PORT'])  (db: $($cfg['POSTGRES_DB']))
  Zitadel console     $authUrl/ui/console
  RabbitMQ mgmt       http://localhost:15672  ($($cfg['RABBITMQ_DEFAULT_USER'])/$($cfg['RABBITMQ_DEFAULT_PASS']))
  SeaweedFS S3        http://localhost:8333   (bucket: $($cfg['S3_BUCKET']))
  SeaweedFS filer     http://localhost:8888
  SeaweedFS master    http://localhost:9333
  Jaeger traces       http://localhost:16686  (OTLP: localhost:4317)
  pgAdmin (-Tools)    http://localhost:$($cfg['PGADMIN_HOST_PORT'])  ($($cfg['PGADMIN_DEFAULT_EMAIL'])/$($cfg['PGADMIN_DEFAULT_PASSWORD']))

  Zitadel admin       $($cfg['ZITADEL_ADMIN_USERNAME'])@zitadel.$($cfg['ZITADEL_EXTERNAL_DOMAIN']) / $($cfg['ZITADEL_ADMIN_PASSWORD'])
  API client id       $($cfg['AUTH_CLIENT_ID'])
  Web client id       $($cfg['WEB_CLIENT_ID'])

$appLines
============================================================
"@ -ForegroundColor Green
