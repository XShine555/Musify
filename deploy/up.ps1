#requires -Version 5.1
<#
.SYNOPSIS
    Brings up the Musify development environment.

.DESCRIPTION
    Starts the infrastructure stack, generates the stream-ticket keys, applies EF
    Core migrations and provisions Zitadel (project + OIDC app + ClientId).

.EXAMPLE
    ./up.ps1                    # Bring everything up
    ./up.ps1 -SkipMigrations    # Skip the database migration step
    ./up.ps1 -Down              # Stop and remove containers (keep volumes)
    ./up.ps1 -Destroy           # Stop and remove containers + volumes (wipe data)
#>
[CmdletBinding()]
param(
    [switch]$Down,
    [switch]$Destroy,
    [switch]$SkipMigrations
)

$ErrorActionPreference = "Stop"
$deployDir   = $PSScriptRoot
$composeFile = Join-Path $deployDir "docker-compose.yml"

. (Join-Path $deployDir "scripts\common.ps1")

# Teardown -------------------------------------------------------------------
if ($Down -or $Destroy) {
    if ($Destroy) {
        Write-Step "Stopping the stack and REMOVING volumes (data is wiped)"
        docker compose -f $composeFile down -v --remove-orphans
        # Drop the Zitadel init secrets (PAT) so the next start re-initializes cleanly.
        Get-ChildItem (Join-Path $deployDir "zitadel\.output") -File -ErrorAction SilentlyContinue | Remove-Item -Force
    } else {
        Write-Step "Stopping the stack (volumes are kept)"
        docker compose -f $composeFile down --remove-orphans
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
$streamKeysDir = if ($cfg['STREAM_KEYS_DIR']) { $cfg['STREAM_KEYS_DIR'] } else { "D:/weed" }
$workerTempDir = if ($cfg['WORKER_TEMP_DIR']) { $cfg['WORKER_TEMP_DIR'] } else { "D:/tempsFilesDev" }

# Infrastructure -------------------------------------------------------------
Write-Step "Starting the infrastructure stack"
docker compose -f $composeFile up -d
if ($LASTEXITCODE -ne 0) { throw "docker compose up failed" }

Write-Step "Waiting for Postgres to become healthy"
$deadline = (Get-Date).AddMinutes(2)
do {
    Start-Sleep -Seconds 2
    $status = docker inspect --format '{{.State.Health.Status}}' musify-postgres 2>$null
} until ($status -eq "healthy" -or (Get-Date) -gt $deadline)
if ($status -ne "healthy") { throw "Postgres did not become healthy in time" }

# Stream-ticket keys ---------------------------------------------------------
Write-Step "Ensuring stream-ticket keys"
& (Join-Path $deployDir "scripts\stream-keys.ps1") -KeysDir $streamKeysDir

# Worker temp directory ------------------------------------------------------
if (-not (Test-Path $workerTempDir)) {
    Write-Step "Creating the Worker temp directory: $workerTempDir"
    New-Item -ItemType Directory -Path $workerTempDir | Out-Null
}

# Database migrations --------------------------------------------------------
if ($SkipMigrations) {
    Write-Host "`nSkipping migrations (-SkipMigrations)."
} else {
    Write-Step "Applying EF Core migrations"
    $conn = "Host=localhost;Port=$($cfg['POSTGRES_HOST_PORT']);Database=$($cfg['POSTGRES_DB']);Username=$($cfg['POSTGRES_USER']);Password=$($cfg['POSTGRES_PASSWORD'])"
    & (Join-Path $deployDir "scripts\migrate.ps1") -ConnectionString $conn
}

# Zitadel provisioning -------------------------------------------------------
Write-Step "Provisioning Zitadel (project + OIDC app + ClientId)"
try {
    & (Join-Path $deployDir "zitadel\provision.ps1")
} catch {
    Write-Host "Warning: Zitadel provisioning failed: $($_.Exception.Message)" -ForegroundColor Yellow
}

# Summary --------------------------------------------------------------------
$zDomain = $cfg['ZITADEL_EXTERNAL_DOMAIN']
$zPort   = $cfg['ZITADEL_EXTERNAL_PORT']
Write-Host @"

============================================================
  Musify development environment READY
============================================================

  Service             URL / port
  ------------------  --------------------------------------
  PostgreSQL          localhost:$($cfg['POSTGRES_HOST_PORT'])  (db: $($cfg['POSTGRES_DB']))
  pgAdmin             http://localhost:$($cfg['PGADMIN_HOST_PORT'])  ($($cfg['PGADMIN_DEFAULT_EMAIL'])/$($cfg['PGADMIN_DEFAULT_PASSWORD']))
  Zitadel console     http://$zDomain`:$zPort/ui/console
  RabbitMQ mgmt       http://localhost:15672  ($($cfg['RABBITMQ_DEFAULT_USER'])/$($cfg['RABBITMQ_DEFAULT_PASS']))
  SeaweedFS S3        http://localhost:8333   (bucket: $($cfg['S3_BUCKET']))
  SeaweedFS filer     http://localhost:8888
  SeaweedFS master    http://localhost:9333
  Jaeger traces       http://localhost:16686  (OTLP: localhost:4317)

  Zitadel admin: $($cfg['ZITADEL_ADMIN_USERNAME'])@zitadel.$zDomain / $($cfg['ZITADEL_ADMIN_PASSWORD'])
  Auth is provisioned automatically; ClientId is in Musify.Api user-secrets and .env.
  Log in via Scalar: http://localhost:5111/scalar/v1

  Run the apps from the IDE or:
    dotnet run --project backend/Musify.Api               # :5111
    dotnet run --project backend/Musify.StreamingGateway  # :8081
    dotnet run --project backend/Musify.Worker            # background
============================================================
"@ -ForegroundColor Green
