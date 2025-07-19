using Microsoft.Extensions.Logging;

namespace Kr.Common.Infrastructure.Http;

public static partial class LoggerExtensions
{
    [LoggerMessage(EventId =1, EventName ="BadRequestException",Level = LogLevel.Error, Message ="Bad request exception encountered in path {path} method {method}")]
    public static partial void LogBadRequestException(this ILogger logger, string path, string method, Exception exception);

    [LoggerMessage(EventId =1, EventName ="ServerException",Level = LogLevel.Error, Message ="Unhandled exception encountered in path {path} method {method}")]
    public static partial void LogServerException(this ILogger logger, string path, string method, Exception exception);

}
