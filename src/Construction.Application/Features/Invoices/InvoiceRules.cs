using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Finance;
using Construction.Domain.Enums;

namespace Construction.Application.Features.Invoices;

/// <summary>
/// Who may see and write invoices, and how an invoice is split among the client's companies.
/// </summary>
/// <remarks>
/// Invoices are money and the customer's rule is that nothing in euro is shown to anyone but the
/// Super Admin and whoever the Super Admin picks, so every invoice call needs the full finance
/// grant on top of the office role.
/// </remarks>
public static class InvoiceRules
{
    /// <summary>Management (Super Admin and Admin) works with invoices, and only with the grant.</summary>
    public static async Task EnsureAllowedAsync(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        CancellationToken cancellationToken)
    {
        if (currentUserService.Role is not (UserRole.SuperAdmin or UserRole.Admin))
        {
            throw new ForbiddenAccessException("Only management may work with invoices.");
        }

        await FinanceRules.EnsureFullAsync(context, currentUserService, cancellationToken);
    }

    /// <summary>
    /// Splits an amount evenly among <paramref name="parts"/> parts to the cent; whatever a cent
    /// cannot divide goes to the first parts, one cent each, so the parts always add up exactly.
    /// </summary>
    public static IReadOnlyList<decimal> SplitEvenly(decimal amount, int parts)
    {
        if (parts <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(parts));
        }

        var cents = (long)Math.Round(amount * 100m, MidpointRounding.AwayFromZero);
        var each = cents / parts;
        var remainder = (int)(cents % parts);
        var step = cents < 0 ? -1 : 1;
        var result = new List<decimal>(parts);

        for (var i = 0; i < parts; i++)
        {
            var extra = i < Math.Abs(remainder) ? step : 0;
            result.Add((each + extra) / 100m);
        }

        return result;
    }
}
