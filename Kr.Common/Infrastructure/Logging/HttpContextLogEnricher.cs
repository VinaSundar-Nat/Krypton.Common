using Microsoft.AspNetCore.Http;
using Serilog.Core;
using Serilog.Events;


namespace Kr.Common.Infrastructure.Logging;

public class HttpContextLogEnricher : ILogEventEnricher
{
    private readonly IHttpContextAccessor _contextAccessor;
    public HttpContextLogEnricher(IHttpContextAccessor contextAccessor)
    {
        _contextAccessor = contextAccessor;
    }

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var properties = new HttpContextProperties(_contextAccessor);

        if (!string.IsNullOrEmpty(properties.User))
        {
            var property = propertyFactory.CreateProperty("User", properties.User);
            logEvent.AddPropertyIfAbsent(property);
        }

        if (!string.IsNullOrEmpty(properties.CorrelationId))
        {
            var property = propertyFactory.CreateProperty("CorrelationId", properties.CorrelationId);
            logEvent.AddPropertyIfAbsent(property);
        }

        if (!string.IsNullOrEmpty(properties.System))
        {
            var property = propertyFactory.CreateProperty("System", properties.System);
            logEvent.AddPropertyIfAbsent(property);
        }
    }
}

internal sealed class HttpContextProperties
{
    internal readonly string? System;
    internal readonly string? User;
    internal readonly string? CorrelationId;

    internal HttpContextProperties(IHttpContextAccessor contextAccessor)
    {
        CorrelationId = contextAccessor?.HttpContext?.Request?.Headers["X-Correlation-ID"];
        User = contextAccessor?.HttpContext?.Request?.Headers["X-User"];
        System = contextAccessor?.HttpContext?.Request?.Headers["X-System"];
    }
}
