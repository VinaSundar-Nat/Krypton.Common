using System;
using Kr.Common.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace Kr.Common.Infrastructure.Http.Models;

public class PagingModel
{
    
    [FromQuery(Name = "index")]
    public string? Index { get; init; }

    public int GetNext => Index?.Page(1) ?? 0;

    [FromQuery(Name = "limit")]
    public string? Limit { get; init; }

    public int GetLimit => Index?.Page(10) ?? 10;
}

