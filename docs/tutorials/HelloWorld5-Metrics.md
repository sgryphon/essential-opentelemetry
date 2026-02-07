# Hello World - Adding Metrics

In this tutorial, you'll add custom metrics to your ASP.NET Core application. Metrics help you understand the behavior and performance of your application over time.

## What are Metrics?

Metrics are numerical measurements that help you understand your application's behavior and performance. Common types include:

- **Counters**: Track the number of times something happens (e.g., requests processed, errors)
- **Gauges**: Track a value that can go up or down (e.g., memory usage, active connections)
- **Histograms**: Track the distribution of values (e.g., request durations, response sizes)

## Update Your Web Application

Starting from the previous ASP.NET Core tutorial, update your `Program.cs`:

```csharp
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
```

## Run the Application

Build and run your application:

```bash
dotnet run
```

## Test the Application

Make several requests to generate metrics:

```bash
# In another terminal, make multiple requests
curl http://localhost:5000
curl http://localhost:5000/greet/Alice
curl http://localhost:5000/greet/Bob
curl http://localhost:5000/slow
curl http://localhost:5000
```

Every 5 seconds, you should see metrics output in the console:

```
[timestamp] METRIC [app.requests] 5s endpoint=/ count=2
[timestamp] METRIC [app.requests] 5s endpoint=/greet count=2
[timestamp] METRIC [app.requests] 5s endpoint=/slow count=1
[timestamp] METRIC [app.request_duration] 5s endpoint=/ min=50ms max=52ms avg=51ms
[timestamp] METRIC [app.request_duration] 5s endpoint=/greet min=75ms max=76ms avg=75.5ms
[timestamp] METRIC [app.request_duration] 5s endpoint=/slow min=200ms max=200ms avg=200ms
[timestamp] METRIC [app.active_requests] 5s count=0
```

**Screenshot placeholder:** _[Screenshot showing colored console output with metrics being exported every 5 seconds, including counters and histograms with different aggregations]_

## Understanding the Code

### 1. Creating a Meter

```csharp
var meter = new Meter(ServiceName);
```

A `Meter` is the entry point for creating metric instruments. The service name identifies your application.

### 2. Creating Metric Instruments

```csharp
var requestCounter = meter.CreateCounter<long>("app.requests", "requests", "Total number of requests");
var activeRequests = meter.CreateUpDownCounter<long>("app.active_requests", "requests", "Number of active requests");
var requestDuration = meter.CreateHistogram<double>("app.request_duration", "ms", "Request duration in milliseconds");
```

Three types of instruments are created:

- **Counter**: Monotonically increasing value (total requests)
- **UpDownCounter**: Can increase or decrease (active requests)
- **Histogram**: Records distribution of values (request durations)

### 3. Configuring Metrics Collection

```csharp
.WithMetrics(metrics =>
{
    metrics
        .AddAspNetCoreInstrumentation()
        .AddMeter(ServiceName)
        .AddColoredConsoleExporter(options =>
        {
            options.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = 5000;
        });
})
```

- `AddAspNetCoreInstrumentation()`: Collects built-in ASP.NET Core metrics
- `AddMeter(ServiceName)`: Collects metrics from your custom meter
- Export interval is set to 5 seconds (default is 60 seconds)

### 4. Recording Metrics

```csharp
requestCounter.Add(1, new KeyValuePair<string, object?>("endpoint", "/"));
activeRequests.Add(1);
requestDuration.Record(stopwatch.ElapsedMilliseconds, new KeyValuePair<string, object?>("endpoint", "/"));
```

- `requestCounter.Add(1)`: Increments the counter
- `activeRequests.Add(1)` and `activeRequests.Add(-1)`: Tracks active requests
- `requestDuration.Record()`: Records a histogram value
- Attributes like `endpoint` help you filter and group metrics

## Understanding Metric Types

### Counter

Tracks cumulative totals:

```csharp
requestCounter.Add(1);  // Increment by 1
requestCounter.Add(5);  // Increment by 5
```

Use counters for:
- Total requests processed
- Total errors encountered
- Total bytes transferred

### UpDownCounter

Tracks values that can increase or decrease:

```csharp
activeRequests.Add(1);   // New request started
activeRequests.Add(-1);  // Request completed
```

Use up-down counters for:
- Active connections
- Queue size
- Memory usage

### Histogram

Records distribution of values:

```csharp
requestDuration.Record(stopwatch.ElapsedMilliseconds);
```

Use histograms for:
- Request durations
- Response sizes
- Processing times

Histograms automatically calculate min, max, average, and percentiles.

## Metric Attributes (Dimensions)

Attributes (also called dimensions or labels) allow you to filter and group metrics:

```csharp
requestCounter.Add(1, new KeyValuePair<string, object?>("endpoint", "/"));
requestCounter.Add(1, new KeyValuePair<string, object?>("endpoint", "/greet"));
```

This allows you to see:
- Total requests across all endpoints
- Requests per endpoint
- Compare different endpoints

## Built-in ASP.NET Core Metrics

The `AddAspNetCoreInstrumentation()` provides several built-in metrics:

- `http.server.request.duration`: HTTP request durations
- `http.server.active_requests`: Number of active HTTP requests
- And many more...

You can view these alongside your custom metrics.

## Best Practices

### 1. Choose Meaningful Names

Use clear, descriptive names following OpenTelemetry conventions:

```csharp
// Good
meter.CreateCounter<long>("app.requests.total")

// Less clear
meter.CreateCounter<long>("counter1")
```

### 2. Add Units

Always specify units for clarity:

```csharp
meter.CreateHistogram<double>("app.request_duration", "ms")  // milliseconds
meter.CreateCounter<long>("app.bytes_sent", "bytes")
```

### 3. Use Attributes Wisely

Don't create too many unique attribute combinations (high cardinality):

```csharp
// Good - limited values
new KeyValuePair<string, object?>("endpoint", "/api/users")

// Bad - unlimited values (user IDs, timestamps, etc.)
new KeyValuePair<string, object?>("user_id", userId)
```

### 4. Consider Performance

Metric collection is lightweight, but avoid:
- Recording metrics in tight loops
- Creating many metric instruments
- Using high-cardinality attributes

## Try It Yourself

Experiment with the code:

1. Add more metric instruments to track different aspects of your application
2. Add attributes to categorize metrics (e.g., by HTTP method, status code)
3. Create custom endpoints that simulate different scenarios
4. Adjust the export interval to see metrics more or less frequently

## Congratulations!

You've completed the getting started tutorial series! You now know how to:

✅ Set up OpenTelemetry in console and web applications  
✅ Use the Essential OpenTelemetry colored console exporter  
✅ Implement logging with correlation IDs  
✅ Create and work with distributed traces  
✅ Add custom spans with attributes  
✅ Instrument ASP.NET Core applications  
✅ Collect and record custom metrics  

## Next Steps

Now that you understand the basics, explore:

- [Logging Levels](../Logging-Levels.md) - Understanding different log severity levels
- [Event IDs](../Event-Ids.md) - Using event IDs to categorize log messages
- [Correlation IDs](../Correlation-Ids.md) - Deep dive into trace and span IDs
- [OpenTelemetry Documentation](https://opentelemetry.io/docs/languages/net/) - Official OpenTelemetry .NET documentation

Consider connecting to real observability backends like:
- Jaeger (for traces)
- Prometheus (for metrics)
- Grafana Loki (for logs)
- Azure Monitor
- AWS X-Ray
- Google Cloud Trace

## Learn More

- [OpenTelemetry Metrics Concepts](https://opentelemetry.io/docs/concepts/signals/metrics/)
- [.NET Metrics](https://learn.microsoft.com/dotnet/core/diagnostics/metrics)
- [OpenTelemetry Semantic Conventions](https://opentelemetry.io/docs/specs/semconv/)
