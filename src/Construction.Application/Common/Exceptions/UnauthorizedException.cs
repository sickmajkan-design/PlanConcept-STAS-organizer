namespace Construction.Application.Common.Exceptions;

/// <summary>Translated to HTTP 401 by the API middleware.</summary>
public class UnauthorizedException : Exception
{
    public UnauthorizedException(string message)
        : base(message)
    {
    }
}
