# Screenshot Placeholders for Getting Started Tutorials

The following tutorial pages have placeholder text where screenshots should be inserted. Below are descriptions of what each screenshot should show:

## HelloWorld1-Console.md

**Location:** After "You should see output similar to this (with colors in your terminal):"

**Screenshot should show:**

- Console output with three log messages
- INFO level message in one color (typically cyan/blue)
- WARN level message in another color (typically yellow/orange)
- ERROR level message in another color (typically red)
- Each line showing timestamp, log level, and message
- Example output:
  ```
  06:12:18 INFO: Hello World!
  06:12:18 WARN: This is a warning message
  06:12:18 ERROR: This is an error message
  ```

## HelloWorld2-Traces.md

**Location:** After "You should see output similar to this:"

**Screenshot should show:**

- Console output with log messages and span information
- All items sharing the same trace ID (first part of the correlation ID)
- Different span IDs (second part of the correlation ID)
- SPAN line showing timing information
- Example output:
  ```
  06:12:19 INFO b32004fb23931c4cee4ad002a7e11c19-a0093f03d9bee5eb: Starting main operation
  06:12:19 INFO b32004fb23931c4cee4ad002a7e11c19-a0093f03d9bee5eb: Main operation completed
  06:12:19 SPAN [MainOperation] b32004fb23931c4cee4ad002a7e11c19-a0093f03d9bee5eb 118ms
  ```

## HelloWorld3-Spans.md

**Location:** After "You should see output showing the hierarchy of spans:"

**Screenshot should show:**

- Console output with parent and child spans
- Multiple INFO log messages with correlation IDs
- Multiple SPAN entries showing the hierarchy (ValidateOrder, ProcessPayment, ArrangeShipping, ProcessOrder)
- All sharing the same trace ID but different span IDs
- Timing information for each span (50ms, 100ms, 75ms, ~225ms)
- Example output:
  ```
  06:12:34 INFO 792272893d06373857e1ce1262b66028-6b2c9923a180dcfc: Processing order 12345
  06:12:34 INFO 792272893d06373857e1ce1262b66028-ebc6cd883a366146: Validating order
  06:12:34 SPAN [ValidateOrder] 792272893d06373857e1ce1262b66028-ebc6cd883a366146 51ms
  06:12:34 INFO 792272893d06373857e1ce1262b66028-53ee6b74e8a573d0: Processing payment
  06:12:34 SPAN [ProcessPayment] 792272893d06373857e1ce1262b66028-53ee6b74e8a573d0 100ms
  06:12:34 INFO 792272893d06373857e1ce1262b66028-1a96072751ab0e0f: Arranging shipping
  06:12:34 SPAN [ArrangeShipping] 792272893d06373857e1ce1262b66028-1a96072751ab0e0f 75ms
  06:12:34 INFO 792272893d06373857e1ce1262b66028-6b2c9923a180dcfc: Order processed successfully
  06:12:34 SPAN [ProcessOrder] 792272893d06373857e1ce1262b66028-6b2c9923a180dcfc 243ms
  ```

## HelloWorld4-AspNetCore.md

**Location:** After "In your application console, you should see output similar to:"

**Screenshot should show:**

- Console output from ASP.NET Core application
- INFO log messages with correlation IDs from HTTP requests
- SPAN entries for HTTP GET requests
- Example showing requests to "/" and "/greet/World"
- Timing information for HTTP requests
- Example output:
  ```
  06:11:32 INFO [ListeningOnAddress]: Now listening on: http://localhost:5000
  [after making requests]
  [timestamp] INFO [trace-id-span-id]: Received request to root endpoint
  [timestamp] SPAN [HTTP GET /] [trace-id-span-id] 5ms
  [timestamp] INFO [trace-id-span-id]: Greeting World
  [timestamp] SPAN [HTTP GET /greet/{name}] [trace-id-span-id] 3ms
  ```

## HelloWorld5-Metrics.md

**Location:** After "Every 5 seconds, you should see metrics output in the console:"

**Screenshot should show:**

- Console output with metric exports
- METRIC entries showing counters and histograms
- Different endpoints (/, /greet, /slow) with their respective metrics
- Request counts, durations (min, max, avg), and active request counts
- Example output:
  ```
  [timestamp] METRIC [app.requests] 5s endpoint=/ count=2
  [timestamp] METRIC [app.requests] 5s endpoint=/greet count=2
  [timestamp] METRIC [app.requests] 5s endpoint=/slow count=1
  [timestamp] METRIC [app.request_duration] 5s endpoint=/ min=50ms max=52ms avg=51ms
  [timestamp] METRIC [app.request_duration] 5s endpoint=/greet min=75ms max=76ms avg=75.5ms
  [timestamp] METRIC [app.request_duration] 5s endpoint=/slow min=200ms max=200ms avg=200ms
  [timestamp] METRIC [app.active_requests] 5s count=0
  ```

## Notes for Screenshot Capture

- Screenshots should be captured from a real terminal with ANSI color support
- Ideally use a terminal with a dark background for better contrast
- Ensure the colored console exporter is displaying colors correctly:
  - INFO/Debug logs in cyan/blue
  - WARN logs in yellow/orange
  - ERROR logs in red
  - SPAN entries in green
  - METRIC entries in magenta/purple
- Capture enough context to show the flow but keep screenshots readable
- Consider using a terminal with a larger font size for better readability in documentation

## Alternative: Text with Color Descriptions

If screenshots cannot be easily captured with colors, the current placeholder text with descriptions of the colors in brackets is acceptable, for example:

```
[Blue] 06:12:18 INFO: Hello World!
[Yellow] 06:12:18 WARN: This is a warning message
[Red] 06:12:18 ERROR: This is an error message
```
