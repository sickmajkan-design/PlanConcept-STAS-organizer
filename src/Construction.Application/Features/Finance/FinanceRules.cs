using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Finance;

/// <summary>
/// Who may see the company's income, spending and profit.
/// </summary>
/// <remarks>
/// A SuperAdmin always may. Anyone else only once a SuperAdmin has set
/// <c>User.FinanceAccess</c> on their account — the same per-account grant as
/// <c>CustomerRules</c> uses for tax details, and read from the database on
/// each call, not from the token, so taking it away applies to the very next
/// request. It is one right for the dashboard widgets and the pages behind
/// them, so a widget never shows an amount its viewer cannot open.
/// </remarks>
public static class FinanceRules
{
    /// <summary>
    /// Whether an account of this role may be given a finance grant at all.
    /// The customer's rule: figures in euro are for the Super Admin and whoever
    /// they choose, and that choice is fixed so it cannot fall on a worker.
    /// </summary>
    public static bool CanBeGranted(UserRole? role) =>
        role is UserRole.SuperAdmin or UserRole.Admin
            or UserRole.ProjectManager or UserRole.Foreman;

    public static async Task<FinanceAccess> ResolveAsync(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        CancellationToken cancellationToken)
    {
        if (currentUserService.Role == UserRole.SuperAdmin)
        {
            return FinanceAccess.Full;
        }

        // A grant can never reach a worker or a customer login, even if a
        // value was stored on the row before that rule existed.
        if (!CanBeGranted(currentUserService.Role) || currentUserService.UserId is not { } userId)
        {
            return FinanceAccess.None;
        }

        return await context.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.FinanceAccess)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Whether the caller may see what people are paid — rates, pay entries and
    /// every figure derived from them. Two things at once: a role that pay may
    /// ever be shown to (<see cref="Costs.CostRules.CanSeeLabourCost"/>), and the
    /// full finance grant, which only a Super Admin hands out. A project manager
    /// without the grant sees the hours, never the euro.
    /// </summary>
    public static async Task<bool> CanSeePayAsync(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        CancellationToken cancellationToken) =>
        Costs.CostRules.CanSeeLabourCost(currentUserService.Role)
        && await ResolveAsync(context, currentUserService, cancellationToken) == FinanceAccess.Full;

    /// <summary>
    /// Whether the caller may see what was spent — the amounts on recorded
    /// materials, fuel, tools, vehicles and accommodation. A site role may
    /// still <em>record</em> spending (<see cref="Costs.CostRules.CanRecordSpending"/>);
    /// reading the euro back needs the full finance grant, so a foreman sees
    /// quantities and litres but not what they cost.
    /// </summary>
    public static async Task<bool> CanSeeSpendingAsync(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        CancellationToken cancellationToken) =>
        Costs.CostRules.CanSeeSpending(currentUserService.Role)
        && await ResolveAsync(context, currentUserService, cancellationToken) == FinanceAccess.Full;

    /// <summary>
    /// Refuses unless the caller may see statistics — the derived percentages,
    /// which <see cref="FinanceAccess.StatisticsOnly"/> and <see cref="FinanceAccess.Full"/> both allow.
    /// </summary>
    public static async Task EnsureStatisticsAsync(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        CancellationToken cancellationToken)
    {
        if (await ResolveAsync(context, currentUserService, cancellationToken) == FinanceAccess.None)
        {
            throw new ForbiddenAccessException("You may not see the company's finances.");
        }
    }

    /// <summary>Refuses unless the caller may see the amounts themselves.</summary>
    public static async Task EnsureFullAsync(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        CancellationToken cancellationToken)
    {
        if (await ResolveAsync(context, currentUserService, cancellationToken) != FinanceAccess.Full)
        {
            throw new ForbiddenAccessException("You may not see the company's finances.");
        }
    }
}
