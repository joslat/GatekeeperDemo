[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string] $Configuration = "Release",

    [switch] $NoRestore
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repositoryRoot = $PSScriptRoot
$solutionPath = Join-Path $repositoryRoot "GatekeeperDemo.slnx"
$applicationProject = Join-Path $repositoryRoot "src\GatekeeperDemo.App\GatekeeperDemo.App.csproj"

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw "The .NET SDK was not found. Install the .NET 10 SDK from https://dotnet.microsoft.com/download/dotnet/10.0"
}

Push-Location $repositoryRoot
try {
    Write-Host ""
    Write-Host "Gatekeeper PartnerDesk Control Room" -ForegroundColor Cyan
    Write-Host "Configuration: $Configuration"
    Write-Host "The GUI defaults to the deterministic offline model; Azure OpenAI can be selected in the app."
    Write-Host ""

    if (-not $NoRestore) {
        Write-Host "Restoring packages..." -ForegroundColor DarkCyan
        & dotnet restore $solutionPath
        if ($LASTEXITCODE -ne 0) {
            throw "Package restore failed with exit code $LASTEXITCODE."
        }
    }

    Write-Host "Launching the desktop application..." -ForegroundColor Green
    & dotnet run --project $applicationProject --configuration $Configuration --no-restore
    if ($LASTEXITCODE -ne 0) {
        throw "The application exited with code $LASTEXITCODE."
    }
}
finally {
    Pop-Location
}
