using System;
namespace Kr.Common.Infrastructure.Http;

public interface IFormatter<ErrorHandler>
{
    void Verify(HttpResponseMessage response);
}


