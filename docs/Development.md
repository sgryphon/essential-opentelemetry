# Development notes

## Versioning

This project uses [GitVersion](https://gitversion.net/) with **Mainline** mode for semantic versioning automatically calculated from Git history.

### Creating Version Tags

To bump versions, create Git tags on the main branch:

```bash
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
