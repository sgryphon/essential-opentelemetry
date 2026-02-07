![Essential OpenTelemetry](docs/images/diagnostics-logo-64.png)

# Essential .NET OpenTelemetry

Guidance, additional exporters, and other extensions for .NET `OpenTelemetry`

## Development

### Dev Containers

This project supports development in containers using VS Code Dev Containers, providing a consistent .NET 10 development environment with all tools pre-installed.

**Quick Start:**

1. Install VS Code and the Dev Containers extension
2. Install Docker Desktop or Podman
3. Open the project in VS Code
4. Press F1 and select "Dev Containers: Reopen in Container"

See [Development Container Setup](docs/DevContainers.md) for detailed instructions, including Podman configuration for Windows users.

## Supported .NET Versions

The library uses modern C# features (file-scoped namespaces, nullable reference types, implicit usings).

It is primarily targeted at .NET 10.0, but is also built for .NET 8.0 and .NET 9.0, using the relevant version of the Microsoft.Extensions library.

## License

Copyright (C) 2026 Gryphon Technology Pty Ltd

This library is free software: you can redistribute it and/or modify it under the terms of the GNU Lesser General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.

This library is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Lesser General Public License and GNU General Public License for more details.

You should have received a copy of the GNU Lesser General Public License and GNU General Public License along with this library. If not, see <https://www.gnu.org/licenses/>.
