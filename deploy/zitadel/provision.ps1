#requires -Version 5.1
<#
.SYNOPSIS
    Provisions Zitadel for Musify in development (idempotent).

.DESCRIPTION
    Creates the Musify project, the API OIDC app (public, PKCE, JWT access
    token) and the web player OIDC app (confidential), then wires the resulting
    client ids into deploy/.env, Musify.Api user-secrets and web-player/.env.
    Authenticates with the service-account PAT that Zitadel writes on init
    (zitadel/.output/admin-sa.pat). Re-running reuses the existing project and
    apps and just refreshes the wired values.
#>
[CmdletBinding()]
param(
    [string]$ProjectName = "Musify",
    [string]$ApiAppName  = "Musify API (Scalar)",
    [string]$WebAppName  = "Musify Web",
    # Alternate .env to read/write (e.g. deploy/.env.share). Defaults to deploy/.env.
    [string]$EnvFile,
    # Skip writing Musify.Api user-secrets and web-player/.env: use when the apps
    # only ever run in Docker for this environment and get their config from compose.
    [switch]$SkipHostWiring,
    # Matches the zitadel volume mount for this environment (see compose.share.yml).
    [string]$OutputDir = ".output"
)

$ErrorActionPreference = "Stop"
$deployDir  = Split-Path $PSScriptRoot -Parent
$repoDir    = Split-Path $deployDir -Parent
$apiProject = Join-Path $repoDir "backend\Hosts\Musify.Api"
$webEnvFile = Join-Path $repoDir "web-player\.env"
$envFile    = if ($EnvFile) { $EnvFile } else { Join-Path $deployDir ".env" }
$patFile    = Join-Path $PSScriptRoot "$OutputDir\admin-sa.pat"

. (Join-Path $deployDir "scripts\common.ps1")

$cfg     = Read-DotEnv $envFile
$issuer  = $cfg['PUBLIC_AUTH_URL']
$apiUrl  = $cfg['PUBLIC_API_URL']
$webUrl  = $cfg['PUBLIC_WEB_URL']
$viteUrl = "http://localhost:5173"   # `npm run dev` in web-player/

Write-Step "Provisioning Zitadel at $issuer"

# Zitadel writes the PAT during init; on a cold start it may take a moment.
$deadline = (Get-Date).AddMinutes(3)
while (-not (Test-Path $patFile) -and (Get-Date) -lt $deadline) { Start-Sleep -Seconds 3 }
if (-not (Test-Path $patFile)) { throw "PAT not found ($patFile). Check 'docker logs musify-zitadel'." }

$headers = @{ Authorization = "Bearer $((Get-Content $patFile -Raw).Trim())" }
$mgmt    = "$issuer/management/v1"

function Invoke-Zitadel($method, $path, $body) {
    $params = @{ Method = $method; Uri = "$mgmt$path"; Headers = $headers; ContentType = "application/json"; TimeoutSec = 20 }
    if ($body) { $params.Body = ($body | ConvertTo-Json -Depth 10) }
    Invoke-RestMethod @params
}

$deadline = (Get-Date).AddMinutes(2)
do {
    try { Invoke-RestMethod "$issuer/.well-known/openid-configuration" -TimeoutSec 5 | Out-Null; $ready = $true }
    catch { $ready = $false; Start-Sleep 3 }
} until ($ready -or (Get-Date) -gt $deadline)
if (-not $ready) { throw "Zitadel did not respond in time." }

# The images tagged `latest` require the standalone Login UI v2 container, which
# this stack does not run, so OIDC would redirect to /ui/v2/login and 404.
# Opting out keeps the built-in v1 login.
try {
    Invoke-RestMethod -Method PUT -Uri "$issuer/v2/features/instance" -Headers $headers `
        -ContentType "application/json" -Body (@{ loginV2 = @{ required = $false } } | ConvertTo-Json) -TimeoutSec 20 | Out-Null
} catch {
    Write-Host "  warning: could not disable the required Login UI v2: $($_.Exception.Message)"
}

# Project --------------------------------------------------------------------
$project = (Invoke-Zitadel POST "/projects/_search" @{
    queries = @(@{ nameQuery = @{ name = $ProjectName; method = "TEXT_QUERY_METHOD_EQUALS" } })
}).result | Where-Object { $_.name -eq $ProjectName } | Select-Object -First 1

if ($project) {
    $projectId = $project.id
    Write-Host "  project '$ProjectName' reused (id $projectId)"
} else {
    $projectId = (Invoke-Zitadel POST "/projects" @{ name = $ProjectName }).id
    Write-Host "  project '$ProjectName' created (id $projectId)"
}

function Get-ZitadelApp($name) {
    (Invoke-Zitadel POST "/projects/$projectId/apps/_search" @{
        queries = @(@{ nameQuery = @{ name = $name; method = "TEXT_QUERY_METHOD_EQUALS" } })
    }).result | Where-Object { $_.name -eq $name } | Select-Object -First 1
}

# API app: public client used by the Scalar UI to obtain a token -------------
$apiApp = Get-ZitadelApp $ApiAppName
if ($apiApp) {
    $apiClientId = (Invoke-Zitadel GET "/projects/$projectId/apps/$($apiApp.id)" $null).app.oidcConfig.clientId
    Write-Host "  app '$ApiAppName' reused (clientId $apiClientId)"
} else {
    $apiClientId = (Invoke-Zitadel POST "/projects/$projectId/apps/oidc" @{
        name                     = $ApiAppName
        redirectUris             = @("$apiUrl/scalar/", "$apiUrl/scalar/oauth2-redirect.html")
        postLogoutRedirectUris   = @("$apiUrl/scalar/")
        responseTypes            = @("OIDC_RESPONSE_TYPE_CODE")
        grantTypes               = @("OIDC_GRANT_TYPE_AUTHORIZATION_CODE")
        appType                  = "OIDC_APP_TYPE_USER_AGENT"
        authMethodType           = "OIDC_AUTH_METHOD_TYPE_NONE"
        version                  = "OIDC_VERSION_1_0"
        devMode                  = $true                  # allow http redirect URIs
        accessTokenType          = "OIDC_TOKEN_TYPE_JWT"  # required so JwtBearer can validate it
        accessTokenRoleAssertion = $true
        idTokenRoleAssertion     = $true
        idTokenUserinfoAssertion = $true
    }).clientId
    Write-Host "  app '$ApiAppName' created (clientId $apiClientId)"
}

# Web app: confidential client used by the SvelteKit server ------------------
$webApp = Get-ZitadelApp $WebAppName
$webSecret = $cfg['WEB_CLIENT_SECRET']
if ($webApp) {
    $webClientId = (Invoke-Zitadel GET "/projects/$projectId/apps/$($webApp.id)" $null).app.oidcConfig.clientId
    # The secret is only returned once, so regenerate it if we no longer have it.
    if (-not $webSecret) {
        $webSecret = (Invoke-Zitadel POST "/projects/$projectId/apps/$($webApp.id)/oidc_config/_generate_client_secret" @{}).clientSecret
        Write-Host "  app '$WebAppName' reused (clientId $webClientId, secret regenerated)"
    } else {
        Write-Host "  app '$WebAppName' reused (clientId $webClientId)"
    }
} else {
    $created = Invoke-Zitadel POST "/projects/$projectId/apps/oidc" @{
        name                     = $WebAppName
        redirectUris             = @("$webUrl/auth/callback", "$viteUrl/auth/callback")
        postLogoutRedirectUris   = @("$webUrl/", "$viteUrl/")
        responseTypes            = @("OIDC_RESPONSE_TYPE_CODE")
        grantTypes               = @("OIDC_GRANT_TYPE_AUTHORIZATION_CODE", "OIDC_GRANT_TYPE_REFRESH_TOKEN")
        appType                  = "OIDC_APP_TYPE_WEB"
        authMethodType           = "OIDC_AUTH_METHOD_TYPE_BASIC"
        version                  = "OIDC_VERSION_1_0"
        devMode                  = $true
        accessTokenType          = "OIDC_TOKEN_TYPE_JWT"
        accessTokenRoleAssertion = $true
        idTokenRoleAssertion     = $true
        idTokenUserinfoAssertion = $true
    }
    $webClientId = $created.clientId
    $webSecret   = $created.clientSecret
    Write-Host "  app '$WebAppName' created (clientId $webClientId)"
}

# Wire the values ------------------------------------------------------------
Set-DotEnvValue $envFile "AUTH_CLIENT_ID"     $apiClientId
Set-DotEnvValue $envFile "WEB_CLIENT_ID"      $webClientId
Set-DotEnvValue $envFile "WEB_CLIENT_SECRET"  $webSecret

if ($SkipHostWiring) {
    Write-Host "  wired into $envFile only (-SkipHostWiring: apps run in Docker)"
} else {
    # Musify.Api reads these from user-secrets when it runs outside Docker.
    @{
        "Authentication:ClientId"              = $apiClientId
        "Authentication:AudienceAddress"       = $apiClientId
        "Authentication:IssuerAddress"         = $issuer
        "Authentication:MetadataAddress"       = "$issuer/.well-known/openid-configuration"
        "Authentication:AuthorizationEndpoint" = "$issuer/oauth/v2/authorize?prompt=login"
        "Authentication:TokenEndpoint"         = "$issuer/oauth/v2/token"
        "Authentication:ScalarRedirectUri"     = "$apiUrl/scalar/"
        "Authentication:RequireHttpsMetadata"  = "false"
    }.GetEnumerator() | ForEach-Object {
        dotnet user-secrets set $_.Key $_.Value --project $apiProject | Out-Null
    }

    # web-player/.env is what `npm run dev` reads (the container gets its config
    # from compose instead).
    if (-not (Test-Path $webEnvFile)) {
        Copy-Item (Join-Path $repoDir "web-player\.env.example") $webEnvFile
    }
    @{
        ZITADEL_ISSUER        = $issuer
        ZITADEL_CLIENT_ID     = $webClientId
        ZITADEL_CLIENT_SECRET = $webSecret
        AUTH_REDIRECT_URI     = "$viteUrl/auth/callback"
        AUTH_POST_LOGOUT_URI  = "$viteUrl/"
        SESSION_SECRET        = $cfg['SESSION_SECRET']
        API_BASE_URL          = $apiUrl
    }.GetEnumerator() | ForEach-Object {
        Set-DotEnvValue $webEnvFile $_.Key $_.Value
    }

    Write-Host "  wired into deploy/.env, Musify.Api user-secrets and web-player/.env"
}
