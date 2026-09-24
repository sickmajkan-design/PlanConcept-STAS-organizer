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
    public static async Task<FinanceAccess> ResolveAsync(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        CancellationToken cancellationToken)
    {
        if (currentUserService.Role == UserRole.SuperAdmin)
        {
            return FinanceAccess.Full;
        }

        if (currentUserService.UserId is not { } userId)
        {
            return FinanceAccess.None;
        }

        return await context.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.FinanceAccess)
            .FirstOrDefaultAsync(cancellationToken);
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
