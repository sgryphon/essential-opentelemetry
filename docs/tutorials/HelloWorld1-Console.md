# Hello World - Console Logging

In this tutorial, you'll create a simple console application that uses OpenTelemetry logging with the Essential OpenTelemetry colored console exporter.

## Create a New Console Application

Open a terminal or command prompt and create a new console application:

```bash
dotnet new console -n HelloLogging
cd HelloLogging
```

## Install Required Packages

Install the necessary NuGet packages:

```bash
dotnet add package Microsoft.Extensions.Hosting
dotnet add package OpenTelemetry.Extensions.Hosting
dotnet add package Essential.OpenTelemetry.Exporter.ColoredConsole
```

These packages provide:
- **Microsoft.Extensions.Hosting**: The hosting infrastructure for dependency injection and configuration
- **OpenTelemetry.Extensions.Hosting**: OpenTelemetry integration with the .NET hosting model
- **Essential.OpenTelemetry.Exporter.ColoredConsole**: The colored console exporter for viewing telemetry

## Write the Code

Replace the contents of `Program.cs` with the following code:

```csharp
using Essential.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// Create the application host with OpenTelemetry
var builder = Host.CreateApplicationBuilder(args);

// Clear default logging providers and configure OpenTelemetry
builder.Logging.ClearProviders();
builder
    .Services.AddOpenTelemetry()
    .WithLogging(logging =>
    {
        logging.AddColoredConsoleExporter();
    });

var host = builder.Build();

// Get the logger from the service provider
var logger = host.Services.GetRequiredService<ILogger<Program>>();

// Log some messages
logger.LogInformation("Hello World!");
logger.LogWarning("This is a warning message");
logger.LogError("This is an error message");
```

## Run the Application

Build and run your application:

```bash
dotnet run
```

You should see output similar to this (with colors in your terminal):

```
[timestamp] INFO [correlation-id]: Hello World!
[timestamp] WARN [correlation-id]: This is a warning message
[timestamp] ERROR [correlation-id]: This is an error message
```

**Screenshot placeholder:** _[Screenshot showing colored console output with INFO, WARN, and ERROR messages in different colors]_

## Understanding the Code

Let's break down what's happening:

### 1. Creating the Host

```csharp
var builder = Host.CreateApplicationBuilder(args);
```

This creates a host builder, which provides dependency injection, configuration, and logging infrastructure. This is the recommended approach for .NET applications, as it makes your code testable and follows best practices.

### 2. Configuring OpenTelemetry Logging

```csharp
builder.Logging.ClearProviders();
builder
    .Services.AddOpenTelemetry()
    .WithLogging(logging =>
    {
        logging.AddColoredConsoleExporter();
    });
```

- `ClearProviders()` removes the default logging providers (like the console logger)
- `AddOpenTelemetry()` adds OpenTelemetry services to the dependency injection container
- `WithLogging()` configures OpenTelemetry logging
- `AddColoredConsoleExporter()` adds the Essential OpenTelemetry colored console exporter

### 3. Getting the Logger

```csharp
var logger = host.Services.GetRequiredService<ILogger<Program>>();
```

This retrieves a logger instance from the service provider. The `ILogger<Program>` syntax creates a logger with the category name "Program", which helps identify where log messages come from.

### 4. Logging Messages

```csharp
logger.LogInformation("Hello World!");
logger.LogWarning("This is a warning message");
logger.LogError("This is an error message");
```

These methods log messages at different severity levels. The colored console exporter displays them in different colors to make them easy to distinguish.

## Log Levels

OpenTelemetry supports the following log levels (from most to least severe):

- **Critical**: For critical errors that require immediate attention
- **Error**: For error conditions that need to be addressed
- **Warning**: For warning messages about potential issues
- **Information**: For general informational messages
- **Debug**: For detailed debugging information
- **Trace**: For very detailed trace information

Try adding more log statements with different levels:

```csharp
logger.LogTrace("This is a trace message");
logger.LogDebug("This is a debug message");
logger.LogCritical("This is a critical message");
```

> **Note:** By default, Trace and Debug messages may not be displayed. You can configure the minimum log level in your application configuration if needed.

## Next Steps

Now that you have basic logging working, continue to the next tutorial: **[Adding Traces](./HelloWorld2-Traces.md)** to learn how to add distributed tracing to your application.

## Learn More

- [Logging Levels Documentation](../Logging-Levels.md)
- [OpenTelemetry Logging Concepts](https://opentelemetry.io/docs/concepts/signals/logs/)
