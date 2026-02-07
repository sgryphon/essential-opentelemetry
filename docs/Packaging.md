# Packaging and Publishing to NuGet

This document describes how to package and publish the Essential.OpenTelemetry library to NuGet.org.

## Overview

The project uses a local build script for creating and publishing NuGet packages. The script uses [GitVersion](https://gitversion.net/) for automatic semantic versioning based on Git commit history.

## Prerequisites

- .NET SDK 10.0 or later
- PowerShell Core
- NuGet API key (for publishing)

## Building Packages Locally

Use the `build.ps1` script to create NuGet packages locally:

```powershell
# Build and test (default Release configuration)
.\build.ps1

# Build without tests
.\build.ps1 -SkipTests

# Build Debug configuration
.\build.ps1 -Configuration Debug
```

The script will:
1. Restore dotnet tools (including GitVersion)
2. Calculate version from Git history (or use fallback version 0.1.0-dev if GitVersion fails)
3. Build the solution
4. Run tests (unless `-SkipTests` is specified)
5. Create NuGet package in the `pack/` directory

**Note**: If you haven't created any version tags yet, GitVersion will not be able to determine a version, and the script will use a default version of `0.1.0-dev`. To create a proper version, see the "Creating Version Tags" section below.

## Publishing to NuGet.org

After building the package locally, publish it to NuGet.org:

```powershell
dotnet nuget push pack/*.nupkg --api-key YOUR_API_KEY --source https://api.nuget.org/v3/index.json
```

**Note**: Replace `YOUR_API_KEY` with your actual NuGet API key.

### Getting a NuGet API Key

1. Create an account on [NuGet.org](https://www.nuget.org)
2. Go to your account settings
3. Navigate to "API Keys"
4. Create a new API key with push permissions for your packages
5. Store the key securely (treat it like a password)

## Versioning

This project uses [GitVersion](https://gitversion.net/) with **Mainline** mode for versioning:

- **Semantic versioning** (SemVer) is automatically calculated from Git history
- **Version format**: `Major.Minor.Patch` (e.g., `1.0.0`)
- **Pre-release versions**: Commits on feature branches get pre-release tags
- **Build metadata**: Short commit SHA is included (e.g., `1.0.0+abc1234`)

### Version Configuration

The versioning behavior is configured in `GitVersion.yml`:

```yaml
mode: Mainline
assembly-versioning-scheme: MajorMinor
assembly-informational-format: '{SemVer}+{ShortSha}'
```

### Creating Version Tags

To bump versions, create Git tags on the main branch:

```bash
# Patch version (1.0.0 → 1.0.1)
git tag v1.0.1

# Minor version (1.0.0 → 1.1.0)
git tag v1.1.0

# Major version (1.0.0 → 2.0.0)
git tag v2.0.0

# Push tags
git push origin --tags
```

### Creating the First Release

For the initial release, create a `v1.0.0` tag on the main branch:

```bash
# Ensure you're on the main branch with all changes merged
git checkout main
git pull

# Create the first version tag
git tag v1.0.0 -a -m "Initial release"

# Push the tag to GitHub
git push origin v1.0.0
```

After creating the tag, run the build script to create the package with the proper version, then publish it manually to NuGet.org.

## Package Metadata

Package metadata is defined in the project file (`Essential.OpenTelemetry.Exporter.ColoredConsole.csproj`):

- **Package ID**: `Essential.OpenTelemetry.Exporter.ColoredConsole`
- **Authors**: Gryphon Technology Pty Ltd
- **License**: LGPL-3.0-or-later
- **Repository**: https://github.com/sgryphon/essential-opentelemetry
- **Tags**: opentelemetry, logging, tracing, metrics, console, exporter, dotnet

## Troubleshooting

### GitVersion Errors

If GitVersion fails:

1. Ensure you have a full Git history (not a shallow clone)
2. Check that `GitVersion.yml` is properly configured
3. Verify you're on a valid branch with commits

### Build Failures

If the build script fails:

1. Ensure all dotnet tools are restored: `dotnet tool restore`
2. Check that the solution builds: `dotnet build Essential.OpenTelemetry.slnx`
3. Verify tests pass: `dotnet test Essential.OpenTelemetry.slnx`

## Additional Resources

- [NuGet Documentation](https://docs.microsoft.com/en-us/nuget/)
- [GitVersion Documentation](https://gitversion.net/docs/)
- [Semantic Versioning (SemVer)](https://semver.org/)
