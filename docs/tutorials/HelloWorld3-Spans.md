# Hello World - Working with Spans

In this tutorial, you'll learn how to create nested spans, add attributes, and structure your traces to provide detailed insights into your application's behavior.

## What are Spans?

A span represents a single operation within a trace. Spans can be nested to represent parent-child relationships between operations. This allows you to:

- Break down complex operations into smaller, measurable units
- Understand the hierarchy of operations
- Identify which sub-operations contribute most to overall latency
- Add contextual information using attributes

## Create the Application

Update your `Program.cs` to include nested spans and attributes:

```csharp
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
```

## Run the Application

Build and run your application:

```bash
dotnet run
```

You should see output showing the hierarchy of spans:

```
[timestamp] INFO [trace-id-span-id]: Processing order 12345
[timestamp] INFO [trace-id-span-id]: Validating order
[timestamp] SPAN [ValidateOrder] [trace-id-span-id] 50ms
[timestamp] INFO [trace-id-span-id]: Processing payment
[timestamp] SPAN [ProcessPayment] [trace-id-span-id] 100ms
[timestamp] INFO [trace-id-span-id]: Arranging shipping
[timestamp] SPAN [ArrangeShipping] [trace-id-span-id] 75ms
[timestamp] INFO [trace-id-span-id]: Order processed successfully
[timestamp] SPAN [ProcessOrder] [trace-id-span-id] 225ms
```

**Screenshot placeholder:** _[Screenshot showing colored console output with parent and child spans, demonstrating the nested structure and timing information]_

## Understanding the Code

### 1. Creating Nested Spans

```csharp
using (var parentActivity = activitySource.StartActivity("ProcessOrder"))
{
    // Child span
    using (var childActivity = activitySource.StartActivity("ValidateOrder"))
    {
        // Work happens here
    }
}
```

When you create an activity while another activity is active, the new activity automatically becomes a child of the current activity. This creates a parent-child relationship that's preserved in the trace data.

### 2. Adding Attributes (Tags)

```csharp
parentActivity?.SetTag("order.id", "12345");
parentActivity?.SetTag("customer.id", "user@example.com");
```

Attributes (also called tags) add contextual information to your spans. They help you:

- Filter and search for specific traces
- Understand the parameters of an operation
- Debug issues by seeing the exact values used

> **Note:** The `?.` (null-conditional operator) is used because `StartActivity()` can return `null` if the ActivitySource is not enabled.

### 3. Understanding Span Timing

The output shows the duration of each span:

- `ValidateOrder`: 50ms
- `ProcessPayment`: 100ms
- `ArrangeShipping`: 75ms
- `ProcessOrder`: ~225ms (the sum of its children plus any overhead)

This makes it easy to identify which operations take the most time.

### 4. Trace Correlation

All the log messages and spans share the same trace ID, allowing you to see the complete picture of what happened during order processing.

## Best Practices for Spans

### 1. Use Meaningful Names

Choose span names that clearly describe the operation:

```csharp
// Good
activitySource.StartActivity("ValidateCustomerAddress")

// Less clear
activitySource.StartActivity("Validate")
```

### 2. Add Relevant Attributes

Include information that will help with debugging and analysis:

```csharp
activity?.SetTag("user.id", userId);
activity?.SetTag("database.query", query);
activity?.SetTag("http.status_code", 200);
```

### 3. Keep Span Granularity Appropriate

Don't create spans for every line of code. Focus on:

- Significant operations (database queries, API calls, business logic)
- Operations that might fail
- Operations you want to measure performance for

### 4. Handle Errors

When an error occurs, record it in the span:

```csharp
try
{
    // Operation that might fail
}
catch (Exception ex)
{
    activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
    logger.LogError(ex, "Operation failed");
    throw;
}
```

## Try It Yourself

Experiment with the code:

1. Add more nested spans to represent additional operations
2. Add different attributes to track various aspects of the operation
3. Add error handling and see how errors are reflected in the trace
4. Change the sleep durations to see how it affects the overall timing

## Next Steps

Now that you understand console applications with OpenTelemetry, move on to web applications: **[Hello World - ASP.NET Core](./HelloWorld4-AspNetCore.md)** to see how traces work in a web application context.

## Learn More

- [OpenTelemetry Semantic Conventions](https://opentelemetry.io/docs/specs/semconv/)
- [Activity and ActivitySource in .NET](https://learn.microsoft.com/dotnet/core/diagnostics/distributed-tracing-instrumentation-walkthroughs)
