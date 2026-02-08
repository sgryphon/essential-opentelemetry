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
app.MapGet(
    "/",
    (ILogger<Program> logger) =>
    {
        logger.LogInformation("Received request to root endpoint");
        return "Hello from OpenTelemetry!";
    }
);

// Define another endpoint with a parameter
app.MapGet(
    "/greet/{name}",
    (string name, ILogger<Program> logger) =>
    {
        logger.LogInformation("Greeting {Name}", name);
        return $"Hello, {name}!";
    }
);

app.Run();
