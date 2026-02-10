![Essential OpenTelemetry](docs/images/diagnostics-logo-64.png)

# Essential .NET OpenTelemetry

Guidance, additional exporters, and other extensions for [OpenTelemetry .NET](https://github.com/open-telemetry/opentelemetry-dotnet).

Currently this consists of a [ColoredConsoleExporter](src/Essential.OpenTelemetry.Exporter.ColoredConsole/README.md) that allows you to use OpenTelemetry from day one when building your project.

This exporter comfortably replaces the default console logging, and allows you to access the benefits of OpenTelemetry auto-instrumentation and standardised distributed tracing.

OpenTelemetry is widely supported by many diagnostics and application performance management providers — this project brings that to your development console.

[![Build](https://github.com/sgryphon/essential-opentelemetry/actions/workflows/build-pipeline.yml/badge.svg)](https://github.com/sgryphon/essential-opentelemetry/actions/workflows/build-pipeline.yml)
[![codecov](https://codecov.io/gh/sgryphon/essential-opentelemetry/branch/main/graph/badge.svg)](https://codecov.io/gh/sgryphon/essential-opentelemetry)

## Getting started with the Colored Console exporter

1. Install the [ColoredConsole NuGet package](https://www.nuget.org/packages/Essential.OpenTelemetry.Exporter.ColoredConsole) via `dotnet` or another package manager:

```powershell
dotnet add package Essential.OpenTelemetry.Exporter.ColoredConsole
```

2. Add the following using statements:

```csharp
using Essential.OpenTelemetry;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
```

3. In your host services, clear the default loggers and configure OpenTelemetry with the colored console exporter:

```csharp
builder.Logging.ClearProviders();
builder.Services.AddOpenTelemetry()
    .WithLogging(logging =>
    {
        logging.AddColoredConsoleExporter();
    });
```

Existing logging will then output using OpenTelemetry, and you can continue development knowing that your application has access to the entire [OpenTelemetry ecosystem](https://opentelemetry.io/ecosystem/vendors/).

## Documentation

New to OpenTelemetry? Check out our [Getting Started Guide](docs/Getting-Started.md) for a walk through for setting up OpenTelemetry logging.

- [Getting Started](docs/Getting-Started.md) - New to OpenTelemetry? Check out our walk through guide.
- [Logging Levels](docs/Logging-Levels.md) - How to use logging levels.
- [Event IDs](docs/Event-Ids.md) - How to use events.
- [Performance Testing](docs/Performance.md) - Performance benchmarks and comparisons.

Or for a simple example application, just clone this repository and run:

```powershell
dotnet run --project .\examples\SimpleConsole --framework net10.0
```

The example supports earlier frameworks, e.g. if you are still using net8.0.

## Earlier related projects

For earlier generations of .NET diagnostics frameworks, see the related projects:

- Essential Logging, for Microsoft.Extensions.Logging: <https://github.com/sgryphon/essential-logging>
- Essential Diagnostics, for System.Diagnostics: <https://github.com/sgryphon/essential-diagnostics>

Guidance, additional exporters, and other extensions for .NET `OpenTelemetry`

## Getting Started

- [Continuous Integration](docs/Continuous-Integration.md) - CI/CD pipeline and testing guide

## License

Copyright (C) 2026 Gryphon Technology Pty Ltd

This library is free software: you can redistribute it and/or modify it under the terms of the GNU Lesser General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.

This library is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Lesser General Public License and GNU General Public License for more details.

You should have received a copy of the GNU Lesser General Public License and GNU General Public License along with this library. If not, see <https://www.gnu.org/licenses/>.
