using System;
using Microsoft.AspNetCore.Mvc;

namespace Kr.Common.Infrastructure.Http.Models;

public record ApiGetRequest(
    [FromHeader(Name = "X-User")] string UserId,
    [FromHeader(Name = "X-Correlation-ID")] string CorrelationId
);

public class ApiGetResponse
{
    public required int StatusCode { get; init; }
    public required string Url { get; init; }
}

public sealed class ApiGetSuccessResponse<T> : ApiGetResponse
{

    public required T? Data { get; init; }
}

public sealed class ApiGetDataResponse<T> :
    ApiGetResponse
    where T : class
{
    public required T Data { get; init; }
}




