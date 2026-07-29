namespace Construction.Application.Common.Exceptions;

/// <summary>Translated to HTTP 404 by the API middleware.</summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message)
        : base(message)
    {
    }

    public NotFoundException(string entityName, object key)
        : base($"{entityName} with id '{key}' was not found.")
    {
    }
}
