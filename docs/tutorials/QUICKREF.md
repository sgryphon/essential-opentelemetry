# Quick Reference: Getting Started Examples

This is a quick reference for running the Getting Started tutorial examples.

## Console Examples (Simple, No Web Server)

### 1. Basic Logging

```bash
cd examples/GettingStarted/HelloWorld1-Console
dotnet run
```

**What it demonstrates:** Basic OpenTelemetry logging with colored console output

**Tutorial:** [HelloWorld1-Console.md](./HelloWorld1-Console.md)

### 2. Logging with Traces

```bash
cd examples/GettingStarted/HelloWorld2-Traces
dotnet run
```

**What it demonstrates:** Correlation between logs and traces using trace/span IDs

**Tutorial:** [HelloWorld2-Traces.md](./HelloWorld2-Traces.md)

### 3. Nested Spans

```bash
cd examples/GettingStarted/HelloWorld3-Spans
dotnet run
```

**What it demonstrates:** Parent-child span relationships and span attributes

**Tutorial:** [HelloWorld3-Spans.md](./HelloWorld3-Spans.md)

## Web Examples (ASP.NET Core, Requires Testing)

### 4. Web App with Auto-Tracing

```bash
# Terminal 1: Start the web app
cd examples/GettingStarted/HelloWorld4-AspNetCore
dotnet run

# Terminal 2: Make requests
curl http://localhost:5000
curl http://localhost:5000/greet/Alice
curl http://localhost:5000/greet/Bob
```

**What it demonstrates:** Automatic HTTP request tracing in ASP.NET Core

**Tutorial:** [HelloWorld4-AspNetCore.md](./HelloWorld4-AspNetCore.md)

### 5. Web App with Metrics

```bash
# Terminal 1: Start the web app
cd examples/GettingStarted/HelloWorld5-Metrics
dotnet run

# Terminal 2: Make requests (metrics export every 5 seconds)
curl http://localhost:5000
curl http://localhost:5000/greet/Alice
curl http://localhost:5000/slow
curl http://localhost:5000
curl http://localhost:5000/greet/Bob
# Wait to see metrics output in Terminal 1
```

**What it demonstrates:** Custom metrics collection and export

**Tutorial:** [HelloWorld5-Metrics.md](./HelloWorld5-Metrics.md)

## Tips

- **Console examples** run quickly and exit immediately - perfect for learning the basics
- **Web examples** need to be stopped with `Ctrl+C` - they run until terminated
- All examples use **colored console output** - run in a terminal with ANSI color support
- Examples build on each other - start with 1 and work through to 5
- Each example is self-contained and can be run independently

## Troubleshooting

### Port Already in Use

If you get an error about the port already being in use:

1. Stop any other web apps running on port 5000
2. Or modify the port in `Properties/launchSettings.json`

### Colors Not Showing

If you don't see colored output:

1. Ensure your terminal supports ANSI colors
2. On Windows, use Windows Terminal, PowerShell, or WSL
3. On Linux/Mac, most terminals support colors by default

### Build Errors

If you get build errors:

1. Ensure you have .NET SDK 10.0 or later installed
2. Run `dotnet restore` in the example directory
3. Check that the Essential.OpenTelemetry.Exporter.ColoredConsole project builds successfully

## Learn More

- [Getting Started Guide](../Getting-Started.md) - Main tutorial index
- [README](../../README.md) - Project documentation
