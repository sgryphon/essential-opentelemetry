# AGENTS.md - AI Assistant Guidelines

## Project Overview

**Essential .NET OpenTelemetry** provides guidance, additional exporters, and extensions for .NET OpenTelemetry implementations. The primary component is a colored console exporter for OpenTelemetry logging.

- **License**: LGPL v3
- **Copyright**: Gryphon Technology Pty Ltd
- **Primary Language**: C# (multi-targeting .NET 10.0, 9.0, 8.0)

## Key Guidelines

- **Language**: C# with multi-targeting support for .NET 10.0, 9.0, and 8.0
- **Formatting**: Use CSharpier for C# files (`dotnet csharpier .`). For other files use Prettier.
- **Testing**: xUnit framework - run with `dotnet test`
- **Building**: Build with `dotnet build Essential.OpenTelemetry.slnx`
- **Style**: Follow EditorConfig and existing code patterns
- **Nullable Types**: Always use nullable reference types (enabled by default)
- **Namespaces**: Use file-scoped namespaces (C# 10+ style)
- **Implicit usings**: Enabled (`<ImplicitUsings>enable</ImplicitUsings>`)
- **Frameworks**: Support .NET 8, .NET 9 and .NET 10; use #if where necessary to use new code constructs and have the older builds use older code.

## Project Structure

- `src/` - Library source code
- `test/` - Unit tests (xUnit)
- `examples/` - Example applications
- `docs/` - Documentation

## Before Committing

1. Format code: `dotnet csharpier format .`
2. Build solution: `dotnet build Essential.OpenTelemetry.slnx`
3. Run tests: `dotnet test Essential.OpenTelemetry.slnx`

## Development Tools & Environment

### Required Tools

- **.NET SDK 10.0** (target framework)
- **CSharpier 1.2.5** - C# code formatter (configured as dotnet tool)
- **GitVersion 6.5.1** - Semantic versioning (configured as dotnet tool)

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

- During development only run the specific test for the functionality you are changing, and only for the latest framework, to reduce time
- After it is working, then consider when you might need to run for other frameworks, or regression test that project
- When checking in, run the entire project (no framework filter)
- Don't run all tests in the entire solution (it takes too long)

```bash
dotnet test ./test/Essential.OpenTelemetry.Exporter.ColoredConsole.Tests --framework net10.0
```

## Versioning

- **GitVersion** handles semantic versioning

## Common Workflows

### Restoring Tools

```bash
dotnet tool restore
```

### Formatting Code

```bash
# Format C# code
dotnet csharpier format .
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
