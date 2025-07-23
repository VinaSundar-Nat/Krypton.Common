using System.Net;
using Kr.Common.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Kr.Common.Infrastructure.Http;

public class BadRequestExceptionHandler : IExceptionHandler
{

    private readonly ILogger<BadRequestExceptionHandler> _logger;

    public BadRequestExceptionHandler(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<BadRequestExceptionHandler>();
    }


    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (!(exception is DomainValidationException domainValidation))
            return false;

        _logger.LogBadRequestException(httpContext.Request.Path, httpContext.Request.Method, exception);

        var errors = domainValidation.Errors.Select(e => $"{e.PropertyName},{e.ErrorMessage}").ToArray() ??
                            new string[] { };

        var problemDetails = new ValidationProblemDetails
        {
            Status = (int)HttpStatusCode.BadRequest,
            Title = "BadRequest Error",
            Detail = exception.Message,
            Instance = $"{httpContext.Request.Method} {httpContext.Request.Path}"
        };

        problemDetails.Errors.Add("Validation", errors);

        await httpContext.Response.WriteAsJsonAsync(problemDetails);

        return true;
    }
}
