using System.Diagnostics;
using Essential.OpenTelemetry.Exporter;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Trace;

namespace Essential.OpenTelemetry;

/// <summary>
/// Extension methods for registering the OTLP file exporter with OpenTelemetry tracing.
/// </summary>
public static class OtlpFileTracingExtensions
{
    /// <summary>
    /// Adds OTLP File exporter with TracerProviderBuilder.
    /// </summary>
    /// <param name="tracerProviderBuilder"><see cref="TracerProviderBuilder"/>.</param>
    /// <returns>The supplied instance of <see cref="TracerProviderBuilder"/> to chain the calls.</returns>
    public static TracerProviderBuilder AddOtlpFileExporter(
        this TracerProviderBuilder tracerProviderBuilder
    ) => AddOtlpFileExporter(tracerProviderBuilder, name: null, configure: null);

    /// <summary>
    /// Adds OTLP File exporter with TracerProviderBuilder.
    /// </summary>
    /// <param name="tracerProviderBuilder"><see cref="TracerProviderBuilder"/>.</param>
    /// <param name="configure">Callback action for configuring <see cref="OtlpFileOptions"/>.</param>
    /// <returns>The supplied instance of <see cref="TracerProviderBuilder"/> to chain the calls.</returns>
    public static TracerProviderBuilder AddOtlpFileExporter(
        this TracerProviderBuilder tracerProviderBuilder,
        Action<OtlpFileOptions> configure
    ) => AddOtlpFileExporter(tracerProviderBuilder, name: null, configure);

    /// <summary>
    /// Adds OTLP File exporter with TracerProviderBuilder.
    /// </summary>
    /// <param name="tracerProviderBuilder"><see cref="TracerProviderBuilder"/>.</param>
    /// <param name="name">Optional name which is used when retrieving options.</param>
    /// <param name="configure">Optional callback action for configuring <see cref="OtlpFileOptions"/>.</param>
    /// <returns>The supplied instance of <see cref="TracerProviderBuilder"/> to chain the calls.</returns>
    public static TracerProviderBuilder AddOtlpFileExporter(
        this TracerProviderBuilder tracerProviderBuilder,
        string? name,
        Action<OtlpFileOptions>? configure
    )
    {
        if (tracerProviderBuilder == null)
            throw new ArgumentNullException(nameof(tracerProviderBuilder));

        name ??= Options.DefaultName;

        if (configure != null)
        {
            tracerProviderBuilder.ConfigureServices(services =>
                services.Configure(name, configure)
            );
        }

        return tracerProviderBuilder.AddProcessor(sp =>
        {
            var options = sp.GetRequiredService<IOptionsMonitor<OtlpFileOptions>>().Get(name);

            return new SimpleActivityExportProcessor(new OtlpFileActivityExporter(options));
        });
    }
}
