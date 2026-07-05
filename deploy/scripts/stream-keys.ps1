#requires -Version 5.1
# Generates the RS256 key pair used to sign (API) and verify (Gateway) stream tickets.
# No-op if both keys already exist.
[CmdletBinding()]
param([Parameter(Mandatory)][string]$KeysDir)

$ErrorActionPreference = "Stop"
$privateKey = Join-Path $KeysDir "stream_private.pem"
$publicKey  = Join-Path $KeysDir "stream_public.pem"

if ((Test-Path $privateKey) -and (Test-Path $publicKey)) {
    Write-Host "Stream-ticket keys already present in $KeysDir"
    return
}

if (-not (Test-Path $KeysDir)) { New-Item -ItemType Directory -Path $KeysDir | Out-Null }

if (Get-Command openssl -ErrorAction SilentlyContinue) {
    & openssl genpkey -algorithm RSA -pkeyopt rsa_keygen_bits:2048 -out $privateKey
    & openssl rsa -in $privateKey -pubout -out $publicKey
} else {
    $rsa = [System.Security.Cryptography.RSA]::Create(2048)
    [System.IO.File]::WriteAllText($privateKey, $rsa.ExportPkcs8PrivateKeyPem())
    [System.IO.File]::WriteAllText($publicKey,  $rsa.ExportSubjectPublicKeyInfoPem())
    $rsa.Dispose()
}

Write-Host "Generated stream-ticket keys in $KeysDir"
