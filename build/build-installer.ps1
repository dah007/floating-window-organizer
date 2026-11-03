<#
.SYNOPSIS
    Tests, publishes and packages Floating Window Organizer into a Setup.exe.

.DESCRIPTION
    1. Runs the unit tests (Release).
    2. Publishes the app self-contained for win-x64 to artifacts\publish\win-x64.
    3. Compiles installer\FloatingWindowOrganizer.iss with Inno Setup into artifacts\installer.

    Requires the .NET 10 SDK and Inno Setup 6.3+ (winget install JRSoftware.InnoSetup).

.PARAMETER Version
    Version to stamp on the app and installer. Defaults to <Version> in Directory.Build.props.

.EXAMPLE
    .\build\build-installer.ps1
.EXAMPLE
    .\build\build-installer.ps1 -Version 0.2.0
#>
[CmdletBinding()]
param(
    [string] $Version,
    [switch] $SkipTests
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$runtime = 'win-x64'

function Invoke-Native([string] $Description, [scriptblock] $Command) {
    Write-Host "==> $Description" -ForegroundColor Cyan
    & $Command
    if ($LASTEXITCODE -ne 0) { throw "$Description failed (exit code $LASTEXITCODE)." }
}

if (-not $Version) {
    [xml] $props = Get-Content (Join-Path $root 'Directory.Build.props')
    $Version = ($props.Project.PropertyGroup | Where-Object { $_.Version } | Select-Object -First 1).Version
}
if ($Version -notmatch '^\d+\.\d+\.\d+$') { throw "Version '$Version' must look like 1.2.3." }
Write-Host "Version $Version" -ForegroundColor Green

$isccOnPath = Get-Command 'ISCC.exe' -ErrorAction SilentlyContinue
$iscc = @(
    $(if ($isccOnPath) { $isccOnPath.Source })
    "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe"
    "$env:ProgramFiles\Inno Setup 6\ISCC.exe"
) | Where-Object { $_ -and (Test-Path $_) } | Select-Object -First 1
if (-not $iscc) { throw 'Inno Setup not found. Install it with: winget install JRSoftware.InnoSetup' }

$publishDir = Join-Path $root "artifacts\publish\$runtime"
if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }

if (-not $SkipTests) {
    Invoke-Native 'Running tests' {
        dotnet test (Join-Path $root 'tests\FloatingWindowOrganizer.Tests\FloatingWindowOrganizer.Tests.csproj') -c Release
    }
}

Invoke-Native 'Publishing app' {
    dotnet publish (Join-Path $root 'src\FloatingWindowOrganizer.App\FloatingWindowOrganizer.App.csproj') `
        -c Release -r $runtime --self-contained true "-p:Version=$Version" -o $publishDir
}

Invoke-Native 'Building installer' {
    & $iscc "/DAppVersion=$Version" "/DPublishDir=$publishDir" (Join-Path $root 'installer\FloatingWindowOrganizer.iss')
}

$setup = Join-Path $root "artifacts\installer\FloatingWindowOrganizer-Setup-$Version.exe"
Write-Host "Installer: $setup" -ForegroundColor Green
