# GitHub Copilot Instructions

This repository contains Essential .NET OpenTelemetry - providing guidance, exporters, and extensions for .NET OpenTelemetry implementations.

## Key Guidelines

- **Language**: C# with .NET 10.0 target framework
- **Formatting**: Use CSharpier for C# files (`dotnet csharpier .`)
- **Testing**: xUnit framework - run with `dotnet test`
- **Building**: Build with `dotnet build Essential.OpenTelemetry.slnx`
- **Style**: Follow EditorConfig and existing code patterns
- **Nullable Types**: Always use nullable reference types (enabled by default)
- **Namespaces**: Use file-scoped namespaces (C# 10+ style)

## Project Structure

- `src/` - Library source code
- `test/` - Unit tests (xUnit)
- `examples/` - Example applications
- `docs/` - Documentation

## Before Committing

1. Format code: `dotnet csharpier .`
2. Run tests: `dotnet test`
3. Build solution: `dotnet build Essential.OpenTelemetry.slnx`

## Additional Details

For comprehensive guidelines, see [AGENTS.md](../AGENTS.md) in the repository root.
