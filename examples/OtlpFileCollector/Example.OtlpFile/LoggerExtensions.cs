using Microsoft.Extensions.Logging;

internal static partial class LoggerExtensions
{
    [LoggerMessage(
        Level = LogLevel.Debug,
        EventId = 10001,
        Message = "This is a debug message for debugging"
    )]
    public static partial void DebugMessage(this ILogger logger);

    [LoggerMessage(
        Level = LogLevel.Information,
        EventId = 2300,
        Message = "User {UserName} logged in from {IpAddress} at {Timestamp}"
    )]
    public static partial void UserLoggedIn(
        this ILogger logger,
        string userName,
        string ipAddress,
        DateTime timestamp
    );

    [LoggerMessage(
        Level = LogLevel.Information,
        EventId = 2501,
        Message = "Processing order {OrderId} for {Amount:C}"
    )]
    public static partial void ProcessingOrder(this ILogger logger, string orderId, decimal amount);

    [LoggerMessage(Level = LogLevel.Warning, EventId = 4100, Message = "{Resource} is running low")]
    public static partial void ResourceRunningLow(this ILogger logger, string resource);

    [LoggerMessage(
        Level = LogLevel.Error,
        EventId = 5100,
        Message = "Exception caught while processing {Operation}"
    )]
    public static partial void OperationError(this ILogger logger, Exception ex, string operation);

    [LoggerMessage(
        Level = LogLevel.Critical,
        EventId = 9100,
        Message = "Critical system failure in {Component}"
    )]
    public static partial void CriticalSystemFailure(this ILogger logger, string component);
}
