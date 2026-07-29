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
    public const string AllEmployees = nameof(AllEmployees);

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
            .AddPolicy(AllEmployees, policy => policy.RequireAuthenticatedUser());

        return services;
    }
}
