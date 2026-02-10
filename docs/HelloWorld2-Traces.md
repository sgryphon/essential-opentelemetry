# Hello World - Adding Traces

In this tutorial, you'll add distributed tracing to your console application. Traces help you understand the flow of requests through your application and identify performance bottlenecks.

This example also shows the best practice use of [compile-time logging source generation](https://learn.microsoft.com/en-us/dotnet/core/extensions/logging/source-generation).

## What are Traces?

A trace represents the journey of a request through your application. It consists of one or more spans, where each span represents a unit of work. Traces help you:

- Understand the flow of execution in your application
- Identify performance bottlenecks
- Track distributed operations across services
- Correlate logs with specific operations

Standard W3C Trace IDs are automatically sent by .NET in HTTP requests, allowing distributed trace correlation by default.

## Update Your Application

Starting from the previous tutorial (or create a new console application following the same steps), update your `Program.cs`:

```csharp
using System.Diagnostics;
using Essential.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;

const string ServiceName = "HelloOpenTelemetry";

// Create an ActivitySource for tracing
var activitySource = new ActivitySource(ServiceName);

// Create the application host with OpenTelemetry
var builder = Host.CreateApplicationBuilder(args);

// Configure OpenTelemetry with both logging and tracing
builder.Logging.ClearProviders();
builder
    .Services.AddOpenTelemetry()
    .WithLogging(logging =>
    {
        logging.AddColoredConsoleExporter();
    })
    .WithTracing(tracing =>
    {
        tracing.AddSource(ServiceName).AddColoredConsoleExporter();
    });

var host = builder.Build();

// Get services
var logger = host.Services.GetRequiredService<ILogger<Program>>();
var tracerProvider = host.Services.GetRequiredService<TracerProvider>();

// Create a trace
using (var activity = activitySource.StartActivity("MainOperation"))
{
    logger.OperationStarting("MainOperation");

    // Simulate some work
    Thread.Sleep(100);

    logger.OperationCompleted();
}

// Force flush to ensure all telemetry is exported before exit
tracerProvider.ForceFlush();

// Source-generated log methods
internal static partial class LoggerExtensions
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Starting {Name}")]
    public static partial void OperationStarting(this ILogger logger, string name);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Main operation completed")]
    public static partial void OperationCompleted(this ILogger logger);
}
```

## Run the Application

Build and run your application:

```powershell
dotnet run
```

You should see output similar to this:

![Example HelloOpenTelemetry tracing screen](images/screen-hello-tracing.png)

## Understanding the Code

### 1. Creating an ActivitySource

```csharp
const string ServiceName = "HelloOpenTelemetry";
var activitySource = new ActivitySource(ServiceName);
```

An `ActivitySource` is used to create activities (spans). The service name identifies your application in the telemetry data.

### 2. Configuring Tracing

```csharp
.WithTracing(tracing =>
{
    tracing.AddSource(ServiceName).AddColoredConsoleExporter();
})
```

- `WithTracing()` configures OpenTelemetry tracing
- `AddSource(ServiceName)` tells OpenTelemetry to collect activities from your ActivitySource
- `AddColoredConsoleExporter()` adds the colored console exporter for traces

**NOTE:** You only need to create your own activity source if you want to create custom activities. Many existing components have automatically instrumented activities, such as ASP.NET, Entity Framework, and other system components.

### 3. Creating an Activity

```csharp
using (var activity = activitySource.StartActivity("MainOperation"))
{
    // Your code here
}
```

The `StartActivity()` method creates a new activity (span). The `using` statement ensures the activity is properly ended when the block completes. Activities automatically capture:

- Start and end times
- Duration
- A unique trace ID (shared across related activities)
- A unique span ID (unique to this activity)

### 4. Flushing Telemetry

```csharp
tracerProvider.ForceFlush();
```

This ensures all telemetry is exported before the application exits. Without this, some telemetry might be lost in short-lived applications.

### 5. Source-Generated Log Methods

```csharp
internal static partial class LoggerExtensions
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Starting {Name}")]
    public static partial void OperationStarting(this ILogger logger, string name);
    ...
}
```

The `[LoggerMessage]` attribute uses C# source generators to create high-performance logging methods at compile time. This is the recommended best practice for logging in .NET and provides:

- Zero allocation
- Compile-time validation
- Event logging — the log event name (e.g. OperationStarting) is added as structured data, and output by the colored console logger
- Structured logging — parameters like `{Name}` are preserved as structured data, making them searchable in telemetry backends

Each method must be `partial` and defined in a `partial class`. The source generator fills in the implementation, which skips string formatting entirely when the log level is disabled.

For full details, see the Microsoft documentation: [Compile-time logging source generation](https://learn.microsoft.com/en-us/dotnet/core/extensions/logging/source-generation).

## Understanding Trace and Span IDs

When you run the application, you'll notice the log messages and span have the same trace ID. This correlation allows you to:

- See all logs that occurred during a particular trace
- Understand the context in which log messages were generated
- Track requests across multiple services (in distributed systems)

The format is typically: `[trace-id]-[span-id]`

---

**Next:** [Hello World - ASP.NET Core](HelloWorld3-AspNetCore.md)

[Home](../README.md) | [Getting Started](./Getting-Started.md) | [Logging Levels](./Logging-Levels.md) | [Event IDs](./Event-Ids.md) | [Performance Testing](./Performance.md)
