using Essential.OpenTelemetry.Exporter;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Logs;

namespace Essential.OpenTelemetry;

public static class ColoredConsoleLoggingExtensions
{
    /// <summary>
    /// Adds Console exporter with LoggerProviderBuilder.
    /// </summary>
    /// <param name="loggerProviderBuilder"><see cref="LoggerProviderBuilder"/>.</param>
    /// <returns>The supplied instance of <see cref="LoggerProviderBuilder"/> to chain the calls.</returns>
    public static LoggerProviderBuilder AddColoredConsoleExporter(
        this LoggerProviderBuilder loggerProviderBuilder
    ) => AddColoredConsoleExporter(loggerProviderBuilder, name: null, configure: null);

    /// <summary>
    /// Adds Console exporter with LoggerProviderBuilder.
    /// </summary>
    /// <param name="loggerProviderBuilder"><see cref="LoggerProviderBuilder"/>.</param>
    /// <param name="configure">Callback action for configuring <see cref="ColoredConsoleOptions"/>.</param>
    /// <returns>The supplied instance of <see cref="LoggerProviderBuilder"/> to chain the calls.</returns>
    public static LoggerProviderBuilder AddColoredConsoleExporter(
        this LoggerProviderBuilder loggerProviderBuilder,
        Action<ColoredConsoleOptions> configure
    ) => AddColoredConsoleExporter(loggerProviderBuilder, name: null, configure);

    /// <summary>
    /// Adds Console exporter with LoggerProviderBuilder.
    /// </summary>
    /// <param name="loggerProviderBuilder"><see cref="LoggerProviderBuilder"/>.</param>
    /// <param name="name">Optional name which is used when retrieving options.</param>
    /// <param name="configure">Optional callback action for configuring <see cref="ColoredConsoleOptions"/>.</param>
    /// <returns>The supplied instance of <see cref="LoggerProviderBuilder"/> to chain the calls.</returns>
    public static LoggerProviderBuilder AddColoredConsoleExporter(
        this LoggerProviderBuilder loggerProviderBuilder,
        string? name,
        Action<ColoredConsoleOptions>? configure
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
            var options = sp.GetRequiredService<IOptionsMonitor<ColoredConsoleOptions>>().Get(name);

            return new SimpleLogRecordExportProcessor(new ColoredConsoleLogRecordExporter(options));
        });
    }
}
