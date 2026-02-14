using Essential.OpenTelemetry.Exporter;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Logs;

namespace Essential.OpenTelemetry;

/// <summary>
/// Extension methods for registering the JSONL console exporter with OpenTelemetry logging.
/// </summary>
public static class JsonlConsoleLoggingExtensions
{
    /// <summary>
    /// Adds JSONL Console exporter with LoggerProviderBuilder.
    /// </summary>
    /// <param name="loggerProviderBuilder"><see cref="LoggerProviderBuilder"/>.</param>
    /// <returns>The supplied instance of <see cref="LoggerProviderBuilder"/> to chain the calls.</returns>
    public static LoggerProviderBuilder AddJsonlConsoleExporter(
        this LoggerProviderBuilder loggerProviderBuilder
    ) => AddJsonlConsoleExporter(loggerProviderBuilder, name: null, configure: null);

    /// <summary>
    /// Adds JSONL Console exporter with LoggerProviderBuilder.
    /// </summary>
    /// <param name="loggerProviderBuilder"><see cref="LoggerProviderBuilder"/>.</param>
    /// <param name="configure">Callback action for configuring <see cref="JsonlConsoleOptions"/>.</param>
    /// <returns>The supplied instance of <see cref="LoggerProviderBuilder"/> to chain the calls.</returns>
    public static LoggerProviderBuilder AddJsonlConsoleExporter(
        this LoggerProviderBuilder loggerProviderBuilder,
        Action<JsonlConsoleOptions> configure
    ) => AddJsonlConsoleExporter(loggerProviderBuilder, name: null, configure);

    /// <summary>
    /// Adds JSONL Console exporter with LoggerProviderBuilder.
    /// </summary>
    /// <param name="loggerProviderBuilder"><see cref="LoggerProviderBuilder"/>.</param>
    /// <param name="name">Optional name which is used when retrieving options.</param>
    /// <param name="configure">Optional callback action for configuring <see cref="JsonlConsoleOptions"/>.</param>
    /// <returns>The supplied instance of <see cref="LoggerProviderBuilder"/> to chain the calls.</returns>
    public static LoggerProviderBuilder AddJsonlConsoleExporter(
        this LoggerProviderBuilder loggerProviderBuilder,
        string? name,
        Action<JsonlConsoleOptions>? configure
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
            var options = sp.GetRequiredService<IOptionsMonitor<JsonlConsoleOptions>>().Get(name);

            return new SimpleLogRecordExportProcessor(new JsonlConsoleLogRecordExporter(options));
        });
    }

    /// <summary>
    /// Adds JSONL Console exporter with OpenTelemetryLoggerOptions.
    /// </summary>
    /// <param name="loggerOptions"><see cref="OpenTelemetryLoggerOptions"/>.</param>
    /// <param name="configure">Callback action for configuring <see cref="JsonlConsoleOptions"/>.</param>
    /// <returns>The supplied instance of <see cref="OpenTelemetryLoggerOptions"/> to chain the calls.</returns>
    public static OpenTelemetryLoggerOptions AddJsonlConsoleExporter(
        this OpenTelemetryLoggerOptions loggerOptions,
        Action<JsonlConsoleOptions>? configure
    )
    {
        if (loggerOptions == null)
            throw new ArgumentNullException(nameof(loggerOptions));

        var jsonlConsoleOptions = new JsonlConsoleOptions();
        configure?.Invoke(jsonlConsoleOptions);

        return loggerOptions.AddProcessor(sp =>
        {
            return new SimpleLogRecordExportProcessor(
                new JsonlConsoleLogRecordExporter(jsonlConsoleOptions)
            );
        });
    }
}
