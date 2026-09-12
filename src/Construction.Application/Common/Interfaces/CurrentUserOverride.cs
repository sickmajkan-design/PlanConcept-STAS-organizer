using Construction.Domain.Enums;

namespace Construction.Application.Common.Interfaces;

/// <summary>
/// Lets a background job stand in for the user whose action it is carrying
/// out, so a role-gated export handler sees a real identity instead of the
/// "nobody signed in" that a job's DI scope has no other way to produce.
/// </summary>
/// <remarks>
/// <para>
/// A scheduled report is queued as the subscription's creator, not as some
/// unrestricted "system" identity: the point is that the export reflects
/// exactly what that person is allowed to see, including a permission they
/// lose between setting the subscription up and it next running.
/// </para>
/// <para>
/// <see cref="AsyncLocal{T}"/> rather than a constructor parameter because
/// <c>ICurrentUserService</c> is resolved deep inside whatever the job calls
/// (an export query handler several layers down) — threading a value through
/// every signature between the job and that handler would mean widening an
/// interface that every other, HTTP-driven caller uses unchanged.
/// </para>
/// </remarks>
public static class CurrentUserOverride
{
    public sealed record Identity(Guid UserId, string Email, UserRole Role, Guid? EmployeeId);

    private static readonly AsyncLocal<Identity?> Holder = new();

    public static Identity? Current => Holder.Value;

    /// <summary>Sets the override for the caller's async flow until disposed.</summary>
    public static IDisposable Push(Identity identity)
    {
        var previous = Holder.Value;
        Holder.Value = identity;
        return new Restorer(previous);
    }

    private sealed class Restorer : IDisposable
    {
        private readonly Identity? _previous;

        public Restorer(Identity? previous) => _previous = previous;

        public void Dispose() => Holder.Value = _previous;
    }
}
