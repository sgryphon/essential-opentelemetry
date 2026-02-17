using Essential.OpenTelemetry.Exporter;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Metrics;

namespace Essential.OpenTelemetry;

/// <summary>
/// Extension methods for registering the OTLP file exporter with OpenTelemetry metrics.
/// </summary>
public static class OtlpFileMetricsExtensions
{
    private const int DefaultExportIntervalMilliseconds = 60_000;
    private const int DefaultExportTimeoutMilliseconds = 30_000;

    /// <summary>
    /// Adds OTLP File exporter to the MeterProvider.
    /// </summary>
    /// <param name="builder"><see cref="MeterProviderBuilder"/> builder to use.</param>
    /// <returns>The instance of <see cref="MeterProviderBuilder"/> to chain the calls.</returns>
    public static MeterProviderBuilder AddOtlpFileExporter(this MeterProviderBuilder builder) =>
        AddOtlpFileExporter(builder, name: null, configure: null);

    /// <summary>
    /// Adds OTLP File exporter to the MeterProvider.
    /// </summary>
    /// <param name="builder"><see cref="MeterProviderBuilder"/> builder to use.</param>
    /// <param name="configure">Callback action for configuring <see cref="OtlpFileOptions"/>.</param>
    /// <returns>The instance of <see cref="MeterProviderBuilder"/> to chain the calls.</returns>
    public static MeterProviderBuilder AddOtlpFileExporter(
        this MeterProviderBuilder builder,
        Action<OtlpFileOptions> configure
    ) => AddOtlpFileExporter(builder, name: null, configure);

    /// <summary>
    /// Adds OTLP File exporter to the MeterProvider.
    /// </summary>
    /// <param name="builder"><see cref="MeterProviderBuilder"/> builder to use.</param>
    /// <param name="name">Optional name which is used when retrieving options.</param>
    /// <param name="configure">Optional callback action for configuring <see cref="OtlpFileOptions"/>.</param>
    /// <returns>The instance of <see cref="MeterProviderBuilder"/> to chain the calls.</returns>
    public static MeterProviderBuilder AddOtlpFileExporter(
        this MeterProviderBuilder builder,
        string? name,
        Action<OtlpFileOptions>? configure
    ) => AddOtlpFileExporter(builder, name, configure, configurePeriodicReader: null);

    /// <summary>
    /// Adds OTLP File exporter to the MeterProvider with configurable export interval.
    /// </summary>
    /// <param name="builder"><see cref="MeterProviderBuilder"/> builder to use.</param>
    /// <param name="configure">Callback action for configuring <see cref="OtlpFileOptions"/>.</param>
    /// <param name="exportIntervalMilliseconds">The export interval in milliseconds.</param>
    /// <returns>The instance of <see cref="MeterProviderBuilder"/> to chain the calls.</returns>
    public static MeterProviderBuilder AddOtlpFileExporter(
        this MeterProviderBuilder builder,
        Action<OtlpFileOptions> configure,
        int exportIntervalMilliseconds
    ) =>
        AddOtlpFileExporter(
            builder,
            name: null,
            configure,
            periodicReaderOptions =>
                periodicReaderOptions.ExportIntervalMilliseconds = exportIntervalMilliseconds
        );

    /// <summary>
    /// Adds OTLP File exporter to the MeterProvider.
    /// </summary>
    /// <param name="builder"><see cref="MeterProviderBuilder"/> builder to use.</param>
    /// <param name="name">Optional name which is used when retrieving options.</param>
    /// <param name="configureExporter">Optional callback action for configuring <see cref="OtlpFileOptions"/>.</param>
    /// <param name="configurePeriodicReader">Optional callback action for configuring <see cref="PeriodicExportingMetricReaderOptions"/>.</param>
    /// <returns>The instance of <see cref="MeterProviderBuilder"/> to chain the calls.</returns>
    public static MeterProviderBuilder AddOtlpFileExporter(
        this MeterProviderBuilder builder,
        string? name,
        Action<OtlpFileOptions>? configureExporter,
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
            var exporterOptions = sp.GetRequiredService<IOptionsMonitor<OtlpFileOptions>>()
                .Get(name);
            var periodicReaderOptions = sp.GetRequiredService<
                IOptionsMonitor<PeriodicExportingMetricReaderOptions>
            >()
                .Get(name);

            var exporter = new OtlpFileMetricExporter(exporterOptions);
            return new PeriodicExportingMetricReader(
                exporter,
                periodicReaderOptions.ExportIntervalMilliseconds
                    ?? DefaultExportIntervalMilliseconds,
                periodicReaderOptions.ExportTimeoutMilliseconds ?? DefaultExportTimeoutMilliseconds
            );
        });
    }
}
