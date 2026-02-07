using System.Diagnostics;
using Essential.OpenTelemetry.Exporter;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Trace;

namespace Essential.OpenTelemetry;

public static class ColoredConsoleTracingExtensions
{
    /// <summary>
    /// Adds Console exporter to the TracerProvider.
    /// </summary>
    /// <param name="builder"><see cref="TracerProviderBuilder"/> builder to use.</param>
    /// <returns>The instance of <see cref="TracerProviderBuilder"/> to chain the calls.</returns>
    public static TracerProviderBuilder AddColoredConsoleExporter(
        this TracerProviderBuilder builder
    ) => AddColoredConsoleExporter(builder, name: null, configure: null);

    /// <summary>
    /// Adds Console exporter to the TracerProvider.
    /// </summary>
    /// <param name="builder"><see cref="TracerProviderBuilder"/> builder to use.</param>
    /// <param name="configure">Callback action for configuring <see cref="ColoredConsoleOptions"/>.</param>
    /// <returns>The instance of <see cref="TracerProviderBuilder"/> to chain the calls.</returns>
    public static TracerProviderBuilder AddColoredConsoleExporter(
        this TracerProviderBuilder builder,
        Action<ColoredConsoleOptions> configure
    ) => AddColoredConsoleExporter(builder, name: null, configure);

    /// <summary>
    /// Adds Console exporter to the TracerProvider.
    /// </summary>
    /// <param name="builder"><see cref="TracerProviderBuilder"/> builder to use.</param>
    /// <param name="name">Optional name which is used when retrieving options.</param>
    /// <param name="configure">Optional callback action for configuring <see cref="ColoredConsoleOptions"/>.</param>
    /// <returns>The instance of <see cref="TracerProviderBuilder"/> to chain the calls.</returns>
    public static TracerProviderBuilder AddColoredConsoleExporter(
        this TracerProviderBuilder builder,
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

        return builder.AddProcessor(sp =>
        {
            var options = sp.GetRequiredService<IOptionsMonitor<ColoredConsoleOptions>>().Get(name);

            return new SimpleActivityExportProcessor(new ColoredConsoleActivityExporter(options));
        });
    }
}
