using Essential.OpenTelemetry.Exporter;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenTelemetry.Metrics;

namespace Essential.OpenTelemetry;

public static class ColoredConsoleMetricsExtensions
{
    private const int DefaultExportIntervalMilliseconds = 60_000;
    private const int DefaultExportTimeoutMilliseconds = 30_000;

    /// <summary>
    /// Adds ColoredConsole exporter to the MeterProvider.
    /// </summary>
    /// <param name="builder"><see cref="MeterProviderBuilder"/> builder to use.</param>
    /// <returns>The instance of <see cref="MeterProviderBuilder"/> to chain the calls.</returns>
    public static MeterProviderBuilder AddColoredConsoleExporter(
        this MeterProviderBuilder builder
    ) =>
        AddColoredConsoleExporter(
            builder,
            name: null,
            configureExporter: null,
            configurePeriodicReader: null
        );

    /// <summary>
    /// Adds ColoredConsole exporter to the MeterProvider.
    /// </summary>
    /// <param name="builder"><see cref="MeterProviderBuilder"/> builder to use.</param>
    /// <param name="configure">Callback action for configuring <see cref="ColoredConsoleOptions"/>.</param>
    /// <returns>The instance of <see cref="MeterProviderBuilder"/> to chain the calls.</returns>
    public static MeterProviderBuilder AddColoredConsoleExporter(
        this MeterProviderBuilder builder,
        Action<ColoredConsoleOptions> configure
    ) => AddColoredConsoleExporter(builder, name: null, configure, configurePeriodicReader: null);

    /// <summary>
    /// Adds ColoredConsole exporter to the MeterProvider.
    /// </summary>
    /// <param name="builder"><see cref="MeterProviderBuilder"/> builder to use.</param>
    /// <param name="name">Optional name which is used when retrieving options.</param>
    /// <param name="configure">Optional callback action for configuring <see cref="ColoredConsoleOptions"/>.</param>
    /// <returns>The instance of <see cref="MeterProviderBuilder"/> to chain the calls.</returns>
    public static MeterProviderBuilder AddColoredConsoleExporter(
        this MeterProviderBuilder builder,
        string? name,
        Action<ColoredConsoleOptions>? configure
    ) => AddColoredConsoleExporter(builder, name, configure, configurePeriodicReader: null);

    /// <summary>
    /// Adds ColoredConsole exporter to the MeterProvider with configurable export interval.
    /// </summary>
    /// <param name="builder"><see cref="MeterProviderBuilder"/> builder to use.</param>
    /// <param name="configure">Callback action for configuring <see cref="ColoredConsoleOptions"/>.</param>
    /// <param name="exportIntervalMilliseconds">The export interval in milliseconds.</param>
    /// <returns>The instance of <see cref="MeterProviderBuilder"/> to chain the calls.</returns>
    public static MeterProviderBuilder AddColoredConsoleExporter(
        this MeterProviderBuilder builder,
        Action<ColoredConsoleOptions> configure,
        int exportIntervalMilliseconds
    ) =>
        AddColoredConsoleExporter(
            builder,
            name: null,
            configure,
            periodicReaderOptions =>
                periodicReaderOptions.ExportIntervalMilliseconds = exportIntervalMilliseconds
        );

    /// <summary>
    /// Adds ColoredConsole exporter to the MeterProvider.
    /// </summary>
    /// <param name="builder"><see cref="MeterProviderBuilder"/> builder to use.</param>
    /// <param name="name">Optional name which is used when retrieving options.</param>
    /// <param name="configureExporter">Optional callback action for configuring <see cref="ColoredConsoleOptions"/>.</param>
    /// <param name="configurePeriodicReader">Optional callback action for configuring <see cref="PeriodicExportingMetricReaderOptions"/>.</param>
    /// <returns>The instance of <see cref="MeterProviderBuilder"/> to chain the calls.</returns>
    public static MeterProviderBuilder AddColoredConsoleExporter(
        this MeterProviderBuilder builder,
        string? name,
        Action<ColoredConsoleOptions>? configureExporter,
        Action<PeriodicExportingMetricReaderOptions>? configurePeriodicReader
    )
    {
        if (builder == null)
            throw new ArgumentNullException(nameof(builder));

        name ??= Options.DefaultName;

        if (configureExporter != null)
        {
            builder.ConfigureServices(services => services.Configure(name, configureExporter));
        }

        if (configurePeriodicReader != null)
        {
            builder.ConfigureServices(services =>
                services.Configure(name, configurePeriodicReader)
            );
        }

        return builder.AddReader(sp =>
        {
            var exporterOptions = sp.GetRequiredService<IOptionsMonitor<ColoredConsoleOptions>>()
                .Get(name);
            var periodicReaderOptions = sp.GetRequiredService<
                IOptionsMonitor<PeriodicExportingMetricReaderOptions>
            >()
                .Get(name);

            var exporter = new ColoredConsoleMetricExporter(exporterOptions);
            return new PeriodicExportingMetricReader(
                exporter,
                periodicReaderOptions.ExportIntervalMilliseconds
                    ?? DefaultExportIntervalMilliseconds,
                periodicReaderOptions.ExportTimeoutMilliseconds ?? DefaultExportTimeoutMilliseconds
            );
        });
    }
}
