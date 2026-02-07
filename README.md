![Essential OpenTelemetry](docs/images/diagnostics-logo-64.png)

# Essential .NET OpenTelemetry

Guidance, additional exporters, and other extensions for .NET `OpenTelemetry`

## Supported .NET Versions

This library supports the following .NET versions:

- .NET 10.0
- .NET 9.0
- .NET 8.0

All officially supported versions of .NET are targeted. The library uses modern C# features (file-scoped namespaces, nullable reference types, implicit usings) that are compatible with these versions.

## Installation

Install the NuGet package:

```bash
dotnet add package Essential.OpenTelemetry.Exporter.ColoredConsole
```

Or via Package Manager:

```powershell
Install-Package Essential.OpenTelemetry.Exporter.ColoredConsole
```

## Features

- **Colored Console Exporter** - Enhanced console output for OpenTelemetry logs, traces, and metrics with color-coded formatting

## Development

### Building and Testing

```bash
# Build the solution
dotnet build Essential.OpenTelemetry.slnx

# Run tests
dotnet test Essential.OpenTelemetry.slnx

# Format code
dotnet csharpier format .
```

### Packaging for NuGet

See [docs/Packaging.md](docs/Packaging.md) for detailed instructions on creating and publishing NuGet packages.

Quick start:

```powershell
# Local build
.\build.ps1

# Or use GitHub Actions by pushing a version tag
git tag v1.0.0
git push origin v1.0.0
```

## License

Copyright (C) 2026 Gryphon Technology Pty Ltd

This library is free software: you can redistribute it and/or modify it under the terms of the GNU Lesser General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.

This library is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Lesser General Public License and GNU General Public License for more details.

You should have received a copy of the GNU Lesser General Public License and GNU General Public License along with this library. If not, see <https://www.gnu.org/licenses/>.
