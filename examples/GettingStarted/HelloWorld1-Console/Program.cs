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
