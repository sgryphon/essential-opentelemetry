using Essential.OpenTelemetry.Exporter;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Logs;

namespace Essential.OpenTelemetry;

/// <summary>
/// Extension methods for registering the OTLP file exporter with OpenTelemetry logging.
/// </summary>
public static class OtlpFileLoggingExtensions
{
    /// <summary>
    /// Adds OTLP File exporter with LoggerProviderBuilder.
    /// </summary>
    /// <param name="loggerProviderBuilder"><see cref="LoggerProviderBuilder"/>.</param>
    /// <returns>The supplied instance of <see cref="LoggerProviderBuilder"/> to chain the calls.</returns>
    public static LoggerProviderBuilder AddOtlpFileExporter(
        this LoggerProviderBuilder loggerProviderBuilder
    ) => AddOtlpFileExporter(loggerProviderBuilder, name: null, configure: null);

    /// <summary>
    /// Adds OTLP File exporter with LoggerProviderBuilder.
    /// </summary>
    /// <param name="loggerProviderBuilder"><see cref="LoggerProviderBuilder"/>.</param>
    /// <param name="configure">Callback action for configuring <see cref="OtlpFileOptions"/>.</param>
    /// <returns>The supplied instance of <see cref="LoggerProviderBuilder"/> to chain the calls.</returns>
    public static LoggerProviderBuilder AddOtlpFileExporter(
        this LoggerProviderBuilder loggerProviderBuilder,
        Action<OtlpFileOptions> configure
    ) => AddOtlpFileExporter(loggerProviderBuilder, name: null, configure);

    /// <summary>
    /// Adds OTLP File exporter with LoggerProviderBuilder.
    /// </summary>
    /// <param name="loggerProviderBuilder"><see cref="LoggerProviderBuilder"/>.</param>
    /// <param name="name">Optional name which is used when retrieving options.</param>
    /// <param name="configure">Optional callback action for configuring <see cref="OtlpFileOptions"/>.</param>
    /// <returns>The supplied instance of <see cref="LoggerProviderBuilder"/> to chain the calls.</returns>
    public static LoggerProviderBuilder AddOtlpFileExporter(
        this LoggerProviderBuilder loggerProviderBuilder,
        string? name,
        Action<OtlpFileOptions>? configure
    )
    {
        if (loggerProviderBuilder == null)
            throw new ArgumentNullException(nameof(loggerProviderBuilder));

        name ??= Options.DefaultName;

        if (configure != null)
        {
            loggerProviderBuilder.ConfigureServices(services =>
                services.Configure(name, configure)
            );
        }

        return loggerProviderBuilder.AddProcessor(sp =>
        {
            var options = sp.GetRequiredService<IOptionsMonitor<OtlpFileOptions>>().Get(name);

            return new SimpleLogRecordExportProcessor(new OtlpFileLogRecordExporter(options));
        });
    }

    /// <summary>
    /// Adds OTLP File exporter with OpenTelemetryLoggerOptions.
    /// </summary>
    /// <param name="loggerOptions"><see cref="OpenTelemetryLoggerOptions"/>.</param>
    /// <param name="configure">Callback action for configuring <see cref="OtlpFileOptions"/>.</param>
    /// <returns>The supplied instance of <see cref="OpenTelemetryLoggerOptions"/> to chain the calls.</returns>
    public static OpenTelemetryLoggerOptions AddOtlpFileExporter(
        this OpenTelemetryLoggerOptions loggerOptions,
        Action<OtlpFileOptions>? configure
    )
    {
        if (loggerOptions == null)
            throw new ArgumentNullException(nameof(loggerOptions));

        var otlpFileOptions = new OtlpFileOptions();
        configure?.Invoke(otlpFileOptions);

        return loggerOptions.AddProcessor(sp =>
        {
            return new SimpleLogRecordExportProcessor(
                new OtlpFileLogRecordExporter(otlpFileOptions)
            );
        });
    }
}
