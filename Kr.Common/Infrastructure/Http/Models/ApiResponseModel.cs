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

public class HttpPostRequest<Request>
{
    public required string Url { get; init; }
    public Request? RequestData { get; init; }
    public string? Version { get; init; }
    public Dictionary<string, string>? Headers { get; init; } = default;
    public CancellationToken Token { get; init; } = default;
}

public sealed class ApiSuccessResponse<T> : ApiResponse
{

    public required T? Data { get; init; }
}

public sealed class ApiSuccessResponseWithMetadata<Response, TMetaData> 
    where TMetaData : class, new()
{
    public int? StatusCode { get; set; }
    public string? Url { get; set; }

    public ApiSuccessResponseWithMetadata()
    {
        MetaData = new TMetaData();
    }

    public Response? Data { get; set; }
    public TMetaData? MetaData { get; set; }
}





