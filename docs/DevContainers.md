# Development Container Setup

This project includes VS Code Dev Container configurations for developing in containerized environments. This allows you to develop with a consistent, reproducible environment regardless of your host operating system.

## What are Dev Containers?

Development Containers (Dev Containers) let you use a container as a full-featured development environment. VS Code can run inside the container, providing a Linux-based development environment with all the necessary tools pre-installed.

## Prerequisites

### Required Software

1. **VS Code** - Install from [code.visualstudio.com](https://code.visualstudio.com/)
2. **Dev Containers Extension** - Install from VS Code Extensions (search for "Dev Containers" by Microsoft)
3. **Container Runtime** - One of the following:
   - **Docker Desktop** (Windows/Mac/Linux)
   - **Podman** (Linux/Windows WSL2) - See [Podman Setup](#using-podman-windows) below
   - **Docker Engine** (Linux)

### Windows with Podman

If you're using Podman on Windows (WSL2/Hyper-V), follow these additional steps:

1. Install Podman Desktop from [podman-desktop.io](https://podman-desktop.io/)
2. Start the Podman Machine (or let Podman Desktop manage it)
3. Configure VS Code to use Podman:
   - Open VS Code Settings (Ctrl+,)
   - Search for "dev.containers.dockerPath"
   - Set it to "podman" (or the full path to podman.exe)
4. Alternatively, set the environment variable:
   ```powershell
   $env:DOCKER_HOST="npipe:////./pipe/podman-machine-default"
   ```

## Available Configurations

This project provides three Dev Container configurations for different .NET versions:

- **.NET 10** (default) - Latest version, located in `.devcontainer/`
- **.NET 9** - Located in `.devcontainer/dotnet9/`
- **.NET 8** - Located in `.devcontainer/dotnet8/`

## Getting Started

### Method 1: Open in Container (Recommended)

1. Open the project folder in VS Code
2. Press `F1` (or `Ctrl+Shift+P`) to open the Command Palette
3. Type and select: **Dev Containers: Reopen in Container**
4. Select the desired configuration:
   - "Essential OpenTelemetry (.NET 10)" - Default, uses .NET 10
   - "Essential OpenTelemetry (.NET 9)" - Uses .NET 9
   - "Essential OpenTelemetry (.NET 8)" - Uses .NET 8
5. Wait for the container to build and initialize (first time takes longer)
6. VS Code will reload inside the container

### Method 2: Clone in Container

1. Press `F1` in VS Code
2. Type and select: **Dev Containers: Clone Repository in Container Volume**
3. Enter the repository URL: `https://github.com/sgryphon/essential-opentelemetry`
4. Select the desired .NET version configuration
5. Wait for the container to build and the repository to clone

## What's Included

Each Dev Container includes:

- **Pre-installed .NET SDK** (8.0, 9.0, or 10.0 depending on configuration)
- **Git** - Version control
- **GitHub CLI (gh)** - GitHub command-line tool
- **VS Code Extensions**:
  - C# Dev Kit
  - C# language support
  - EditorConfig
  - CSharpier (C# formatter)
  - Prettier (Markdown/JSON formatter)
- **NuGet package cache** - Mounted from host for faster restores
- **dotnet tools** - CSharpier and GitVersion (automatically restored)

## Working in the Dev Container

Once inside the container, you can use the integrated terminal to run all project commands:

### Building

```bash
dotnet build Essential.OpenTelemetry.slnx
```

### Testing

```bash
dotnet test Essential.OpenTelemetry.slnx
```

### Formatting

```bash
dotnet csharpier format .
```

### Running Examples

```bash
dotnet run --project examples/SimpleConsole/
```

## Tips and Best Practices

### Performance Optimization

The Dev Container configuration mounts your local NuGet package cache into the container. This means:

- Faster package restores
- Reduced bandwidth usage
- Packages downloaded once are available in all containers

### Rebuilding the Container

If you need to rebuild the container (e.g., after configuration changes):

1. Press `F1`
2. Select: **Dev Containers: Rebuild Container**
3. Choose "Rebuild" or "Rebuild Without Cache"

### Switching Between .NET Versions

To test your code on different .NET versions:

1. Press `F1`
2. Select: **Dev Containers: Reopen in Container**
3. Choose a different configuration (e.g., switch from .NET 10 to .NET 8)
4. Your local changes are preserved

### Running Multiple Containers

You can open the same project in multiple VS Code windows with different .NET versions:

1. Open the project in VS Code
2. Open in .NET 10 container
3. Open a new VS Code window (File > New Window)
4. Open the same project folder
5. Open in .NET 8 or 9 container

This allows side-by-side testing across .NET versions.

## Troubleshooting

### Container Fails to Start

**Symptom**: Error message when trying to open in container

**Solutions**:

1. Ensure your container runtime is running (Docker Desktop or Podman)
2. Check Docker/Podman is accessible from command line:
   ```bash
   docker --version   # or: podman --version
   ```
3. Check VS Code can find Docker/Podman:
   - Settings > dev.containers.dockerPath
4. Try rebuilding: Dev Containers: Rebuild Container

### Podman-Specific Issues

**Symptom**: VS Code can't connect to Podman

**Solutions**:

1. Ensure Podman Machine is running:
   ```bash
   podman machine list
   podman machine start
   ```
2. Set the DOCKER_HOST environment variable (Windows):
   ```powershell
   $env:DOCKER_HOST="npipe:////./pipe/podman-machine-default"
   ```
3. Or configure VS Code settings: dev.containers.dockerPath = "podman"

### Slow Performance on Windows

**Symptom**: Container is slow, especially file operations

**Solutions**:

1. Ensure you're using WSL2 backend (not Hyper-V)
2. Clone repository inside WSL2 for better performance
3. Consider using "Clone in Container Volume" for best performance

### Extensions Not Loading

**Symptom**: VS Code extensions missing in container

**Solution**:

1. Wait for extensions to install (check status bar)
2. Rebuild container: Dev Containers: Rebuild Container
3. Manually install extensions from Extensions sidebar

## Advanced Configuration

### Customizing the Dev Container

You can customize the Dev Container by editing `.devcontainer/devcontainer.json`:

- **Add more extensions**: Add to `customizations.vscode.extensions`
- **Change VS Code settings**: Modify `customizations.vscode.settings`
- **Add more tools**: Use the `features` section or `postCreateCommand`
- **Expose ports**: Add to `forwardPorts` for web applications

Example: Adding a port forward

```json
"forwardPorts": [5000, 5001]
```

### Using with Podman Compose

If you prefer Docker Compose style orchestration with Podman:

```bash
# On Windows/WSL2 with Podman
podman-compose up -d
```

## Resources

- [VS Code Dev Containers Documentation](https://code.visualstudio.com/docs/devcontainers/containers)
- [Dev Container Specification](https://containers.dev/)
- [Podman Desktop Documentation](https://podman-desktop.io/docs)
- [Microsoft .NET Container Images](https://hub.docker.com/_/microsoft-dotnet)

## Need Help?

If you encounter issues not covered here, please:

1. Check the [VS Code Dev Containers documentation](https://code.visualstudio.com/docs/devcontainers/containers)
2. Check Podman documentation if using Podman
3. Open an issue in this repository with details about your environment and the problem
