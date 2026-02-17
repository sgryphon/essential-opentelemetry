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
    private const int DefaultExportIntervalMilliseconds = 10000;
    private const int DefaultExportTimeoutMilliseconds = Timeout.Infinite;

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
            var options = sp.GetRequiredService<IOptionsMonitor<OtlpFileOptions>>().Get(name);

            var exporter = new OtlpFileMetricExporter(options);
            return new PeriodicExportingMetricReader(
                exporter,
                DefaultExportIntervalMilliseconds,
                DefaultExportTimeoutMilliseconds
            );
        });
    }

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
            var options = sp.GetRequiredService<IOptionsMonitor<OtlpFileOptions>>().Get(name);

            var exporter = new OtlpFileMetricExporter(options);
            return new PeriodicExportingMetricReader(
                exporter,
                exportIntervalMilliseconds,
                DefaultExportTimeoutMilliseconds
            );
        });
    }
}
