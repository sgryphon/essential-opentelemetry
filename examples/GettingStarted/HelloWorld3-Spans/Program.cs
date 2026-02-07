using System.Diagnostics;
using Essential.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;

const string ServiceName = "HelloLogging";

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

// Create a parent span
using (var parentActivity = activitySource.StartActivity("ProcessOrder"))
{
    // Add attributes to provide context
    parentActivity?.SetTag("order.id", "12345");
    parentActivity?.SetTag("customer.id", "user@example.com");

    logger.LogInformation("Processing order 12345");

    // Create a child span for validation
    using (var validationActivity = activitySource.StartActivity("ValidateOrder"))
    {
        logger.LogInformation("Validating order");
        Thread.Sleep(50);
        validationActivity?.SetTag("validation.result", "success");
    }

    // Create a child span for payment processing
    using (var paymentActivity = activitySource.StartActivity("ProcessPayment"))
    {
        logger.LogInformation("Processing payment");
        Thread.Sleep(100);
        paymentActivity?.SetTag("payment.amount", 99.99);
        paymentActivity?.SetTag("payment.method", "credit_card");
    }

    // Create a child span for shipping
    using (var shippingActivity = activitySource.StartActivity("ArrangeShipping"))
    {
        logger.LogInformation("Arranging shipping");
        Thread.Sleep(75);
        shippingActivity?.SetTag("shipping.carrier", "FastShip");
        shippingActivity?.SetTag("shipping.method", "express");
    }

    logger.LogInformation("Order processed successfully");
}

// Force flush to ensure all telemetry is exported before exit
tracerProvider.ForceFlush();
