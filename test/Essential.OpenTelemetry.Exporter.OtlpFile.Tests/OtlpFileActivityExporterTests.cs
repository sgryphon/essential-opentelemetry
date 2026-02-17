using System.Diagnostics;
using System.Text.Json;
using Essential.OpenTelemetry;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Xunit;

namespace Essential.OpenTelemetry.Exporter.OtlpFile.Tests;

[Collection("OtlpFileTests")]
public class OtlpFileActivityExporterTests
{
    [Fact]
    public void BasicTraceExportTest()
    {
        // Arrange
        var mockOutput = new MockConsole();
        var activitySourceName = "TestSource";
        using var tracerProvider = Sdk.CreateTracerProviderBuilder()
            .AddSource(activitySourceName)
            .AddOtlpFileExporter(configure =>
            {
                configure.Console = mockOutput;
            })
            .Build();

        // Act
        var activitySource = new ActivitySource(activitySourceName);
        using (var activity = activitySource.StartActivity("TestOperation"))
        {
            activity?.SetTag("test.key", "test.value");
        }

        tracerProvider?.ForceFlush();

        // Assert
        Assert.Single(mockOutput.Lines);
        var jsonLine = mockOutput.Lines[0];

        // Validate it's valid JSON
        var doc = JsonDocument.Parse(jsonLine);
        var root = doc.RootElement;

        // Check structure
        var resourceSpans = root.GetProperty("resourceSpans");
        Assert.Equal(1, resourceSpans.GetArrayLength());

        var scopeSpans = resourceSpans[0].GetProperty("scopeSpans");
        Assert.Equal(1, scopeSpans.GetArrayLength());

        var spans = scopeSpans[0].GetProperty("spans");
        Assert.Equal(1, spans.GetArrayLength());

        var span = spans[0];
        Assert.Equal("TestOperation", span.GetProperty("name").GetString());

        // Verify trace ID and span ID are hex strings
        var traceId = span.GetProperty("traceId").GetString();
        Assert.NotNull(traceId);
        Assert.Equal(32, traceId!.Length); // 16 bytes = 32 hex chars

        var spanId = span.GetProperty("spanId").GetString();
        Assert.NotNull(spanId);
        Assert.Equal(16, spanId!.Length); // 8 bytes = 16 hex chars
    }

    [Fact]
    public void TraceWithAttributesTest()
    {
        // Arrange
        var mockOutput = new MockConsole();
        var activitySourceName = "TestSource";
        using var tracerProvider = Sdk.CreateTracerProviderBuilder()
            .AddSource(activitySourceName)
            .AddOtlpFileExporter(configure =>
            {
                configure.Console = mockOutput;
            })
            .Build();

        // Act
        var activitySource = new ActivitySource(activitySourceName);
        using (var activity = activitySource.StartActivity("TestOperation"))
        {
            activity?.SetTag("string.key", "string.value");
            activity?.SetTag("int.key", 42);
            activity?.SetTag("bool.key", true);
            activity?.SetTag("double.key", 3.14);
        }

        tracerProvider?.ForceFlush();

        // Assert
        var doc = JsonDocument.Parse(mockOutput.Lines[0]);
        var span = doc
            .RootElement.GetProperty("resourceSpans")[0]
            .GetProperty("scopeSpans")[0]
            .GetProperty("spans")[0];

        var attributes = span.GetProperty("attributes")
            .EnumerateArray()
            .ToDictionary(x => x.GetProperty("key").GetString() ?? string.Empty);

        Assert.True(attributes.ContainsKey("string.key"));
        Assert.Equal(
            "string.value",
            attributes["string.key"].GetProperty("value").GetProperty("stringValue").GetString()
        );

        Assert.True(attributes.ContainsKey("int.key"));
        Assert.Equal(
            "42",
            attributes["int.key"].GetProperty("value").GetProperty("intValue").GetString()
        );

        Assert.True(attributes.ContainsKey("bool.key"));
        Assert.True(
            attributes["bool.key"].GetProperty("value").GetProperty("boolValue").GetBoolean()
        );

        Assert.True(attributes.ContainsKey("double.key"));
        Assert.Equal(
            3.14,
            attributes["double.key"].GetProperty("value").GetProperty("doubleValue").GetDouble()
        );
    }

    [Fact]
    public void TraceWithSpanKindTest()
    {
        // Arrange
        var mockOutput = new MockConsole();
        var activitySourceName = "TestSource";
        using var tracerProvider = Sdk.CreateTracerProviderBuilder()
            .AddSource(activitySourceName)
            .AddOtlpFileExporter(configure =>
            {
                configure.Console = mockOutput;
            })
            .Build();

        // Act
        var activitySource = new ActivitySource(activitySourceName);
        using (var activity = activitySource.StartActivity("TestOperation", ActivityKind.Client))
        {
            // Activity is automatically tracked
        }

        tracerProvider?.ForceFlush();

        // Assert
        var doc = JsonDocument.Parse(mockOutput.Lines[0]);
        var span = doc
            .RootElement.GetProperty("resourceSpans")[0]
            .GetProperty("scopeSpans")[0]
            .GetProperty("spans")[0];

        // SpanKind.Client = 3
        Assert.Equal(3, span.GetProperty("kind").GetInt32());
    }

    [Fact]
    public void TraceWithEventsTest()
    {
        // Arrange
        var mockOutput = new MockConsole();
        var activitySourceName = "TestSource";
        using var tracerProvider = Sdk.CreateTracerProviderBuilder()
            .AddSource(activitySourceName)
            .AddOtlpFileExporter(configure =>
            {
                configure.Console = mockOutput;
            })
            .Build();

        // Act
        var activitySource = new ActivitySource(activitySourceName);
        using (var activity = activitySource.StartActivity("TestOperation"))
        {
            activity?.AddEvent(
                new ActivityEvent(
                    "TestEvent",
                    tags: new ActivityTagsCollection { { "event.key", "event.value" } }
                )
            );
        }

        tracerProvider?.ForceFlush();

        // Assert
        var doc = JsonDocument.Parse(mockOutput.Lines[0]);
        var span = doc
            .RootElement.GetProperty("resourceSpans")[0]
            .GetProperty("scopeSpans")[0]
            .GetProperty("spans")[0];

        var events = span.GetProperty("events");
        Assert.Equal(1, events.GetArrayLength());

        var evt = events[0];
        Assert.Equal("TestEvent", evt.GetProperty("name").GetString());

        var eventAttributes = evt.GetProperty("attributes")
            .EnumerateArray()
            .ToDictionary(x => x.GetProperty("key").GetString() ?? string.Empty);

        Assert.True(eventAttributes.ContainsKey("event.key"));
        Assert.Equal(
            "event.value",
            eventAttributes["event.key"].GetProperty("value").GetProperty("stringValue").GetString()
        );
    }

    [Fact]
    public void TraceWithLinksTest()
    {
        // Arrange
        var mockOutput = new MockConsole();
        var activitySourceName = "TestSource";
        using var tracerProvider = Sdk.CreateTracerProviderBuilder()
            .AddSource(activitySourceName)
            .AddOtlpFileExporter(configure =>
            {
                configure.Console = mockOutput;
            })
            .Build();

        // Act
        var activitySource = new ActivitySource(activitySourceName);

        // Create a linked context
        var linkedContext = new ActivityContext(
            ActivityTraceId.CreateRandom(),
            ActivitySpanId.CreateRandom(),
            ActivityTraceFlags.Recorded
        );

        var links = new[] { new ActivityLink(linkedContext) };

        using (
            var activity = activitySource.StartActivity(
                "TestOperation",
                ActivityKind.Client,
                parentContext: default(ActivityContext),
                links: links
            )
        )
        {
            // Activity is automatically tracked
        }

        tracerProvider?.ForceFlush();

        // Assert
        var doc = JsonDocument.Parse(mockOutput.Lines[0]);
        var span = doc
            .RootElement.GetProperty("resourceSpans")[0]
            .GetProperty("scopeSpans")[0]
            .GetProperty("spans")[0];

        var spanLinks = span.GetProperty("links");
        Assert.Equal(1, spanLinks.GetArrayLength());

        var link = spanLinks[0];
        Assert.NotNull(link.GetProperty("traceId").GetString());
        Assert.NotNull(link.GetProperty("spanId").GetString());
    }

    [Fact]
    public void TraceWithStatusTest()
    {
        // Arrange
        var mockOutput = new MockConsole();
        var activitySourceName = "TestSource";
        using var tracerProvider = Sdk.CreateTracerProviderBuilder()
            .AddSource(activitySourceName)
            .AddOtlpFileExporter(configure =>
            {
                configure.Console = mockOutput;
            })
            .Build();

        // Act
        var activitySource = new ActivitySource(activitySourceName);
        using (var activity = activitySource.StartActivity("TestOperation"))
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Test error description");
        }

        tracerProvider?.ForceFlush();

        // Assert
        var doc = JsonDocument.Parse(mockOutput.Lines[0]);
        var span = doc
            .RootElement.GetProperty("resourceSpans")[0]
            .GetProperty("scopeSpans")[0]
            .GetProperty("spans")[0];

        var status = span.GetProperty("status");
        // StatusCode.Error = 2
        Assert.Equal(2, status.GetProperty("code").GetInt32());
        Assert.Equal("Test error description", status.GetProperty("message").GetString());
    }

    [Fact]
    public void MultipleScopesTest()
    {
        // Arrange
        var mockOutput = new MockConsole();
        var activitySourceName1 = "TestSource1";
        var activitySourceName2 = "TestSource2";
        using var tracerProvider = Sdk.CreateTracerProviderBuilder()
            .AddSource(activitySourceName1)
            .AddSource(activitySourceName2)
            .AddOtlpFileExporter(configure =>
            {
                configure.Console = mockOutput;
            })
            .Build();

        // Act
        var activitySource1 = new ActivitySource(activitySourceName1);
        var activitySource2 = new ActivitySource(activitySourceName2);

        using (var activity1 = activitySource1.StartActivity("Operation1"))
        {
            // Activity is automatically tracked
        }

        using (var activity2 = activitySource2.StartActivity("Operation2"))
        {
            // Activity is automatically tracked
        }

        tracerProvider?.ForceFlush();

        // Assert
        // Each activity creates a separate JSONL entry
        Assert.Equal(2, mockOutput.Lines.Count);

        // Verify first activity
        var doc1 = JsonDocument.Parse(mockOutput.Lines[0]);
        var scopeSpans1 = doc1
            .RootElement.GetProperty("resourceSpans")[0]
            .GetProperty("scopeSpans");
        Assert.Equal(1, scopeSpans1.GetArrayLength());

        var scope1 = scopeSpans1[0].GetProperty("scope");
        Assert.Equal(activitySourceName1, scope1.GetProperty("name").GetString());

        // Verify second activity
        var doc2 = JsonDocument.Parse(mockOutput.Lines[1]);
        var scopeSpans2 = doc2
            .RootElement.GetProperty("resourceSpans")[0]
            .GetProperty("scopeSpans");
        Assert.Equal(1, scopeSpans2.GetArrayLength());

        var scope2 = scopeSpans2[0].GetProperty("scope");
        Assert.Equal(activitySourceName2, scope2.GetProperty("name").GetString());
    }

    [Fact]
    public void ParentChildSpansTest()
    {
        // Arrange
        var mockOutput = new MockConsole();
        var activitySourceName = "TestSource";
        using var tracerProvider = Sdk.CreateTracerProviderBuilder()
            .AddSource(activitySourceName)
            .AddOtlpFileExporter(configure =>
            {
                configure.Console = mockOutput;
            })
            .Build();

        // Act
        var activitySource = new ActivitySource(activitySourceName);
        using (var parentActivity = activitySource.StartActivity("ParentOperation"))
        {
            using (var childActivity = activitySource.StartActivity("ChildOperation"))
            {
                // Child activity is automatically tracked
            }
        }

        tracerProvider?.ForceFlush();

        // Assert - Should have 2 spans exported
        Assert.Equal(2, mockOutput.Lines.Count);

        // Parse child span (first to complete)
        var childDoc = JsonDocument.Parse(mockOutput.Lines[0]);
        var childSpan = childDoc
            .RootElement.GetProperty("resourceSpans")[0]
            .GetProperty("scopeSpans")[0]
            .GetProperty("spans")[0];

        // Parse parent span
        var parentDoc = JsonDocument.Parse(mockOutput.Lines[1]);
        var parentSpan = parentDoc
            .RootElement.GetProperty("resourceSpans")[0]
            .GetProperty("scopeSpans")[0]
            .GetProperty("spans")[0];

        // Verify names
        Assert.Equal("ChildOperation", childSpan.GetProperty("name").GetString());
        Assert.Equal("ParentOperation", parentSpan.GetProperty("name").GetString());

        // Verify parent-child relationship
        var parentSpanId = parentSpan.GetProperty("spanId").GetString();
        var childParentSpanId = childSpan.GetProperty("parentSpanId").GetString();
        Assert.Equal(parentSpanId, childParentSpanId);

        // Verify they share the same trace ID
        var parentTraceId = parentSpan.GetProperty("traceId").GetString();
        var childTraceId = childSpan.GetProperty("traceId").GetString();
        Assert.Equal(parentTraceId, childTraceId);
    }
}
