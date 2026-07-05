#requires -Version 5.1
# Shared helpers for the deploy scripts.

function Write-Step([string]$Message) { Write-Host "`n==> $Message" -ForegroundColor Cyan }

# Parses a .env file into a hashtable (ignores comments and blank lines).
function Read-DotEnv([string]$Path) {
    $values = @{}
    if (Test-Path $Path) {
        Get-Content $Path | ForEach-Object {
            if ($_ -match '^\s*([^#=\s][^=]*)=(.*)$') { $values[$matches[1].Trim()] = $matches[2].Trim() }
        }
    }
    return $values
}
