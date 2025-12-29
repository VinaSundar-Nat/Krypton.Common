using System;
using System.Net;
using FluentValidation;
using FluentValidation.Results;

namespace Kr.Common.Exceptions;

[AttributeUsage(AttributeTargets.Class)]
public class ImplementorAttribute(string implementor) : Attribute
{
    public readonly string nameof = implementor;
}

[Implementor("ValidationException")]
public class DomainValidationException : ValidationException
{
    public DomainValidationException(string message,IEnumerable<ValidationFailure> failures)
        : base(message, failures)
    {
        
    }
}


[Implementor("InternalServerException")]
public class DomainServerException(Exception ex, string message, string? customMessage = null) : Exception(message, ex)
{
    public string? CustomMessage { get; private init; } = customMessage;
}

[Implementor("UnauthorizedException")]
public class DomainUnauthorizedException : Exception
{
    public string? CustomMessage { get; private init; }

    public DomainUnauthorizedException(Exception ex, string message, string? customMessage = null)
        : base(message, ex)
    {
        CustomMessage = customMessage;
    }
}


public class APIException : Exception
{
    public readonly HttpStatusCode statusCode = HttpStatusCode.InternalServerError;

    public APIException()
    {
    }

    public APIException(string message, HttpStatusCode _statusCode) : base(message)
    {
        statusCode = _statusCode;
    }
}




