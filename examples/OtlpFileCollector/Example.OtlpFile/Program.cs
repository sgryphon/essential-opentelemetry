// OtlpFile Exporter example for OpenTelemetry Collector verification
// This example outputs logs in OTLP file format that can be consumed by the OTel Collector
using System.Diagnostics;
using System.Net;
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

logger.DebugMessage();
logger.CriticalSystemFailure("payment-service");

// Structured logging with multiple attributes
logger.UserLoggedIn("Alice", IPAddress.Parse("fdbe:4f24:c288:3b1b::4"), DateTimeOffset.UtcNow);

// Log within an activity to include trace context
using (var activity = activitySource.StartActivity("OrderProcessing"))
{
    activity?.SetTag("order.id", "ORD-789");
    activity?.SetTag("order.amount", 150.00);

    logger.ProcessingOrder("ORD-789", 150.00m);
    logger.ResourceRunningLow("disk space");
}

// Exception logging example
try
{
    throw new InvalidOperationException("Simulated exception for testing");
}
catch (Exception ex)
{
    logger.OperationError(ex, "test-operation");
}
