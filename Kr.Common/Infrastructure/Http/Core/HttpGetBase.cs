using Kr.Common.Extensions;

namespace Kr.Common.Infrastructure.Http;

public abstract partial class HttpBase<ErrorHandler>
{
    protected async Task<TResponse?> Get<TResponse>(string url,
        string version, CancellationToken token = new CancellationToken())
        => await this.ExecuteGetAsync<TResponse>(url, version,
            new Dictionary<string, string>(), token).ConfigureAwait(false);

    protected async Task<TResponse?> Get<TResponse>(string url,
    string version, Dictionary<string, string> headers,
      CancellationToken token = new CancellationToken())
           => await this.ExecuteGetAsync<TResponse>(url, version, headers, token)
                    .ConfigureAwait(false);

    private async Task<TResponse?> ExecuteGetAsync<TResponse>(string url,
    string version,
    Dictionary<string, string> headers,
    CancellationToken token)
    {
        using (var request = new HttpRequestMessage(HttpMethod.Get, url))
        {
            _client.Addheaders(headers, request, () => HttpBase<ErrorHandler>.contextVersion(version));
            return await ExecuteAsync<TResponse>(request, url, token: token).ConfigureAwait(false);
        }
    }
}

