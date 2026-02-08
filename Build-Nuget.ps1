#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Build and package Essential.OpenTelemetry NuGet packages

.DESCRIPTION
    This script builds and packages the Essential.OpenTelemetry library for NuGet.
    It uses GitVersion for automatic semantic versioning based on Git history.

.EXAMPLE
    .\Build-Nuget.ps1 -Verbose
    Build and package with Release configuration

.EXAMPLE
    .\Build-Nuget.ps1 -Configuration Debug -SkipTests
    Build and package Debug configuration without running tests
#>
[CmdletBinding()]
param(
    # Build configuration (Debug or Release). Default is Release.
    [string]$Configuration = "Release",
    # Skip running tests before packaging.
    [switch]$SkipTests,
    # Name of the base folder to create packages in
    $PackFolder = "pack"
)

$ErrorActionPreference = "Stop"

Write-Host "Building NuGet Packages for Essential OpenTelemetry" -ForegroundColor Cyan

# Restore dotnet tools
Write-Verbose "Restoring dotnet tools..."
dotnet tool restore
if (!$?) { throw 'Tool restore failed' }

# Get version from GitVersion
Write-Verbose "Getting version from GitVersion..."
$json = dotnet tool run dotnet-gitversion /output json
$gitVersionExitCode = $LASTEXITCODE

Write-Verbose "$json"
if ($gitVersionExitCode -ne 0) {
    throw "Gitversion failed"
} else {
    $v = ($json | ConvertFrom-Json)
}

Write-Host "Building version $($v.SemVer)+$($v.ShortSha) (NuGet $($v.FullSemVer))" -ForegroundColor Green

# Build solution
Write-Verbose "Building solution..."
dotnet build (Join-Path $PSScriptRoot Essential.OpenTelemetry.slnx) -c $Configuration
if (!$?) { throw 'Build failed' }

# Run tests (unless skipped)
if (!$SkipTests) {
    Write-Verbose "Running tests..."
    dotnet test (Join-Path $PSScriptRoot Essential.OpenTelemetry.slnx) -c $Configuration --no-build
    if (!$?) { throw 'Tests failed' }
}

# Create pack output directory
$packDir = Join-Path $PSScriptRoot $PackFolder
if (Test-Path $packDir) {
    Write-Verbose "Cleaning pack directory $packDir..."
    Remove-Item $packDir -Recurse -Force
}
New-Item -ItemType Directory -Path $packDir | Out-Null

# Pack the project
Write-Verbose "Creating NuGet package..."
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

Write-Host "Package created successfully in $packDir" -ForegroundColor Green
Write-Verbose "To publish to NuGet.org, run:"
Write-Verbose '  $nugetKey = "YOUR_API_KEY"'
Write-Verbose '  dotnet nuget push pack/*.nupkg --api-key $nugetKey --source https://api.nuget.org/v3/index.json'
