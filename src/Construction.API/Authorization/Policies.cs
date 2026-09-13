using Construction.Domain.Enums;

namespace Construction.API.Authorization;

/// <summary>
/// Central definition of role-based authorization policies. Policies express
/// the role hierarchy so controllers never hard-code role lists.
/// </summary>
public static class Policies
{
    public const string SuperAdminOnly = nameof(SuperAdminOnly);
    public const string AdminAndAbove = nameof(AdminAndAbove);
    public const string ProjectManagerAndAbove = nameof(ProjectManagerAndAbove);
    public const string ForemanAndAbove = nameof(ForemanAndAbove);

    /// <summary>
    /// Every internal-staff role — deliberately not "any authenticated
    /// user". <see cref="UserRole.Customer"/> is authenticated too, and would
    /// silently pass a <c>RequireAuthenticatedUser()</c> check on every one
    /// of the many controllers gated by this policy (time entries, tools,
    /// vehicles, absences, bulletin, and more) — exactly the kind of leak
    /// that stays invisible until the day a customer login actually exists.
    /// Listing the five roles explicitly means adding a new role is opt-in
    /// everywhere, never opt-out by accident.
    /// </summary>
    public const string AllEmployees = nameof(AllEmployees);

    public const string CustomerOnly = nameof(CustomerOnly);

    public static IServiceCollection AddAuthorizationPolicies(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            .AddPolicy(SuperAdminOnly, policy => policy.RequireRole(
                nameof(UserRole.SuperAdmin)))
            .AddPolicy(AdminAndAbove, policy => policy.RequireRole(
                nameof(UserRole.SuperAdmin),
                nameof(UserRole.Admin)))
            .AddPolicy(ProjectManagerAndAbove, policy => policy.RequireRole(
                nameof(UserRole.SuperAdmin),
                nameof(UserRole.Admin),
                nameof(UserRole.ProjectManager)))
            .AddPolicy(ForemanAndAbove, policy => policy.RequireRole(
                nameof(UserRole.SuperAdmin),
                nameof(UserRole.Admin),
                nameof(UserRole.ProjectManager),
                nameof(UserRole.Foreman)))
            .AddPolicy(AllEmployees, policy => policy.RequireRole(
                nameof(UserRole.SuperAdmin),
                nameof(UserRole.Admin),
                nameof(UserRole.ProjectManager),
                nameof(UserRole.Foreman),
                nameof(UserRole.Worker)))
            .AddPolicy(CustomerOnly, policy => policy.RequireRole(
                nameof(UserRole.Customer)));

        return services;
    }
}
