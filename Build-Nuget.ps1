#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Build and package Essential.OpenTelemetry NuGet packages

.DESCRIPTION
    This script builds and packages the Essential.OpenTelemetry library for NuGet.
    It uses GitVersion for automatic semantic versioning based on Git history.

.EXAMPLE
    .\Build-Nuget.ps1 -Verbose
    Build and package all projects with Release configuration

.EXAMPLE
    .\Build-Nuget.ps1 -Project ColoredConsole
    Build and package only the ColoredConsole exporter

.EXAMPLE
    .\Build-Nuget.ps1 -Project OtlpFile -PackageVersion 0.1.2-alpha.1
    Build and package the OtlpFile exporter with a specific version override

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
    $PackFolder = "pack",
    # Which project(s) to pack (All, ColoredConsole, OtlpFile). Default is All.
    [ValidateSet("All", "ColoredConsole", "OtlpFile")]
    [string]$Project = "All",
    # Override the version for all packages. If not specified, the GitVersion-generated version is used.
    # When set, PackageVersion is set to this value, FileVersion to "major.minor.patch.0",
    # AssemblyVersion to "major.minor.0.0", and Version (InformationalVersion) to "$PackageVersion+$ShortSha"
    [string]$PackageVersion = ""
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

# Determine version values based on whether PackageVersion is specified
if ($PackageVersion -ne "") {
    Write-Host "Using package version override: $PackageVersion" -ForegroundColor Yellow
    
    # Parse major.minor.patch from PackageVersion (e.g., "1.2.3-beta.1" -> major=1, minor=2, patch=3)
    if ($PackageVersion -match '^(\d+)\.(\d+)\.(\d+)')
    {
        $major = $matches[1]
        $minor = $matches[2]
        $patch = $matches[3]
        
        $nugetVersion = $PackageVersion
        $assemblyVersion = "$major.$minor.0.0"
        $fileVersion = "$major.$minor.$patch.0"
        $version = "$PackageVersion+$($v.ShortSha)"
    }
    else
    {
        throw "PackageVersion '$PackageVersion' does not match expected format 'major.minor.patch[...]"
    }
}
else {
    # Use GitVersion InformationalVersion (includes short Sha)
    $nugetVersion = $v.SemVer
    $assemblyVersion = $v.AssemblySemVer
    $fileVersion = $v.AssemblySemFileVer
    $version = $v.InformationalVersion
}

Write-Host "Building version $version (Assembly $assemblyVersion, NuGet $nugetVersion)" -ForegroundColor Green

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

# Pack the ColoredConsole project
if ($Project -eq "All" -or $Project -eq "ColoredConsole") {
    Write-Verbose "Creating ColoredConsole NuGet package..."
    $projectPath = Join-Path $PSScriptRoot 'src/Essential.OpenTelemetry.Exporter.ColoredConsole'
    dotnet pack $projectPath `
        -c $Configuration `
        --no-build `
        -p:AssemblyVersion=$assemblyVersion `
        -p:FileVersion=$fileVersion `
        -p:Version=$version `
        -p:PackageVersion=$nugetVersion `
        --output $packDir
    if (!$?) { throw 'ColoredConsole pack failed' }
}

# Pack the OtlpFile project
if ($Project -eq "All" -or $Project -eq "OtlpFile") {
    Write-Verbose "Creating OtlpFile NuGet package..."
    $projectPath = Join-Path $PSScriptRoot 'src/Essential.OpenTelemetry.Exporter.OtlpFile'

    dotnet pack $projectPath `
        -c $Configuration `
        --no-build `
        -p:AssemblyVersion=$assemblyVersion `
        -p:FileVersion=$fileVersion `
        -p:Version=$version `
        -p:PackageVersion=$nugetVersion `
        --output $packDir
    if (!$?) { throw 'OtlpFile pack failed' }
}

Write-Host "Package(s) created successfully in $packDir" -ForegroundColor Green
Write-Verbose "To publish to NuGet.org, run:"
Write-Verbose '  $nugetKey = "YOUR_API_KEY"'
Write-Verbose '  dotnet nuget push pack/*.nupkg --api-key $nugetKey --source https://api.nuget.org/v3/index.json'
