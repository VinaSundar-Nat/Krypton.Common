using Kr.Common.Extensions;

namespace Kr.Common.Infrastructure.Http;

public abstract partial class HttpBase<ErrorHandler>
{
    private readonly HttpClient _client;
    private readonly IFormatter<ErrorHandler> _formatter;

    protected HttpBase(HttpClient httpClient, IFormatter<ErrorHandler> formatter)
    {
        _client = httpClient;
        _formatter = formatter;
    }

    protected async Task<TResponse?> ExecuteFormEncodedPostAsync<TResponse>(string url,
     KeyValuePair<string, string>[] model, string contentType,
     Dictionary<string, string> headers,
     CancellationToken token = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = new FormUrlEncodedContent(model);
            request.Content.Headers.ContentType = CustomMediaTypeHeaderValue(HttpConstants.TypeFormEncoded);
            _client.Addheaders(new Dictionary<string, string>(), request);
            return await ExecuteAsync<TResponse>(request, url, token: token).ConfigureAwait(false);
    }

    private async Task<TResponse?> ExecuteAsync<TResponse>(HttpRequestMessage request,
      string url, Action<HttpResponseMessage>? metadataOps = null, CancellationToken token = default)
    {
        using var response = await _client
                      .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
            _formatter.Verify(response);
            metadataOps?.Invoke(response);
            var responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            return responseStream.ReadAndDeserializeFromJson<TResponse>();
    }
}




