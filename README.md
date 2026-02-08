![Essential OpenTelemetry](docs/images/diagnostics-logo-64.png)

# Essential .NET OpenTelemetry

Guidance, additional exporters, and other extensions for .NET `OpenTelemetry`

## Getting Started

New to OpenTelemetry? Check out our [Getting Started Guide](docs/Getting-Started.md) for hands-on tutorials that walk you through:

- Setting up OpenTelemetry logging in console applications
- Adding distributed tracing
- Instrumenting ASP.NET Core applications
- Viewing built-in metrics

The tutorials use the Essential OpenTelemetry colored console exporter to make observability data easy to read during development.

## Documentation

- [Getting Started](docs/Getting-Started.md)
- [Logging Levels](docs/Logging-Levels.md)
- [Event IDs](docs/Event-Ids.md)
- [Performance Testing](docs/Performance.md) - Performance benchmarks and comparisons

## Supported .NET Versions

This library supports the following .NET versions:

- .NET 10.0
- .NET 9.0
- .NET 8.0

All officially supported versions of .NET are targeted. The library uses modern C# features (file-scoped namespaces, nullable reference types, implicit usings) that are compatible with these versions.

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

Use the `build.ps1` script to create NuGet packages locally:

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

## Other projects

For earlier versions of .NET see:

- Essential Logging, for Microsoft.Extensions.Logging: <https://github.com/sgryphon/essential-logging>
- Essential Diagnostics, for System.Diagnostics: <https://github.com/sgryphon/essential-diagnostics>

## License

Copyright (C) 2026 Gryphon Technology Pty Ltd

This library is free software: you can redistribute it and/or modify it under the terms of the GNU Lesser General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.

This library is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Lesser General Public License and GNU General Public License for more details.

You should have received a copy of the GNU Lesser General Public License and GNU General Public License along with this library. If not, see <https://www.gnu.org/licenses/>.
