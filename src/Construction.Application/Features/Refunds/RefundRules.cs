using Construction.Domain.Enums;

namespace Construction.Application.Features.Refunds;

/// <summary>Who may ask to be paid back, who decides, and what a request must say.</summary>
public static class RefundRules
{
    /// <summary>Most a single request may ask for. A typo guard, not a policy: the limit the firm wants is still to be set.</summary>
    public const decimal MaxAmount = 100000m;

    /// <summary>How far back an expense may be. A receipt older than this is an accounting question, not a refund.</summary>
    public const int MaxBackdatingDays = 365;

    /// <summary>The office decides. Nobody decides their own.</summary>
    public static bool CanReview(UserRole? role) =>
        role is UserRole.SuperAdmin or UserRole.Admin or UserRole.ProjectManager;

    /// <summary>Approved refunds pay out with a payroll month; only the roles that see the payroll may change which.</summary>
    public static bool CanSetPayrollMonth(UserRole? role) => CanReview(role);
}
