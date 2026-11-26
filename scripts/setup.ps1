<#
  Sustain Monitor — first-time setup on Windows.
  Run from the repo root:  powershell -ExecutionPolicy Bypass -File scripts\setup.ps1
  Steps: check .NET 10, restore, build, test, set the platform admin in user-secrets,
  create the first EF Core migration, trust the dev HTTPS certificate.
#>
param(
    [string]$AdminEmail,
    [SecureString]$AdminPassword,
    [switch]$SkipMigration
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$api = Join-Path $root 'src\SustainMonitor.Api'
$infra = Join-Path $root 'src\SustainMonitor.Infrastructure'

function Step($text) { Write-Host "`n==> $text" -ForegroundColor Cyan }

Step 'Checking .NET SDK'
$sdk = (dotnet --version) 2>$null
if (-not $sdk -or -not $sdk.StartsWith('10.')) {
    throw ".NET 10 SDK is required (found '$sdk'). Install it from https://dotnet.microsoft.com/download/dotnet/10.0"
}
Write-Host "dotnet $sdk"

Step 'Restoring and building'
dotnet restore "$root\SustainMonitor.slnx"
dotnet build "$root\SustainMonitor.slnx" --no-restore -c Debug

Step 'Running tests'
dotnet test "$root\SustainMonitor.slnx" --no-build

Step 'Platform admin account'
if (-not $AdminEmail) { $AdminEmail = Read-Host 'Platform admin email' }
if (-not $AdminPassword) { $AdminPassword = Read-Host 'Platform admin password (10+ chars, upper, lower, digit)' -AsSecureString }
$plain = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($AdminPassword))
dotnet user-secrets set 'Seed:AdminEmail' $AdminEmail --project $api | Out-Null
dotnet user-secrets set 'Seed:AdminPassword' $plain --project $api | Out-Null
Write-Host "Saved to user-secrets (not in source control)."

if (-not $SkipMigration) {
    Step 'Creating the first EF Core migration'
    if (-not (dotnet tool list --global | Select-String 'dotnet-ef')) {
        dotnet tool install --global dotnet-ef
    }
    $migrations = Join-Path $infra 'Data\Migrations'
    if (Test-Path $migrations) {
        Write-Host 'Migrations already exist; skipping.'
    } else {
        Get-ChildItem $api -Filter 'sustainmonitor.db*' -ErrorAction SilentlyContinue | Remove-Item
        dotnet ef migrations add InitialCreate --project $infra --startup-project $api --output-dir Data/Migrations
    }
}

Step 'Trusting the HTTPS development certificate'
dotnet dev-certs https --trust

Step 'Done'
Write-Host "Start the API:  dotnet run --project src\SustainMonitor.Api --launch-profile https"
Write-Host "API docs:       https://localhost:7180/scalar"
Write-Host "Health:         https://localhost:7180/health"
Write-Host "Try the flow:   src\SustainMonitor.Api\SustainMonitor.Api.http"
