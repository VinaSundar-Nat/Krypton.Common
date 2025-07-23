
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;
using Polly.Retry;

namespace Kr.Common.Infrastructure.Http;

public static class HttpResiliencyWrapper
{
    public static void GetRetryPolicy<T>(this IHttpClientBuilder builder, int retryCount,
        int retryIn) where T : class
    {
        builder.AddPolicyHandler((serviceProvider,  request)=>
        {
            if (request.Method != HttpMethod.Get)
                return Policy.NoOpAsync<HttpResponseMessage>();

            var logger = serviceProvider.GetRequiredService<ILogger<T>>();
            return GetRetryPolicy(builder,retryCount,retryIn, request ,logger);
        });
    }
    
    private static AsyncRetryPolicy<HttpResponseMessage>  GetRetryPolicy<T>(this IHttpClientBuilder builder, int retryCount,
        int retryIn, HttpRequestMessage request,
        ILogger<T> logger)
    {
            return HttpPolicyExtensions.HandleTransientHttpError()
                        .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound)
                .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
                .WaitAndRetryAsync(retryCount, retryAttempt =>
                {
                    logger.LogError($"Attempting retry: {retryAttempt} for failure.");
                    return TimeSpan.FromSeconds(Math.Pow(retryAttempt, retryIn));
                },
                (exception, timeSpan, retryCount, context) =>
                {
                    var cId = request.Headers.FirstOrDefault(a => a.Key == "X-Correlation-ID");
                    logger.LogError($"Attempting retry: Correlation-ID - {cId}. error : {exception.Exception}");
                });
    }

    public static void GetOnlyRetryPolicy(this IHttpClientBuilder builder, int retryCount, int retryIn,
       Action<string> log)
    {
        builder.AddPolicyHandler((provider, request) =>
        {
            if (request.Method != HttpMethod.Get)
                return Policy.NoOpAsync<HttpResponseMessage>();

            return HttpPolicyExtensions.HandleTransientHttpError()
                .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound)
                .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
                .WaitAndRetryAsync(retryCount, retryAttempt =>
                {
                    log($"Attempting retry: {retryAttempt} for failure.");
                    return TimeSpan.FromSeconds(Math.Pow(retryAttempt, retryIn));
                },
                (exception, timeSpan, retryCount, context) =>
                {
                    var cId = request.Headers.FirstOrDefault(a => a.Key == "X-Correlation-ID");
                    log($"Attempting retry: Correlation-ID - {cId}. error : {exception.Exception}");
                });
        });

    }
}


