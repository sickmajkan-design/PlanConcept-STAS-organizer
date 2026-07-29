namespace Construction.Application.Common.Exceptions;

/// <summary>Translated to HTTP 409 by the API middleware.</summary>
public class ConflictException : Exception
{
    public ConflictException(string message)
        : base(message)
    {
    }
}
