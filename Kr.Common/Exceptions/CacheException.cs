
namespace Kr.Common.Exceptions;

public enum CacheExceptionType
{
    Connection,
    Store
}

public class CacheException : Exception
{
    public CacheException(string msg, Exception ex) : base(msg, ex)
    {

    }

    public CacheException(string msg) : base(msg)
    {

    }
}

public class CacheConnectionException : Exception
{
    public CacheConnectionException(string msg, Exception ex) : base(msg, ex)
    {

    }

    public CacheConnectionException(string msg) : base(msg)
    {

    }
}


