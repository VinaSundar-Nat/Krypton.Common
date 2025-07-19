
namespace Kr.Common.Infrastructure.Http.Models;

public class HttpPostRequest<Request>
{
    public required string Url { get; init; }
    public Request? RequestData { get; init; }
    public string? Version { get; init; }
    public Dictionary<string, string>? Headers { get; init; } = default;
    public CancellationToken Token { get; init; } = default;
}

public class HttpPostResponse<Response>
{
    public Response? Data { get; set; }
}


public sealed class HttpPostDataResponse<Response, TMetaData>: HttpPostResponse<Response>
    where TMetaData : class, new()
{
    public HttpPostDataResponse()
    {
        MetaData = new TMetaData();
    }

    public TMetaData? MetaData { get; set; }
}