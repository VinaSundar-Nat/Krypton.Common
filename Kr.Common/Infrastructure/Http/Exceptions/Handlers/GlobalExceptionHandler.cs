using System.Net;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Kr.Common.Infrastructure.Http;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IProblemDetailsService _problemDetailsService;

    public GlobalExceptionHandler(ILoggerFactory loggerFactory, IProblemDetailsService problemDetailsService)
    {
        _logger = loggerFactory.CreateLogger<GlobalExceptionHandler>();
        _problemDetailsService = problemDetailsService;
    }


    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogServerException(httpContext.Request.Path, httpContext.Request.Method, exception);

        var problemDetails = new ProblemDetails
        {
            Status = (int)HttpStatusCode.InternalServerError,
            Title = "Internal Server Error",
            Detail = "Request has encountered a failure. Please contact service support.",
            Instance = httpContext.Request.Path
        };

        httpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            ProblemDetails = problemDetails,
            HttpContext = httpContext
        });

        return true;
    }
}
