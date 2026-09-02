namespace Construction.Application.Common.Exceptions;

/// <summary>
/// Translated to HTTP 502 by the API middleware — a call out to a
/// third-party service failed or refused to answer. Unlike
/// <see cref="ConflictException"/>, nothing about the request itself was
/// wrong; asking again, or asking differently, may well succeed.
/// </summary>
public class ExternalServiceException : Exception
{
    public ExternalServiceException(string message)
        : base(message)
    {
    }
}
