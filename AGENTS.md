# AGENTS.md - AI Assistant Guidelines

This file provides context and guidelines for AI coding assistants working on the Essential .NET OpenTelemetry project.

## Project Overview

**Essential .NET OpenTelemetry** provides guidance, additional exporters, and extensions for .NET OpenTelemetry implementations. The primary component is a colored console exporter for OpenTelemetry logging.

- **License**: LGPL v3
- **Copyright**: Gryphon Technology Pty Ltd
- **Primary Language**: C# (.NET 10.0)

## Repository Structure

```
Essential.OpenTelemetry.slnx     # XML-based solution file (preferred format)
├── src/                         # Source code for libraries
│   └── Essential.OpenTelemetry.Exporter.ColoredConsole/
├── test/                        # Unit tests (xUnit)
│   └── Essential.OpenTelemetry.Exporter.ColoredConsole.Tests/
├── examples/                    # Example applications
│   └── SimpleConsole/
├── docs/                        # Documentation
└── .config/                     # Tooling configuration
    └── dotnet-tools.json
```

## Development Tools & Environment

### Required Tools

- **.NET SDK 10.0** (target framework)
- **CSharpier 1.2.5** - C# code formatter (configured as dotnet tool)
- **GitVersion 6.5.1** - Semantic versioning (configured as dotnet tool)

### VS Code Extensions

The project recommends these extensions (in [.vscode/extensions.json](.vscode/extensions.json)):

- `EditorConfig.EditorConfig` - Base formatting rules
- `csharpier.csharpier-vscode` - C# formatting
- `esbenp.prettier-vscode` - Formatting for JSON, YAML, Markdown, etc.

### Formatting Configuration

- **Format on save**: Enabled
- **C# files**: Formatted with CSharpier
- **JSON/JSONC**: Formatted with Prettier (format on save disabled)
- **Markdown, YAML, HTML, TypeScript**: Formatted with Prettier
- **Default solution**: `Essential.OpenTelemetry.slnx`

## Code Style & Conventions

### General Rules (from .editorconfig)

- **Indentation**: Spaces (never tabs)
- **C# files** (`.cs`):
  - 4 spaces per indent level
  - UTF-8 with BOM encoding
  - Insert final newline
- **XML files** (`.csproj`, `.props`, `.targets`, etc.): 2 spaces
- **JSON files**: 2 spaces
- **PowerShell** (`.ps1`): 2 spaces
- **Shell scripts** (`.sh`): 2 spaces, LF line endings

### C# Specific Conventions

- **Nullable reference types**: Enabled (`<Nullable>enable</Nullable>`)
- **Implicit usings**: Enabled (`<ImplicitUsings>enable</ImplicitUsings>`)
- **XML documentation**: Suppressed for CS1591 (missing XML comments warning)
- **Namespace style**: File-scoped namespaces (C# 10+ style)
- **Access modifiers**: Explicit for public APIs
- **Naming**: PascalCase for public members, camelCase for parameters

### Project File Conventions

- Use SDK-style project files
- Target framework: `net10.0` for libraries
- References:
  - OpenTelemetry core packages
  - Microsoft.Extensions.\* for dependency injection and hosting
- Organize ItemGroups logically (properties, packages, project references)

## Testing Strategy

### Test Framework

- **xUnit 2.9.2** - Primary test framework
- **Microsoft.NET.Test.Sdk 17.11.1** - Test platform

### Test Conventions

- Test projects named: `{ProjectName}.Tests`
- Test files named: `{ClassUnderTest}Tests.cs`
- Test methods: Descriptive names ending in `Test` (e.g., `FullIntegrationTest`)
- Use `Fact` for simple tests, `Theory` for parameterized tests
- Arrange-Act-Assert pattern with comments
- Mock external dependencies (see `MockConsole` pattern)

### Running Tests

```bash
dotnet test
```

## Versioning

- **GitVersion** handles semantic versioning
- Mode: Mainline
- Assembly versioning: MajorMinor
- Informational format: `{SemVer}+{ShortSha}`

## Common Workflows

### Restoring Tools

```bash
dotnet tool restore
```

### Formatting Code

```bash
# Format C# code
dotnet csharpier .

# VS Code will auto-format on save
```

### Building

```bash
# Build entire solution
dotnet build Essential.OpenTelemetry.slnx

# Build specific project
dotnet build src/Essential.OpenTelemetry.Exporter.ColoredConsole/
```

### Running Examples

```bash
dotnet run --project examples/SimpleConsole/
```

## API Design Patterns

### Extension Methods

- Place OpenTelemetry extensions in `Essential.OpenTelemetry` namespace
- Follow OpenTelemetry naming: `Add{ComponentName}Exporter`
- Support optional configuration via `Action<TOptions>`
- Provide overloads for common scenarios

### Options Pattern

- Use `Microsoft.Extensions.Options`
- Options classes named: `{ComponentName}Options`
- Validate required parameters
- Use null-coalescing for defaults: `name ??= Options.DefaultName`

### Dependency Injection

- Use constructor injection
- Register services in extension methods
- Support named options
- Make implementations internal where appropriate

## Documentation

### XML Documentation

- Required for all public APIs
- Use standard XML tags: `<summary>`, `<param>`, `<returns>`
- Reference types with `<see cref="TypeName"/>`
- Keep descriptions concise and clear

### Markdown Files

- Follow standard Markdown conventions
- Use Prettier formatting
- Include code examples where helpful
- See [docs/](docs/) for existing documentation patterns

## Adding New Components

When adding new exporters or extensions:

1. Create project in `src/` directory
2. Follow naming: `Essential.OpenTelemetry.{ComponentType}.{ComponentName}`
3. Add corresponding test project in `test/`
4. Update solution file (`Essential.OpenTelemetry.slnx`)
5. Add example in `examples/` if applicable
6. Document in `docs/` as needed
7. Use consistent patterns with existing exporters

## Important Notes

- **Do not commit** `bin/`, `obj/` directories
- **InternalsVisibleTo**: Used for test assembly access
- **Console abstraction**: Use `IConsole` interface for testability
- **Thread safety**: Consider for exporters and shared resources
- **Performance**: OpenTelemetry is performance-sensitive, avoid allocations in hot paths

## AI Assistant Recommendations

When working on this project:

1. **Maintain consistency** with existing code patterns
2. **Run formatters** before committing (CSharpier handles this)
3. **Write tests** for new functionality using xUnit
4. **Follow OpenTelemetry patterns** for exporters and extensions
5. **Use nullable reference types** correctly
6. **Keep dependencies minimal** and up-to-date
7. **Consider testability** when designing APIs (use interfaces)
8. **Document public APIs** with XML comments

## References

- [OpenTelemetry .NET](https://github.com/open-telemetry/opentelemetry-dotnet)
- [EditorConfig](https://editorconfig.org/)
- [CSharpier](https://csharpier.com/)
- [GitVersion](https://gitversion.net/)
