#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Build and package Essential.OpenTelemetry NuGet packages
.DESCRIPTION
    This script builds and packages the Essential.OpenTelemetry library for NuGet.
    It uses GitVersion for automatic semantic versioning based on Git history.
.PARAMETER Configuration
    Build configuration (Debug or Release). Default is Release.
.PARAMETER SkipTests
    Skip running tests before packaging.
.EXAMPLE
    .\build.ps1
    Build and package with Release configuration
.EXAMPLE
    .\build.ps1 -Configuration Debug -SkipTests
    Build and package Debug configuration without running tests
#>

param(
    [string]$Configuration = "Release",
    [switch]$SkipTests
)

$ErrorActionPreference = "Stop"

Write-Host "Building NuGet Packages for Essential OpenTelemetry" -ForegroundColor Cyan
Write-Host ""

# Restore dotnet tools
Write-Host "Restoring dotnet tools..." -ForegroundColor Yellow
dotnet tool restore
if (!$?) { throw 'Tool restore failed' }

# Get version from GitVersion
Write-Host ""
Write-Host "Getting version from GitVersion..." -ForegroundColor Yellow
$json = (dotnet tool run dotnet-gitversion /output json 2>&1)
$gitVersionExitCode = $LASTEXITCODE

if ($gitVersionExitCode -ne 0) {
    Write-Host "GitVersion encountered an issue (this is normal for feature branches or repos without tags)" -ForegroundColor Yellow
    Write-Host "Using default version 0.1.0-dev" -ForegroundColor Yellow
    $v = @{
        SemVer = "0.1.0-dev"
        ShortSha = (git rev-parse --short HEAD)
        FullSemVer = "0.1.0-dev"
        AssemblySemVer = "0.1.0.0"
        AssemblySemFileVer = "0.1.0.0"
    }
} else {
    Write-Host $json
    $v = ($json | ConvertFrom-Json)
}

Write-Host "Building version $($v.SemVer)+$($v.ShortSha) (NuGet $($v.FullSemVer))" -ForegroundColor Green
Write-Host ""

# Build solution
Write-Host "Building solution..." -ForegroundColor Yellow
dotnet build (Join-Path $PSScriptRoot Essential.OpenTelemetry.slnx) -c $Configuration
if (!$?) { throw 'Build failed' }

# Run tests (unless skipped)
if (!$SkipTests) {
    Write-Host ""
    Write-Host "Running tests..." -ForegroundColor Yellow
    dotnet test (Join-Path $PSScriptRoot Essential.OpenTelemetry.slnx) -c $Configuration --no-build
    if (!$?) { throw 'Tests failed' }
}

# Create pack output directory
$packDir = Join-Path $PSScriptRoot "pack"
if (Test-Path $packDir) {
    Write-Host ""
    Write-Host "Cleaning pack directory..." -ForegroundColor Yellow
    Remove-Item $packDir -Recurse -Force
}
New-Item -ItemType Directory -Path $packDir | Out-Null

# Pack the project
Write-Host ""
Write-Host "Creating NuGet package..." -ForegroundColor Yellow
$projectPath = Join-Path $PSScriptRoot 'src/Essential.OpenTelemetry.Exporter.ColoredConsole'
dotnet pack $projectPath `
    -c $Configuration `
    --no-build `
    -p:AssemblyVersion=$($v.AssemblySemVer) `
    -p:FileVersion=$($v.AssemblySemFileVer) `
    -p:Version=$($v.SemVer)+$($v.ShortSha) `
    -p:PackageVersion=$($v.FullSemVer) `
    --output $packDir

if (!$?) { throw 'Pack failed' }

Write-Host ""
Write-Host "Package created successfully in $packDir" -ForegroundColor Green
Write-Host ""
Write-Host "To publish to NuGet.org, run:" -ForegroundColor Cyan
Write-Host "  dotnet nuget push pack/*.nupkg --api-key YOUR_API_KEY --source https://api.nuget.org/v3/index.json" -ForegroundColor White
Write-Host ""
