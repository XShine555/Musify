#requires -Version 5.1
<#
.SYNOPSIS
    Provisions Zitadel for Musify (idempotent).

.DESCRIPTION
    Creates the project and the OIDC app (PKCE, JWT access token) and wires the
    generated ClientId into Musify.Api user-secrets and deploy/.env (AUTH_CLIENT_ID).
    Uses the service-account PAT that Zitadel writes on init (zitadel/.output/admin-sa.pat).
    Re-running reuses the existing project/app and refreshes the wired values.
#>
[CmdletBinding()]
param(
    [string]$ProjectName = "Musify",
    [string]$AppName     = "Musify API (Scalar)"
)

$ErrorActionPreference = "Stop"
$deployDir = Split-Path $PSScriptRoot -Parent
$apiProject = Resolve-Path (Join-Path $deployDir "..\backend\Musify.Api")
$patFile    = Join-Path $PSScriptRoot ".output\admin-sa.pat"

. (Join-Path $deployDir "scripts\common.ps1")

$cfg     = Read-DotEnv (Join-Path $deployDir ".env")
$issuer  = "http://$($cfg['ZITADEL_EXTERNAL_DOMAIN']):$($cfg['ZITADEL_EXTERNAL_PORT'])"
$apiBase = "http://localhost:5111"
$scalarRedirect = "$apiBase/scalar/"

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

Write-Step "Waiting for Zitadel at $issuer"
$deadline = (Get-Date).AddMinutes(2)
do {
    try { Invoke-RestMethod "$issuer/.well-known/openid-configuration" -TimeoutSec 5 | Out-Null; $ready = $true }
    catch { $ready = $false; Start-Sleep 3 }
} until ($ready -or (Get-Date) -gt $deadline)
if (-not $ready) { throw "Zitadel did not respond in time." }

# Login UI (idempotent) ------------------------------------------------------
# The 'latest' Zitadel image requires the Login UI v2 (a separate 'login'
# container that this stack does not run), so OIDC would redirect to
# /ui/v2/login and 404. Disable the requirement to use the built-in v1 login.
Write-Step "Disabling required Login UI v2 (use built-in /ui/login)"
try {
    Invoke-RestMethod -Method PUT -Uri "$issuer/v2/features/instance" -Headers $headers `
        -ContentType "application/json" -Body (@{ loginV2 = @{ required = $false } } | ConvertTo-Json) -TimeoutSec 20 | Out-Null
    Write-Host "  loginV2.required = false"
} catch {
    Write-Host "  warning: could not update loginV2 feature: $($_.Exception.Message)"
}

# Project (idempotent) -------------------------------------------------------
Write-Step "Project '$ProjectName'"
$project = (Invoke-Zitadel POST "/projects/_search" @{
    queries = @(@{ nameQuery = @{ name = $ProjectName; method = "TEXT_QUERY_METHOD_EQUALS" } })
}).result | Where-Object { $_.name -eq $ProjectName } | Select-Object -First 1

if ($project) {
    $projectId = $project.id
    Write-Host "  reused (id $projectId)"
} else {
    $projectId = (Invoke-Zitadel POST "/projects" @{ name = $ProjectName }).id
    Write-Host "  created (id $projectId)"
}

# OIDC app (idempotent) ------------------------------------------------------
Write-Step "OIDC app '$AppName'"
$app = (Invoke-Zitadel POST "/projects/$projectId/apps/_search" @{
    queries = @(@{ nameQuery = @{ name = $AppName; method = "TEXT_QUERY_METHOD_EQUALS" } })
}).result | Where-Object { $_.name -eq $AppName } | Select-Object -First 1

if ($app) {
    $clientId = (Invoke-Zitadel GET "/projects/$projectId/apps/$($app.id)" $null).app.oidcConfig.clientId
    Write-Host "  reused (clientId $clientId)"
} else {
    $clientId = (Invoke-Zitadel POST "/projects/$projectId/apps/oidc" @{
        name                   = $AppName
        redirectUris           = @($scalarRedirect, "$apiBase/scalar/oauth2-redirect.html")
        postLogoutRedirectUris = @($scalarRedirect)
        responseTypes          = @("OIDC_RESPONSE_TYPE_CODE")
        grantTypes             = @("OIDC_GRANT_TYPE_AUTHORIZATION_CODE")
        appType                = "OIDC_APP_TYPE_USER_AGENT"
        authMethodType         = "OIDC_AUTH_METHOD_TYPE_NONE"
        version                = "OIDC_VERSION_1_0"
        devMode                = $true                   # allow http redirect URIs
        accessTokenType        = "OIDC_TOKEN_TYPE_JWT"   # required so JwtBearer can validate
        accessTokenRoleAssertion = $true
        idTokenRoleAssertion     = $true
        idTokenUserinfoAssertion = $true
    }).clientId
    Write-Host "  created (clientId $clientId)"
}

# Wire the config: user-secrets (local runs) + .env (docker runs) -------------
Write-Step "Wiring Authentication into Musify.Api user-secrets"
@{
    "Authentication:ClientId"              = $clientId
    "Authentication:AudienceAddress"       = $clientId
    "Authentication:IssuerAddress"         = $issuer
    "Authentication:MetadataAddress"       = "$issuer/.well-known/openid-configuration"
    "Authentication:AuthorizationEndpoint" = "$issuer/oauth/v2/authorize?prompt=login"
    "Authentication:TokenEndpoint"         = "$issuer/oauth/v2/token"
    "Authentication:ScalarRedirectUri"     = $scalarRedirect
    "Authentication:RequireHttpsMetadata"  = "false"
}.GetEnumerator() | ForEach-Object {
    dotnet user-secrets set $_.Key $_.Value --project $apiProject | Out-Null
}

$envPath  = Join-Path $deployDir ".env"
$envLines = Get-Content $envPath
if ($envLines -match '^\s*AUTH_CLIENT_ID=') {
    $envLines = $envLines -replace '^\s*AUTH_CLIENT_ID=.*', "AUTH_CLIENT_ID=$clientId"
} else {
    $envLines += "AUTH_CLIENT_ID=$clientId"
}
Set-Content -Path $envPath -Value $envLines -Encoding utf8

Write-Host "`nZitadel provisioned:" -ForegroundColor Green
Write-Host "  Project:  $ProjectName ($projectId)"
Write-Host "  ClientId: $clientId"
Write-Host "  Issuer:   $issuer"
