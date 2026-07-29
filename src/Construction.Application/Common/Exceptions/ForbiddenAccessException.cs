namespace Construction.Application.Common.Exceptions;

/// <summary>Translated to HTTP 403 by the API middleware.</summary>
public class ForbiddenAccessException : Exception
{
    public ForbiddenAccessException()
        : base("You do not have permission to perform this action.")
    {
    }

    public ForbiddenAccessException(string message)
        : base(message)
    {
    }
}
