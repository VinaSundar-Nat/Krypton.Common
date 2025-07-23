using Kr.Common.Extensions;
using System.Net.Http.Headers;
using System.Text;
using System.ComponentModel;
using System.Text.Json.Serialization;
using Kr.Common.Guard;

namespace Kr.Common.Infrastructure.Http;

public abstract partial class HttpBase<ErrorHandler>
{		
    //param: vnd.offering.v1
    private static string contextVersion(string? text) =>
        !string.IsNullOrEmpty(text) ? $"application/{text}+json" : "application/json";

    private static MediaTypeHeaderValue CustomMediaTypeHeaderValue(string type) => new(type);

    protected static string BuildUrl<T>(T model) where T : class
    {
        var urlBuilder = new StringBuilder();
        var type = model.GetType();

        foreach (var property in type.GetProperties())
        {
            JsonPropertyNameAttribute? attribute = (JsonPropertyNameAttribute)Attribute.GetCustomAttribute(property, typeof(JsonPropertyNameAttribute))
                                                .Gaurd("Implementor type not defined.");
            urlBuilder.Append($"{attribute.Name}={property.GetValue(model)}&");
        }

        string url = urlBuilder.ToString();
        return url.Length > 0 ? url.Substring(0, url.Length - 1) : string.Empty;
    }

    private void GetMetaData<TMetaData>(HttpResponseMessage response, TMetaData? metaData) where TMetaData : class
    {
        if (response == null || metaData == null)
            return;

        var expressions = metaData.BuildMemberExpressions<TMetaData>();

        foreach (var expression in expressions)
        {
            var field = expression.GetCustomAttributeValue<TMetaData, DescriptionAttribute, string>(attr => attr.Description);
            var value = response.Headers.GetValues(field)?.FirstOrDefault() ?? string.Empty;
            metaData.ExpressionSetter<TMetaData>(expression.Member.Name, value);
        }
    }
}


