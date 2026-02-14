// Example application demonstrating JSONL Console Exporter for OpenTelemetry
using System.Diagnostics;
using Essential.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

#if NET10_0
const string ServiceName = "JsonlConsole-Net10";
const string FrameworkVersion = ".NET 10.0";
#elif NET9_0
const string ServiceName = "JsonlConsole-Net9";
const string FrameworkVersion = ".NET 9.0";
#elif NET8_0
const string ServiceName = "JsonlConsole-Net8";
const string FrameworkVersion = ".NET 8.0";
#else
const string ServiceName = "JsonlConsole";
const string FrameworkVersion = "Unknown";
#endif

var activitySource = new ActivitySource(ServiceName);

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.ClearProviders();
builder
    .Services.AddOpenTelemetry()
    .WithLogging(logging =>
    {
        logging.AddJsonlConsoleExporter();
    })
    .WithTracing(tracing =>
    {
        tracing.AddSource(ServiceName);
    });
var host = builder.Build();

var logger = host.Services.GetRequiredService<ILogger<Program>>();

// Example 1: Simple log message
logger.LogInformation("Application starting with {Framework}", FrameworkVersion);

// Example 2: Log with structured data
logger.LogInformation("User {UserName} logged in from {IpAddress}", "Alice", "192.168.1.100");

// Example 3: Log with different severity levels
logger.LogTrace("This is a trace message");
logger.LogDebug("Debug message with details");
logger.LogWarning("Warning: Resource usage at {Percentage}%", 85);
logger.LogError("Error processing request {RequestId}", "REQ-12345");

// Example 4: Log with event ID
var eventId = new EventId(1001, "UserLogin");
logger.Log(LogLevel.Information, eventId, "User login successful");

// Example 5: Log with activity/trace context
using (var activity = activitySource.StartActivity("ExampleOperation"))
{
    activity?.SetTag("operation.type", "demo");
    logger.LogInformation("Executing operation within trace context");
}

// Example 6: Log with exception
try
{
    throw new InvalidOperationException("Example exception for demonstration");
}
catch (Exception ex)
{
    logger.LogError(ex, "An error occurred while processing");
}

logger.LogInformation("Application completed successfully");

await host.StopAsync();
