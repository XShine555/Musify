#requires -Version 7.0
<#
.SYNOPSIS
    Provisions Zitadel for Musify in production (idempotent).

.DESCRIPTION
    Same idea as provision.ps1 but for the VPS deploy: creates the project, the
    API OIDC app (PKCE, JWT) and the Web OIDC app (confidential, JWT), then
    wires the values into deploy/.env.prod. Run from deploy/zitadel/ on the VPS
    after `docker compose ... up -d` has brought zitadel up.
#>
[CmdletBinding()]
param(
    [string]$ProjectName = "Musify",
    [string]$ApiAppName  = "Musify API",
    [string]$WebAppName  = "Musify Web"
)

$ErrorActionPreference = "Stop"
$deployDir = Split-Path $PSScriptRoot -Parent
$envPath   = Join-Path $deployDir ".env.prod"
$patFile   = Join-Path $PSScriptRoot ".output/admin-sa.pat"

function Read-DotEnv($path) {
    $map = @{}
    Get-Content $path | ForEach-Object {
        if ($_ -match '^\s*#' -or $_ -notmatch '=') { return }
        $k, $v = $_.Split('=', 2)
        $map[$k.Trim()] = $v.Trim()
    }
    return $map
}

$cfg    = Read-DotEnv $envPath
$issuer = "https://$($cfg['AUTH_DOMAIN'])"
$apiUrl = "https://$($cfg['API_DOMAIN'])"
$webUrl = "https://$($cfg['WEB_DOMAIN'])"

$deadline = (Get-Date).AddMinutes(3)
while (-not (Test-Path $patFile) -and (Get-Date) -lt $deadline) { Start-Sleep -Seconds 3 }
if (-not (Test-Path $patFile)) { throw "PAT not found ($patFile). Check 'docker compose logs zitadel'." }

$headers = @{ Authorization = "Bearer $((Get-Content $patFile -Raw).Trim())" }
$mgmt    = "$issuer/management/v1"

function Invoke-Zitadel($method, $path, $body) {
    $params = @{ Method = $method; Uri = "$mgmt$path"; Headers = $headers; ContentType = "application/json"; TimeoutSec = 20 }
    if ($body) { $params.Body = ($body | ConvertTo-Json -Depth 10) }
    Invoke-RestMethod @params -SkipCertificateCheck
}

Write-Host "Waiting for Zitadel at $issuer"
$deadline = (Get-Date).AddMinutes(2)
do {
    try { Invoke-RestMethod "$issuer/.well-known/openid-configuration" -TimeoutSec 5 -SkipCertificateCheck | Out-Null; $ready = $true }
    catch { $ready = $false; Start-Sleep 3 }
} until ($ready -or (Get-Date) -gt $deadline)
if (-not $ready) { throw "Zitadel did not respond in time." }

Write-Host "Disabling required Login UI v2 (use built-in /ui/login)"
try {
    Invoke-RestMethod -Method PUT -Uri "$issuer/v2/features/instance" -Headers $headers `
        -ContentType "application/json" -Body (@{ loginV2 = @{ required = $false } } | ConvertTo-Json) -TimeoutSec 20 -SkipCertificateCheck | Out-Null
} catch {
    Write-Host "  warning: could not update loginV2 feature: $($_.Exception.Message)"
}

Write-Host "Project '$ProjectName'"
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

Write-Host "OIDC app '$ApiAppName' (public, PKCE)"
$apiApp = (Invoke-Zitadel POST "/projects/$projectId/apps/_search" @{
    queries = @(@{ nameQuery = @{ name = $ApiAppName; method = "TEXT_QUERY_METHOD_EQUALS" } })
}).result | Where-Object { $_.name -eq $ApiAppName } | Select-Object -First 1

if ($apiApp) {
    $apiClientId = (Invoke-Zitadel GET "/projects/$projectId/apps/$($apiApp.id)" $null).app.oidcConfig.clientId
    Write-Host "  reused (clientId $apiClientId)"
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
        devMode                  = $false
        accessTokenType          = "OIDC_TOKEN_TYPE_JWT"
        accessTokenRoleAssertion = $true
        idTokenRoleAssertion     = $true
        idTokenUserinfoAssertion = $true
    }).clientId
    Write-Host "  created (clientId $apiClientId)"
}

Write-Host "OIDC app '$WebAppName' (confidential)"
$webApp = (Invoke-Zitadel POST "/projects/$projectId/apps/_search" @{
    queries = @(@{ nameQuery = @{ name = $WebAppName; method = "TEXT_QUERY_METHOD_EQUALS" } })
}).result | Where-Object { $_.name -eq $WebAppName } | Select-Object -First 1

$webClientSecret = $null
if ($webApp) {
    $webClientId = (Invoke-Zitadel GET "/projects/$projectId/apps/$($webApp.id)" $null).app.oidcConfig.clientId
    Write-Host "  reused (clientId $webClientId, secret NOT regenerated)"
} else {
    $created = Invoke-Zitadel POST "/projects/$projectId/apps/oidc" @{
        name                     = $WebAppName
        redirectUris             = @("$webUrl/auth/callback")
        postLogoutRedirectUris   = @("$webUrl/")
        responseTypes            = @("OIDC_RESPONSE_TYPE_CODE")
        grantTypes               = @("OIDC_GRANT_TYPE_AUTHORIZATION_CODE")
        appType                  = "OIDC_APP_TYPE_WEB"
        authMethodType           = "OIDC_AUTH_METHOD_TYPE_BASIC"
        version                  = "OIDC_VERSION_1_0"
        devMode                  = $false
        accessTokenType          = "OIDC_TOKEN_TYPE_JWT"
        accessTokenRoleAssertion = $true
        idTokenRoleAssertion     = $true
        idTokenUserinfoAssertion = $true
    }
    $webClientId     = $created.clientId
    $webClientSecret = $created.clientSecret
    Write-Host "  created (clientId $webClientId)"
}

$envLines = Get-Content $envPath
function Set-EnvValue([string[]]$lines, [string]$key, [string]$value) {
    if ($lines -match "^\s*$key=") {
        return $lines -replace "^\s*$key=.*", "$key=$value"
    }
    return $lines + "$key=$value"
}
$envLines = Set-EnvValue $envLines "AUTH_CLIENT_ID" $apiClientId
$envLines = Set-EnvValue $envLines "WEB_CLIENT_ID" $webClientId
if ($webClientSecret) {
    $envLines = Set-EnvValue $envLines "WEB_CLIENT_SECRET" $webClientSecret
}
Set-Content -Path $envPath -Value $envLines -Encoding utf8

Write-Host "`nZitadel provisioned:" -ForegroundColor Green
Write-Host "  Project:  $ProjectName ($projectId)"
Write-Host "  API app:  $apiClientId"
Write-Host "  Web app:  $webClientId"
Write-Host "  Issuer:   $issuer"
if ($webClientSecret) { Write-Host "  Web app secret written to .env.prod (WEB_CLIENT_SECRET)" }
