// JSONL Console Exporter example for OpenTelemetry Collector verification
// This example outputs logs in JSONL format that can be consumed by the OTel Collector
using System.Diagnostics;
using Essential.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

const string ServiceName = "Example.OtlpFile";

var activitySource = new ActivitySource(ServiceName);

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.ClearProviders();
builder
    .Services.AddOpenTelemetry()
    .WithLogging(logging =>
    {
        logging.AddOtlpFileExporter();
    })
    .WithTracing(tracing =>
    {
        tracing.AddSource(ServiceName);
    });

var host = builder.Build();
var logger = host.Services.GetRequiredService<ILogger<Program>>();

// Log with different severity levels
logger.LogTrace("This is a trace message for detailed debugging");
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
    logger.LogWarning("Warning: {Resource} is running low", "disk space");
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
