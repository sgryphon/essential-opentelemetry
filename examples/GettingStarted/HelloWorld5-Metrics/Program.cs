using System.Diagnostics.Metrics;
using Essential.OpenTelemetry;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

const string ServiceName = "HelloWeb";

// Create a Meter for metrics
var meter = new Meter(ServiceName);

// Create metric instruments
var requestCounter = meter.CreateCounter<long>("app.requests", "requests", "Total number of requests");
var activeRequests = meter.CreateUpDownCounter<long>("app.active_requests", "requests", "Number of active requests");
var requestDuration = meter.CreateHistogram<double>("app.request_duration", "ms", "Request duration in milliseconds");

var builder = WebApplication.CreateBuilder(args);

// Configure OpenTelemetry with logging, tracing, and metrics
builder.Logging.ClearProviders();
builder
    .Services.AddOpenTelemetry()
    .WithLogging(logging =>
    {
        logging.AddColoredConsoleExporter();
    })
    .WithTracing(tracing =>
    {
        tracing.AddAspNetCoreInstrumentation().AddColoredConsoleExporter();
    })
    .WithMetrics(metrics =>
    {
        // Collect metrics from ASP.NET Core and our custom meter
        metrics
            .AddAspNetCoreInstrumentation()
            .AddMeter(ServiceName)
            .AddColoredConsoleExporter(options => { }, exportIntervalMilliseconds: 5000);
    });

var app = builder.Build();

// Define endpoints with metrics
app.MapGet("/", async (ILogger<Program> logger) =>
{
    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
    activeRequests.Add(1);
    
    try
    {
        logger.LogInformation("Processing request to root endpoint");
        
        // Simulate some work
        await Task.Delay(50);
        
        requestCounter.Add(1, new KeyValuePair<string, object?>("endpoint", "/"));
        return "Hello from OpenTelemetry with Metrics!";
    }
    finally
    {
        activeRequests.Add(-1);
        requestDuration.Record(stopwatch.ElapsedMilliseconds, new KeyValuePair<string, object?>("endpoint", "/"));
    }
});

app.MapGet("/greet/{name}", async (string name, ILogger<Program> logger) =>
{
    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
    activeRequests.Add(1);
    
    try
    {
        logger.LogInformation("Greeting {Name}", name);
        
        // Simulate some work
        await Task.Delay(75);
        
        requestCounter.Add(1, new KeyValuePair<string, object?>("endpoint", "/greet"));
        return $"Hello, {name}!";
    }
    finally
    {
        activeRequests.Add(-1);
        requestDuration.Record(stopwatch.ElapsedMilliseconds, new KeyValuePair<string, object?>("endpoint", "/greet"));
    }
});

app.MapGet("/slow", async (ILogger<Program> logger) =>
{
    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
    activeRequests.Add(1);
    
    try
    {
        logger.LogInformation("Processing slow request");
        
        // Simulate slow operation
        await Task.Delay(200);
        
        requestCounter.Add(1, new KeyValuePair<string, object?>("endpoint", "/slow"));
        return "This was a slow operation";
    }
    finally
    {
        activeRequests.Add(-1);
        requestDuration.Record(stopwatch.ElapsedMilliseconds, new KeyValuePair<string, object?>("endpoint", "/slow"));
    }
});

app.Run();
