using Microsoft.Extensions.Logging;

namespace Essential.OpenTelemetry.Exporter.OtlpFile.Tests;

internal static partial class LoggerExtensions
{
    [LoggerMessage(
        LogLevel.Error,
        "Exception caught while processing order {OrderId} for {Amount:C}"
    )]
    public static partial void OperationError(
        this ILogger logger,
        Exception ex,
        string orderId,
        decimal amount
    );
}
