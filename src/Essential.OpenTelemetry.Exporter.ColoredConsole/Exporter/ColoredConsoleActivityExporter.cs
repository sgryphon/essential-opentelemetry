using System.Diagnostics;
using System.Globalization;
using Essential.System;
using OpenTelemetry;

namespace Essential.OpenTelemetry.Exporter;

public class ColoredConsoleActivityExporter : ColoredConsoleExporter<Activity>
{
    private const ConsoleColor SpanForeground = ConsoleColor.DarkCyan;
    private const ConsoleColor SpanBackground = ConsoleColor.Black;
    private const string SpanText = "SPAN";

    private const ConsoleColor ErrorForeground = ConsoleColor.Black;
    private const ConsoleColor ErrorBackground = ConsoleColor.DarkRed;
    private const string ErrorText = "ERROR";

    public ColoredConsoleActivityExporter(ColoredConsoleOptions options)
        : base(options) { }

    /// <inheritdoc/>
    public override ExportResult Export(in Batch<Activity> batch)
    {
        var console = this.Options.Console;

        foreach (var activity in batch)
        {
            // Build the timestamp (if configured)
            var spanEnd = activity.StartTimeUtc + activity.Duration;
            var timestamp = string.Empty;
            if (!string.IsNullOrEmpty(this.Options.TimestampFormat))
            {
                var timestampToFormat = this.Options.UseUtcTimestamp
                    ? spanEnd.ToUniversalTime()
                    : spanEnd.ToLocalTime();

                timestamp = timestampToFormat.ToString(
                    this.Options.TimestampFormat!,
                    CultureInfo.InvariantCulture
                );
            }

            var isError = activity.Status == ActivityStatusCode.Error;

            // Build the first line details
            var activityDetails = string.Empty;
            activityDetails += $" [{activity.OperationName}]";

            // Output trace ID & span ID
            if (activity.TraceId != default)
            {
                var traceIdHex = activity.TraceId.ToHexString();
                activityDetails += $" {traceIdHex}";

                if (activity.SpanId != default)
                {
                    activityDetails += $"-{activity.SpanId.ToHexString()}";
                }
            }

            // Duration (in milliseconds)
            activityDetails += $" {activity.Duration.TotalMilliseconds:N0}ms";

            lock (console.SyncRoot)
            {
                // Output the line, starting with timestamp (if specified)
                console.Write(timestamp);

                // Write severity in color, then rest of the line in default color
                console.WriteColor(SpanText, SpanForeground, SpanBackground);

                if (isError)
                {
                    console.Write(" ");
                    console.WriteColor(ErrorText, ErrorForeground, ErrorBackground);
                }

                console.WriteLine(activityDetails);
            }
        }

        return ExportResult.Success;
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
    }
}
