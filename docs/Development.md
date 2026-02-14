# Development notes

## Getting the code

This repository uses a git submodule for OpenTelemetry proto files. After cloning the repository, initialize the submodule:

```powershell
git submodule update --init --recursive
```

## Development tools

Several .NET tools are used; restore them for use during development.

```powershell
dotnet tool restore
```

## Tests

To run tests for the latest framework, use (omit `--framework` to run for all):

```powershell
dotnet test ./test/Essential.OpenTelemetry.Exporter.ColoredConsole.Tests --framework net10.0
```

A simple example application is also available:

```powershell
dotnet run --project examples/SimpleConsole/ --framework net10.0
```

## Code Coverage

To generate a code coverage report locally:

```powershell
# Run tests with coverage collection
dotnet test Essential.OpenTelemetry.slnx --configuration Release --framework net10.0 --collect:"XPlat Code Coverage" --results-directory ./coverage

# Generate an HTML report (requires dotnet tool restore)
dotnet reportgenerator -reports:"./coverage/**/coverage.cobertura.xml" -targetdir:"./coverage-report" -reporttypes:"Html;MarkdownSummary"
```

Then open `./coverage-report/index.html` in a browser to view the results.

## Versioning

This project uses [GitVersion](https://gitversion.net/) with **Mainline** mode for semantic versioning automatically calculated from Git history.

### Creating Version Tags

To bump versions, create Git tags on the main branch:

```powershell
# Minor version (1.0.0 → 1.1.0)
git tag v1.1.0

# Push tags
git push origin --tags
```

In mainline mode patch versions are automatically generated.

For more details see:

- [GitVersion Documentation](https://gitversion.net/docs/)
- [Semantic Versioning (SemVer)](https://semver.org/)

## Packaging

Use the `Build-Nuget.ps1` script to create NuGet packages locally:

```powershell
# Build and test (default Release configuration)
.\Build-Nuget.ps1

# Build without tests
.\Build-Nuget.ps1 -SkipTests

# Build Debug configuration
.\Build-Nuget.ps1 -Configuration Debug
```

### Publishing to NuGet.org

After building the package locally, publish it to NuGet.org:

```powershell
$nugetKey = "YOUR_API_KEY"
dotnet nuget push pack/Essential.OpenTelemetry.Exporter.ColoredConsole.1.0.0.nupkg --api-key $nugetKey --source https://api.nuget.org/v3/index.json
```

For more details see:

- [NuGet Documentation](https://learn.microsoft.com/en-us/nuget/)
