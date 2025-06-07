using Microsoft.Extensions.Logging;

namespace XIVRaidBot.Services;

/// <summary>
/// Factory service that provides strongly-typed loggers for classes in the application.
/// This removes the need to inject ILoggerFactory directly in each class.
/// </summary>
public class LoggingService
{
    private readonly ILoggerFactory _loggerFactory;

    public LoggingService(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
    }

    /// <summary>
    /// Gets a logger instance for the specified type.
    /// </summary>
    /// <typeparam name="T">The type requesting the logger</typeparam>
    /// <returns>An ILogger instance for the specified type</returns>
    public ILogger<T> GetLogger<T>() => _loggerFactory.CreateLogger<T>();
}