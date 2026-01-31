using OpenTelemetry;

namespace Essential.OpenTelemetry.Exporter;

public abstract class ColoredConsoleExporter<T> : BaseExporter<T>
    where T : class
{
    protected ColoredConsoleOptions Options { get; private set; }

    protected ColoredConsoleExporter(ColoredConsoleOptions options)
    {
        this.Options = options ?? new ColoredConsoleOptions();
    }
}
