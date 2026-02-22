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

    [Fact]
    public void CheckTraceExporterAgainstCollector()
    {
        // Arrange
        var mockOutput = new MockConsole();
        var activitySourceName = "Test.OtlpFile.Source";
        using var tracerProvider = Sdk.CreateTracerProviderBuilder()
            .SetResourceBuilder(
                ResourceBuilder
                    .CreateDefault()
                    .AddService("test-service-name", serviceVersion: "v1.2.3-test")
                    .AddAttributes(
                        new KeyValuePair<string, object>[]
                        {
                            new("host.name", "test-host"),
                            new("deployment.environment.name", "test"),
                        }
                    )
            )
            .AddSource(activitySourceName)
            .AddOtlpFileExporter(configure =>
            {
                configure.Console = mockOutput;
            })
            .Build();

        // Act
        var activitySource = new ActivitySource(activitySourceName);

        // Outer Span
        using (var activity = activitySource.StartActivity("OrderProcessing"))
        {
            activity?.SetTag("order.id", "ORD-789");
            activity?.AddBaggage("Baggage1", "One");

            var eventTags = new ActivityTagsCollection() { { "ActivityTag", 150 } };
            activity?.AddEvent(new ActivityEvent("ActivityEvent", tags: eventTags));

            // Inner Span
            using (
                var innerActivity = activitySource.StartActivity(
                    "InnerActivity",
                    kind: ActivityKind.Client
                )
            )
            {
                var linkTags = new ActivityTagsCollection() { { "LinkTag", "LINK" } };
                innerActivity?.AddLink(new ActivityLink(activity!.Context, linkTags));

                try
                {
                    throw new InvalidOperationException("Simulated activity exception for testing");
                }
                catch (Exception ex)
                {
                    var exceptionTags = new TagList() { { "LinkTag", "LINK" } };
                    innerActivity?.AddException(ex, tags: exceptionTags);
                }

                innerActivity?.SetStatus(ActivityStatusCode.Error, "Status error");
            }
        }

        tracerProvider?.ForceFlush();

        // Assert
        var innerLine = mockOutput.Lines[0];

        Console.WriteLine("OUTPUT 0: " + innerLine);

        // Validate it's valid JSON
        var innerSpanDoc = JsonDocument.Parse(innerLine);

        // Check against sample output from OpenTelemetry Collector
        // This was generated by configuring the Collector with an OTLP receiver and File exporter,
        // sending using the OtlpExporter, and then checking the file output

        // {
        //   "resourceSpans": [
        var innerResourceSpans = innerSpanDoc.RootElement.GetProperty("resourceSpans");
        Assert.Equal(1, innerResourceSpans.GetArrayLength());

        //     {
        //       "resource": {
        //         "attributes": [
        var innerResourceAttributes = innerResourceSpans[0]
            .GetProperty("resource")
            .GetProperty("attributes")
            .EnumerateArray()
            .ToDictionary(x => x.GetProperty("key").ToString());

        //           { "key": "host.name", "value": { "stringValue": "TAR-VALON" } },
        Assert.Equal(
            "test-host",
            innerResourceAttributes["host.name"]
                .GetProperty("value")
                .GetProperty("stringValue")
                .GetString()
        );

        //           {
        //             "key": "deployment.environment.name",
        //             "value": { "stringValue": "production" }
        //           },
        Assert.Equal(
            "test",
            innerResourceAttributes["deployment.environment.name"]
                .GetProperty("value")
                .GetProperty("stringValue")
                .GetString()
        );

        //           {
        //             "key": "service.name",
        //             "value": { "stringValue": "Example.OtlpFile" }
        //           },
        Assert.Equal(
            "test-service-name",
            innerResourceAttributes["service.name"]
                .GetProperty("value")
                .GetProperty("stringValue")
                .GetString()
        );

        //           {
        //             "key": "service.version",
        //             "value": {
        //               "stringValue": "1.0.0+d40ee8c350d543a91f3bfabebe40e61b30c7508d"
        //             }
        //           },
        Assert.Equal(
            "v1.2.3-test",
            innerResourceAttributes["service.version"]
                .GetProperty("value")
                .GetProperty("stringValue")
                .GetString()
        );

        //           {
        //             "key": "service.instance.id",
        //             "value": { "stringValue": "e960c336-0a1f-4d58-b53f-589747299687" }
        //           },
        //           {
        //             "key": "telemetry.sdk.name",
        //             "value": { "stringValue": "opentelemetry" }
        //           },
        Assert.Equal(
            "opentelemetry",
            innerResourceAttributes["telemetry.sdk.name"]
                .GetProperty("value")
                .GetProperty("stringValue")
                .GetString()
        );

        //           {
        //             "key": "telemetry.sdk.language",
        //             "value": { "stringValue": "dotnet" }
        //           },
        Assert.Equal(
            "dotnet",
            innerResourceAttributes["telemetry.sdk.language"]
                .GetProperty("value")
                .GetProperty("stringValue")
                .GetString()
        );

        //           {
        //             "key": "telemetry.sdk.version",
        //             "value": { "stringValue": "1.15.0" }
        //           }
        //         ]
        //       },

        //       "scopeSpans": [
        var innerScopeSpans = innerResourceSpans[0].GetProperty("scopeSpans");
        Assert.Equal(1, innerScopeSpans.GetArrayLength());

        //         {
        //           "scope": { "name": "Example.OtlpFile" },
        Assert.Equal(
            "Test.OtlpFile.Source",
            innerScopeSpans[0].GetProperty("scope").GetProperty("name").GetString()
        );

        //           "spans": [
        var innerSpans = innerScopeSpans[0].GetProperty("spans");
        Assert.Equal(1, innerSpans.GetArrayLength());

        // -----------------------------------------------------------------------
        // Inner Span finishes first
        var innerSpan = innerSpans[0];

        //             {
        //               "traceId": "16951e3c199f2c55780bfbe6e32fb70f",
        Assert.True(
            innerSpan.TryGetProperty("traceId", out var innerTraceIdElement),
            "traceId should be present"
        );
        Assert.Matches("^[0-9a-f]{32}$", innerTraceIdElement.GetString());

        //               "spanId": "9c69499f6471c5c4",
        Assert.True(
            innerSpan.TryGetProperty("spanId", out var innerSpanIdElement),
            "spanId should be present"
        );
        Assert.Matches("^[0-9a-f]{16}$", innerSpanIdElement.GetString());

        //               "parentSpanId": "77a5e18b65f6f0dd",
        Assert.True(
            innerSpan.TryGetProperty("parentSpanId", out var innerParentSpanIdElement),
            "parentSpanId should be present"
        );

        //               "flags": 257,
        //               "name": "InnerActivity",
        Assert.Equal("InnerActivity", innerSpan.GetProperty("name").GetString());

        //               "kind": 1,
        Assert.Equal((int)SpanKind.Client, innerSpan.GetProperty("kind").GetInt32());

        //               "startTimeUnixNano": "1771654044393311500",
        //               "endTimeUnixNano": "1771654044433891700",
        //               "events": [
        var innerEvents = innerSpan.GetProperty("events");
        Assert.Equal(1, innerEvents.GetArrayLength());

        //                 {
        //                   "timeUnixNano": "1771654044433602500",
        //                   "name": "exception",
        Assert.Equal("exception", innerEvents[0].GetProperty("name").GetString());

        //                   "attributes": [
        var innerEventAttributes = innerEvents[0]
            .GetProperty("attributes")
            .EnumerateArray()
            .ToDictionary(x => x.GetProperty("key").ToString());

        //                     { "key": "LinkTag", "value": { "stringValue": "LINK" } },
        Assert.Equal(
            "LINK",
            innerEventAttributes["LinkTag"]
                .GetProperty("value")
                .GetProperty("stringValue")
                .GetString()
        );

        //                     {
        //                       "key": "exception.message",
        //                       "value": {
        //                         "stringValue": "Simulated activity exception for testing"
        //                       }
        //                     },
        Assert.Equal(
            "Simulated activity exception for testing",
            innerEventAttributes["exception.message"]
                .GetProperty("value")
                .GetProperty("stringValue")
                .GetString()
        );

        //                     {
        //                       "key": "exception.stacktrace",
        //                       "value": {
        //                         "stringValue": "System.InvalidOperationException: Simulated activity exception for testing\r\n   at Program.<Main>$(String[] args) in C:\\Code\\essential-opentelemetry\\examples\\OtlpFileCollector\\Example.OtlpFile\\Program.cs:line 150"
        //                       }
        //                     },
        Assert.StartsWith(
            "System.InvalidOperationException: Simulated activity exception for testing",
            innerEventAttributes["exception.stacktrace"]
                .GetProperty("value")
                .GetProperty("stringValue")
                .GetString()
        );

        //                     {
        //                       "key": "exception.type",
        //                       "value": {
        //                         "stringValue": "System.InvalidOperationException"
        //                       }
        //                     }
        Assert.Equal(
            "System.InvalidOperationException",
            innerEventAttributes["exception.type"]
                .GetProperty("value")
                .GetProperty("stringValue")
                .GetString()
        );

        //                   ]
        //                 }
        //               ],
        //               "links": [
        var innerLinks = innerSpan.GetProperty("links");
        Assert.Equal(1, innerEvents.GetArrayLength());

        //                 {
        //                   "traceId": "16951e3c199f2c55780bfbe6e32fb70f",
        Assert.Equal(
            innerTraceIdElement.GetString(),
            innerLinks[0].GetProperty("traceId").GetString()
        );

        //                   "spanId": "77a5e18b65f6f0dd",
        //                   "attributes": [
        //                     { "key": "LinkTag", "value": { "stringValue": "LINK" } }
        //                   ],
        Assert.Equal(
            "LINK",
            innerLinks[0]
                .GetProperty("attributes")[0]
                .GetProperty("value")
                .GetProperty("stringValue")
                .GetString()
        );

        //                   "flags": 257
        //                 }
        //               ],
        //               "status": { "message": "Status error", "code": 2 }
        //             },
        Assert.Equal(
            "Status error",
            innerSpan.GetProperty("status").GetProperty("message").GetString()
        );
        Assert.Equal(
            (int)StatusCode.Error,
            innerSpan.GetProperty("status").GetProperty("code").GetInt32()
        );

        // -----------------------------------------------------------------------
        // Outer Span finishes second

        var outerLine = mockOutput.Lines[1];
        var outerSpanDoc = JsonDocument.Parse(outerLine);
        var outerResourceSpans = outerSpanDoc.RootElement.GetProperty("resourceSpans");

        var outerResourceAttributes = outerResourceSpans[0]
            .GetProperty("resource")
            .GetProperty("attributes")
            .EnumerateArray()
            .ToDictionary(x => x.GetProperty("key").ToString());
        Assert.Equal(
            "test-service-name",
            innerResourceAttributes["service.name"]
                .GetProperty("value")
                .GetProperty("stringValue")
                .GetString()
        );

        var outerScopeSpans = outerResourceSpans[0].GetProperty("scopeSpans");
        Assert.Equal(
            "Test.OtlpFile.Source",
            outerScopeSpans[0].GetProperty("scope").GetProperty("name").GetString()
        );

        var outerSpans = outerScopeSpans[0].GetProperty("spans");

        var outerSpan = outerSpans[0];

        // {
        //   "traceId": "16951e3c199f2c55780bfbe6e32fb70f",
        Assert.Equal(innerTraceIdElement.GetString(), outerSpan.GetProperty("traceId").GetString());

        //   "spanId": "77a5e18b65f6f0dd",
        // Should match parent span of inner
        Assert.Equal(
            innerParentSpanIdElement.GetString(),
            outerSpan.GetProperty("spanId").GetString()
        );

        //   "flags": 257,
        //   "name": "OrderProcessing",
        Assert.Equal("OrderProcessing", outerSpan.GetProperty("name").GetString());

        //   "kind": 1,
        Assert.Equal((int)SpanKind.Internal, outerSpan.GetProperty("kind").GetInt32());

        //   "startTimeUnixNano": "1771654044359652000",
        //   "endTimeUnixNano": "1771654044452814500",
        //   "attributes": [
        //     { "key": "order.id", "value": { "stringValue": "ORD-789" } }
        Assert.Equal(
            "order.id",
            outerSpan.GetProperty("attributes")[0].GetProperty("key").GetString()
        );
        Assert.Equal(
            "ORD-789",
            outerSpan
                .GetProperty("attributes")[0]
                .GetProperty("value")
                .GetProperty("stringValue")
                .GetString()
        );

        //   ],
        //   "events": [
        //     {
        //       "timeUnixNano": "1771654044392496100",
        //       "name": "ActivityEvent",
        Assert.Equal(
            "ActivityEvent",
            outerSpan.GetProperty("events")[0].GetProperty("name").GetString()
        );

        //       "attributes": [
        //         { "key": "ActivityTag", "value": { "intValue": "150" } }
        Assert.Equal(
            "ActivityTag",
            outerSpan
                .GetProperty("events")[0]
                .GetProperty("attributes")[0]
                .GetProperty("key")
                .GetString()
        );
        Assert.Equal(
            "150",
            outerSpan
                .GetProperty("events")[0]
                .GetProperty("attributes")[0]
                .GetProperty("value")
                .GetProperty("intValue")
                .GetString()
        );

        //       ]
        //     }
        //   ],
        //   "status": {}
        // }
    }
}
