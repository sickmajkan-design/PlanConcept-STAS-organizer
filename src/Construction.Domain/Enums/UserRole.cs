namespace Construction.Domain.Enums;

public enum UserRole
{
    SuperAdmin = 1,
    Admin = 2,
    ProjectManager = 3,
    Foreman = 4,
    Worker = 5,

    /// <summary>
    /// An external client, not a staff member — logs into a separate,
    /// read-only portal to see their own project's status. Deliberately
    /// outside every "AndAbove" tier: see the remarks on
    /// <see cref="Construction.API.Authorization.Policies.AllEmployees"/> for
    /// why this must never be picked up by an internal-staff policy by
    /// accident.
    /// </summary>
    Customer = 6
}
