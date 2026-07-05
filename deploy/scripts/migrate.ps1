#requires -Version 5.1
# Applies EF Core migrations to the Musify database.
[CmdletBinding()]
param([Parameter(Mandatory)][string]$ConnectionString)

$ErrorActionPreference = "Stop"
$infraProject = Resolve-Path (Join-Path $PSScriptRoot "..\..\backend\Musify.Infrastructure")

# The design-time factory reads the connection string from user-secrets.
dotnet user-secrets set "Database:ConnectionString" $ConnectionString --project $infraProject | Out-Null

# Infrastructure is also the startup project: it owns the IDesignTimeDbContextFactory
# and the EFCore.Design package (Musify.Api does not reference Design).
dotnet ef database update --project $infraProject --startup-project $infraProject --context Database
if ($LASTEXITCODE -ne 0) { throw "EF Core migration failed" }
