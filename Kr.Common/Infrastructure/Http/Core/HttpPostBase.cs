using System.Net.Http.Headers;
using Kr.Common.Extensions;
using Kr.Common.Infrastructure.Http.Models;


namespace Kr.Common.Infrastructure.Http;

public abstract partial class HttpBase<ErrorHandler>
{
    protected async Task<TResponse?> Post<Request, TResponse>(HttpPostRequest<Request> httpRequest)
        => await this.ExecutePostAsync<Request, TResponse>(httpRequest)
                 .ConfigureAwait(false);

    protected async Task<ApiSuccessResponseWithMetadata<TResponse, TMetaData>> Post<Request, TResponse, TMetaData>
        (HttpPostRequest<Request> request)
        where TMetaData : class, new()
    {
        var response = new ApiSuccessResponseWithMetadata<TResponse, TMetaData>();
        response.Data = await this.ExecutePostAsync<Request, TResponse>(request,
            (responseMsg) => GetMetaData<TMetaData>(responseMsg, response.MetaData)).ConfigureAwait(false);

        return response;
    }

    private async Task<TResponse?> ExecutePostAsync<Request, TResponse>(HttpPostRequest<Request> httpRequest,
    Action<HttpResponseMessage>? metadataOps = null)
    {
        using (var memStream = new MemoryStream())
        {
            memStream.SerializeToJsonStream(httpRequest.RequestData);

            using (var request = new HttpRequestMessage(HttpMethod.Post, httpRequest.Url))
            {
                using (var stream = new StreamContent(memStream))
                {
                    stream.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    request.Content = stream;
                    _client.Addheaders(httpRequest.Headers, request, () => HttpBase<ErrorHandler>.contextVersion(httpRequest.Version));
                    return await ExecuteAsync<TResponse>(request, httpRequest.Url, metadataOps, httpRequest.Token).ConfigureAwait(false);
                }
            }
        }
    }

}


