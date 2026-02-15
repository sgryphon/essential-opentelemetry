// JSONL Console Exporter example for OpenTelemetry Collector verification
// This example outputs logs in JSONL format that can be consumed by the OTel Collector
using System.Diagnostics;
using Essential.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

#if NET10_0
const string ServiceName = "JsonlCollector-Net10";
const string FrameworkVersion = ".NET 10.0";
#elif NET9_0
const string ServiceName = "JsonlCollector-Net9";
const string FrameworkVersion = ".NET 9.0";
#elif NET8_0
const string ServiceName = "JsonlCollector-Net8";
const string FrameworkVersion = ".NET 8.0";
#else
const string ServiceName = "JsonlCollector";
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

// Output various log levels and structured logging examples
logger.LogInformation("Application starting on {Framework}", FrameworkVersion);

// Log with different severity levels
logger.LogTrace("This is a trace message for detailed debugging");
logger.LogDebug("Debug message with {Count} items", 5);
logger.LogInformation("Information message about {Operation}", "data processing");
logger.LogWarning("Warning: {Resource} is running low", "disk space");
logger.LogError("Error occurred while processing {RequestId}", "req-12345");
logger.LogCritical("Critical system failure in {Component}", "payment-service");

// Structured logging with multiple attributes
logger.LogInformation(
    "User {UserName} logged in from {IpAddress} at {Timestamp}",
    "Alice",
    "192.168.1.100",
    DateTime.UtcNow
);

// Log within an activity to include trace context
using (var activity = activitySource.StartActivity("OrderProcessing"))
{
    activity?.SetTag("order.id", "ORD-789");
    activity?.SetTag("order.amount", 150.00);

    logger.LogInformation("Processing order {OrderId} for {Amount:C}", "ORD-789", 150.00m);
    logger.LogDebug("Validating order items");
    logger.LogInformation("Order {OrderId} completed successfully", "ORD-789");
}

// Exception logging example
try
{
    throw new InvalidOperationException("Simulated exception for testing");
}
catch (Exception ex)
{
    logger.LogError(ex, "Exception caught while processing {Operation}", "test-operation");
}

logger.LogInformation("Application completed successfully");
