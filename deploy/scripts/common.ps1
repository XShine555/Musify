#requires -Version 5.1
# Shared helpers for the development deploy scripts.

function Write-Step([string]$Message) { Write-Host "`n==> $Message" -ForegroundColor Cyan }

# Parses a .env file into an ordered hashtable (ignores comments and blank lines).
function Read-DotEnv([string]$Path) {
    $values = [ordered]@{}
    if (Test-Path $Path) {
        Get-Content $Path | ForEach-Object {
            if ($_ -match '^\s*([^#=\s][^=]*)=(.*)$') { $values[$matches[1].Trim()] = $matches[2].Trim() }
        }
    }
    return $values
}

# Sets (or appends) a key in a .env file, preserving the rest of the file.
function Set-DotEnvValue([string]$Path, [string]$Key, [string]$Value) {
    $lines = @(Get-Content $Path)
    if ($lines -match "^\s*$Key=") {
        $lines = $lines -replace "^\s*$Key=.*", "$Key=$Value"
    } else {
        $lines += "$Key=$Value"
    }
    Set-Content -Path $Path -Value $lines -Encoding utf8
}
