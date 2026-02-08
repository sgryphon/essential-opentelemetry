![Essential OpenTelemetry](docs/images/diagnostics-logo-64.png)

# Essential .NET OpenTelemetry

Guidance, additional exporters, and other extensions for [OpenTelemetry .NET](https://github.com/open-telemetry/opentelemetry-dotnet).

Currently this consists of a **ColoredConsoleExporter** that allows you to use OpenTelemetry from day one when building your project.

This exporter comfortably replaces the default console logging, and allows you to access the benefits of OpenTelemetry auto-instrumentation and standardised distributed tracing.

OpenTelemetry is widely supported by my diagnostics and application performance management providers -- this project brings that to your development console.

## Getting started with the Colored Console exporter

Install the NuGet package via `dotnet` or another package manager:

```powershell
dotnet add package Essential.OpenTelemetry.Exporter.ColoredConsole
```

Add the following using statements:

```csharp
using Essential.OpenTelemetry;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
```

In your host services, clear the default loggers and configure OpenTelemetry with the colored console exporter:

```csharp
builder.Logging.ClearProviders();
builder.Services.AddOpenTelemetry()
    .WithLogging(logging =>
    {
        logging.AddColoredConsoleExporter();
    });
```

## Features

- **Color-coded output**: Different colors for different log levels, trace events, and metrics
- **Structured logging support**: Displays structured log data in a readable format
- **OpenTelemetry standard compliance**: Works seamlessly with the OpenTelemetry SDK
- **Multi-framework support**: Compatible with currently supported .NET versions

## Documentation

New to OpenTelemetry? Check out our [Getting Started Guide](docs/Getting-Started.md) for a walk through settings up OpenTelemetry logging.

- [Getting Started](docs/Getting-Started.md) - New to OpenTelemetry? Check out our walk through guide.
- [Logging Levels](docs/Logging-Levels.md) - How to use logging levels.
- [Event IDs](docs/Event-Ids.md) - How to use events.
- [Performance Testing](docs/Performance.md) - Performance benchmarks and comparisons.

## Releases

### Versioning

This project uses [GitVersion](https://gitversion.net/) with **Mainline** mode for semantic versioning automatically calculated from Git history.

#### Creating Version Tags

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

### Packaging

Use the `build.ps1` script to create NuGet packages locally:

```powershell
# Build and test (default Release configuration)
.\Build-Nuget.ps1

# Build without tests
.\Build-Nuget.ps1 -SkipTests

# Build Debug configuration
.\Build-Nuget.ps1 -Configuration Debug
```

#### Publishing to NuGet.org

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
