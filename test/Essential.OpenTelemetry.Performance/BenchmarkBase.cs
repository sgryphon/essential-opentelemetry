using Microsoft.Extensions.Configuration;

namespace Essential.OpenTelemetry.Performance;

/// <summary>
/// Base class for benchmarks that provides access to configuration.
/// </summary>
public abstract class BenchmarkBase
{
    private static BenchmarkConfiguration? _configuration;

    protected static BenchmarkConfiguration Configuration
    {
        get
        {
            if (_configuration == null)
            {
                var config = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: true)
                    .AddCommandLine(Environment.GetCommandLineArgs())
                    .Build();

                _configuration = new BenchmarkConfiguration();
                config.GetSection("BenchmarkConfiguration").Bind(_configuration);
            }
            return _configuration;
        }
    }
}
