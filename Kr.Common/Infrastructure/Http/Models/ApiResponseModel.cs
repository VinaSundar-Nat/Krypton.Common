using System;
using Microsoft.AspNetCore.Mvc;

namespace Kr.Common.Infrastructure.Http.Models;

public record ApiHeaders(
    [FromHeader(Name = "X-User")] string UserId,
    [FromHeader(Name = "X-Correlation-ID")] string CorrelationId
);

public class ApiResponse
{
    public required int StatusCode { get; init; }
    public required string Url { get; init; }
}

public sealed class ApiSuccessResponse<T> : ApiResponse
{

    public required T? Data { get; init; }
}

public sealed class ApiSuccessResponseWithMetadata<Response, TMetaData> : ApiResponse
    where TMetaData : class, new()
{
    public ApiSuccessResponseWithMetadata()
    {
        MetaData = new TMetaData();
    }

    public required Response? Data { get; init; }
    public TMetaData? MetaData { get; set; }
}





