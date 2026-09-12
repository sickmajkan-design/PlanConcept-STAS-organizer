namespace Construction.Application.Common.Exceptions;

/// <summary>
/// A dependency outside this system could not answer.
/// </summary>
/// <remarks>
/// Separate from the opaque 500 because the two mean different things to
/// whoever is holding the phone: a 500 says this system has a fault and the
/// request will keep failing, a 503 says something it leans on is busy or
/// unreachable and the same request is worth making again. The assistant is
/// the first feature here that depends on a third party while a user waits,
/// so it is the first to need the distinction.
/// </remarks>
public class ServiceUnavailableException : Exception
{
    public ServiceUnavailableException(string message)
        : base(message)
    {
    }

    public ServiceUnavailableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
