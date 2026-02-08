# Hello World - ASP.NET Core

In this tutorial, you'll create an ASP.NET Core web application with OpenTelemetry. You'll discover that ASP.NET Core automatically creates traces for HTTP requests, making it easy to observe your web application.

## Create a New Web Application

Open a terminal or command prompt and create a new web application:

```bash
dotnet new web -n HelloWeb
cd HelloWeb
```

## Install Required Packages

Install the necessary NuGet packages:

```bash
dotnet add package OpenTelemetry.Extensions.Hosting
dotnet add package OpenTelemetry.Instrumentation.AspNetCore
dotnet add package Essential.OpenTelemetry.Exporter.ColoredConsole
```

These packages provide:

- **OpenTelemetry.Extensions.Hosting**: OpenTelemetry integration with the .NET hosting model
- **OpenTelemetry.Instrumentation.AspNetCore**: Automatic instrumentation for ASP.NET Core
- **Essential.OpenTelemetry.Exporter.ColoredConsole**: The colored console exporter for viewing telemetry

## Write the Code

Replace the contents of `Program.cs` with the following code:

```csharp
using Essential.OpenTelemetry;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// Configure OpenTelemetry with logging and tracing
builder.Logging.ClearProviders();
builder
    .Services.AddOpenTelemetry()
    .WithLogging(logging =>
    {
        logging.AddColoredConsoleExporter();
    })
    .WithTracing(tracing =>
    {
        // ASP.NET Core instrumentation automatically creates spans for HTTP requests
        tracing.AddAspNetCoreInstrumentation().AddColoredConsoleExporter();
    });

var app = builder.Build();

// Define a simple endpoint
app.MapGet("/", (ILogger<Program> logger) =>
{
    logger.LogInformation("Received request to root endpoint");
    return "Hello from OpenTelemetry!";
});

// Define another endpoint with a parameter
app.MapGet("/greet/{name}", (string name, ILogger<Program> logger) =>
{
    logger.LogInformation("Greeting {Name}", name);
    return $"Hello, {name}!";
});

app.Run();
```

## Run the Application

Build and run your application:

```bash
dotnet run
```

You should see output indicating the application is listening:

```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
```

## Test the Application

Open a web browser or use curl to make requests:

```bash
# In another terminal
curl http://localhost:5000
curl http://localhost:5000/greet/World
```

In your application console, you should see output similar to:

```
[timestamp] INFO [trace-id-span-id]: Received request to root endpoint
[timestamp] SPAN [HTTP GET /] [trace-id-span-id] 5ms
[timestamp] INFO [trace-id-span-id]: Greeting World
[timestamp] SPAN [HTTP GET /greet/{name}] [trace-id-span-id] 3ms
```

**Screenshot placeholder:** _[Screenshot showing colored console output with HTTP request spans and log messages, demonstrating automatic tracing of web requests]_

## Understanding the Code

### 1. ASP.NET Core Instrumentation

```csharp
tracing.AddAspNetCoreInstrumentation().AddColoredConsoleExporter();
```

The `AddAspNetCoreInstrumentation()` method automatically instruments your ASP.NET Core application. It creates a span for every HTTP request, capturing:

- HTTP method (GET, POST, etc.)
- URL path
- Status code
- Response time
- Request headers (optionally)

You don't need to manually create activities for HTTP requests – they're created automatically!

### 2. Logging Within Request Context

```csharp
app.MapGet("/", (ILogger<Program> logger) =>
{
    logger.LogInformation("Received request to root endpoint");
    return "Hello from OpenTelemetry!";
});
```

When you log messages during HTTP request processing, they automatically include the trace ID and span ID of the current HTTP request. This makes it easy to correlate logs with specific requests.

### 3. Minimal API Pattern

This example uses the minimal API pattern introduced in .NET 6. The `MapGet` method:

- Defines an HTTP endpoint
- Uses dependency injection to provide the logger
- Returns a response

## Exploring the Trace Data

Notice that each HTTP request generates a span with information about:

- **Operation name**: `HTTP GET /` or `HTTP GET /greet/{name}`
- **Timing**: How long the request took to process
- **Trace ID**: A unique identifier for this request
- **Span ID**: A unique identifier for this specific operation

The log messages include the same trace ID, making it easy to see which logs belong to which request.

## Understanding Automatic Instrumentation

ASP.NET Core instrumentation provides many benefits automatically:

- **Zero-code tracing**: HTTP requests are traced without manual instrumentation
- **Consistent span naming**: All HTTP spans follow a consistent naming pattern
- **Standard attributes**: HTTP method, URL, status codes are captured automatically
- **Error tracking**: Failed requests are marked as errors in the trace data

---

**Next:** [Adding Metrics](HelloWorld4-Metrics.md)

[Home](../README.md) | [Getting Started](./Getting-Started.md) | [Logging Levels](./Logging-Levels.md) | [Event IDs](./Event-Ids.md) | [Performance Testing](docs/Performance.md)
