# Packaging and Publishing to NuGet

This document describes how to package and publish the Essential.OpenTelemetry library to NuGet.org.

## Overview

The project supports two methods for creating and publishing NuGet packages:

1. **Local Build Script** - Manual packaging using PowerShell script
2. **GitHub Actions** - Automated builds and publishing via CI/CD

Both methods use [GitVersion](https://gitversion.net/) for automatic semantic versioning based on Git commit history.

## Prerequisites

- .NET SDK 10.0 or later
- PowerShell Core (for local builds)
- NuGet API key (for publishing)

## Local Build and Packaging

### Building Packages Locally

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

### Publishing to NuGet.org

After building the package locally, publish it to NuGet.org:

```powershell
dotnet nuget push pack/*.nupkg --api-key YOUR_API_KEY --source https://api.nuget.org/v3/index.json
```

**Note**: Replace `YOUR_API_KEY` with your actual NuGet API key.

#### Getting a NuGet API Key

1. Create an account on [NuGet.org](https://www.nuget.org)
2. Go to your account settings
3. Navigate to "API Keys"
4. Create a new API key with push permissions for your packages
5. Store the key securely (treat it like a password)

## Automated GitHub Actions Workflow

### How It Works

The GitHub Actions workflow (`.github/workflows/build-and-publish.yml`) automatically:

- **On Pull Requests**: Build and test only
- **On Push to Main**: Build, test, and create package artifacts
- **On Version Tags** (e.g., `v1.0.0`): Build, test, create packages, and publish to NuGet.org

### Setting Up GitHub Actions Publishing

To enable automated publishing to NuGet.org:

1. Get your NuGet API key (see above)
2. Add it as a GitHub secret:
   - Go to your repository on GitHub
   - Navigate to Settings → Secrets and variables → Actions
   - Click "New repository secret"
   - Name: `NUGET_API_KEY`
   - Value: Your NuGet API key
   - Click "Add secret"

### Publishing a Release

To publish a new version:

1. Ensure all changes are committed and pushed to `main`
2. Create and push a version tag:
   ```bash
   git tag v1.0.0
   git push origin v1.0.0
   ```
3. GitHub Actions will automatically build, test, and publish the package

### Downloading Build Artifacts

For non-tagged builds (pull requests or pushes to main), packages are available as workflow artifacts:

1. Go to the Actions tab in your GitHub repository
2. Click on the workflow run
3. Download the "nuget-packages" artifact

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

This will trigger the GitHub Actions workflow to automatically build, test, and publish version 1.0.0 to NuGet.org (if the `NUGET_API_KEY` secret is configured).

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

### GitHub Actions Workflow Failures

If the GitHub Actions workflow fails:

1. Check the workflow logs in the Actions tab
2. Verify the `NUGET_API_KEY` secret is set correctly
3. Ensure .NET 10.0 SDK is available (update workflow if needed)

## Additional Resources

- [NuGet Documentation](https://docs.microsoft.com/en-us/nuget/)
- [GitVersion Documentation](https://gitversion.net/docs/)
- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [Semantic Versioning (SemVer)](https://semver.org/)
