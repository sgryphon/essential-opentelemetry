# Dev Containers Quick Reference

## Opening in Container

1. Open project in VS Code
2. Press `F1` or `Ctrl+Shift+P`
3. Type: `Dev Containers: Reopen in Container`

## Common Commands (Inside Container)

```bash
# Build
dotnet build Essential.OpenTelemetry.slnx

# Test
dotnet test Essential.OpenTelemetry.slnx

# Format
dotnet csharpier format .

# Run example
dotnet run --project examples/SimpleConsole/

# Restore tools
dotnet tool restore
```

## Rebuilding Container

1. Press `F1`
2. `Dev Containers: Rebuild Container`

## Windows with Podman

Set VS Code setting: `dev.containers.dockerPath` = `"podman"`

Or set environment variable:

```powershell
$env:DOCKER_HOST="npipe:////./pipe/podman-machine-default"
```

## Full Documentation

See [DevContainers.md](DevContainers.md) for complete instructions.
