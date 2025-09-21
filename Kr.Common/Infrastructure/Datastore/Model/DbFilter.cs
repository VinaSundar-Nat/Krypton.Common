using System;
using Newtonsoft.Json;

namespace Kr.Common.Infrastructure.Datastore.Model;

public enum FilterOperation
{
    [JsonProperty("eq")]
    Equal,
    [JsonProperty("neq")]
    NotEqual,
    [JsonProperty("gt")]
    GreaterThan,
    [JsonProperty("gte")]
    GreaterThanOrEqual,
    [JsonProperty("lt")]
    LessThan,
    [JsonProperty("lte")]
    LessThanOrEqual
}

public sealed record DbFilter<T>(string Property, T Value, FilterOperation Operation);


