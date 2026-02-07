using Essential.OpenTelemetry.Exporter;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Metrics;

namespace Essential.OpenTelemetry;

public static class ColoredConsoleMetricsExtensions
{
    private const int DefaultExportIntervalMilliseconds = 10000;
    private const int DefaultExportTimeoutMilliseconds = Timeout.Infinite;

    /// <summary>
    /// Adds Console exporter to the MeterProvider.
    /// </summary>
    /// <param name="builder"><see cref="MeterProviderBuilder"/> builder to use.</param>
    /// <returns>The instance of <see cref="MeterProviderBuilder"/> to chain the calls.</returns>
    public static MeterProviderBuilder AddColoredConsoleExporter(
        this MeterProviderBuilder builder
    ) => AddColoredConsoleExporter(builder, name: null, configure: null);

    /// <summary>
    /// Adds Console exporter to the MeterProvider.
    /// </summary>
    /// <param name="builder"><see cref="MeterProviderBuilder"/> builder to use.</param>
    /// <param name="configure">Callback action for configuring <see cref="ColoredConsoleOptions"/>.</param>
    /// <returns>The instance of <see cref="MeterProviderBuilder"/> to chain the calls.</returns>
    public static MeterProviderBuilder AddColoredConsoleExporter(
        this MeterProviderBuilder builder,
        Action<ColoredConsoleOptions> configure
    ) => AddColoredConsoleExporter(builder, name: null, configure);

    /// <summary>
    /// Adds Console exporter to the MeterProvider.
    /// </summary>
    /// <param name="builder"><see cref="MeterProviderBuilder"/> builder to use.</param>
    /// <param name="name">Optional name which is used when retrieving options.</param>
    /// <param name="configure">Optional callback action for configuring <see cref="ColoredConsoleOptions"/>.</param>
    /// <returns>The instance of <see cref="MeterProviderBuilder"/> to chain the calls.</returns>
    public static MeterProviderBuilder AddColoredConsoleExporter(
        this MeterProviderBuilder builder,
        string? name,
        Action<ColoredConsoleOptions>? configure
    )
    {
        if (builder == null)
            throw new ArgumentNullException(nameof(builder));

        name ??= Options.DefaultName;

        if (configure != null)
        {
            builder.ConfigureServices(services => services.Configure(name, configure));
        }

        return builder.AddReader(sp =>
        {
            var options = sp.GetRequiredService<IOptionsMonitor<ColoredConsoleOptions>>().Get(name);

            var exporter = new ColoredConsoleMetricExporter(options);
            return new PeriodicExportingMetricReader(
                exporter,
                DefaultExportIntervalMilliseconds,
                DefaultExportTimeoutMilliseconds
            );
        });
    }

    /// <summary>
    /// Adds Console exporter to the MeterProvider with configurable export interval.
    /// </summary>
    /// <param name="builder"><see cref="MeterProviderBuilder"/> builder to use.</param>
    /// <param name="configure">Callback action for configuring <see cref="ColoredConsoleOptions"/>.</param>
    /// <param name="exportIntervalMilliseconds">The export interval in milliseconds.</param>
    /// <returns>The instance of <see cref="MeterProviderBuilder"/> to chain the calls.</returns>
    public static MeterProviderBuilder AddColoredConsoleExporter(
        this MeterProviderBuilder builder,
        Action<ColoredConsoleOptions> configure,
        int exportIntervalMilliseconds
    )
    {
        if (builder == null)
            throw new ArgumentNullException(nameof(builder));

        var name = Options.DefaultName;

        if (configure != null)
        {
            builder.ConfigureServices(services => services.Configure(name, configure));
        }

        return builder.AddReader(sp =>
        {
            var options = sp.GetRequiredService<IOptionsMonitor<ColoredConsoleOptions>>().Get(name);

            var exporter = new ColoredConsoleMetricExporter(options);
            return new PeriodicExportingMetricReader(
                exporter,
                exportIntervalMilliseconds,
                DefaultExportTimeoutMilliseconds
            );
        });
    }
}
