using Construction.Application.Common.Interfaces;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Customers;

/// <summary>
/// Who may see and change a customer's tax ID, registration number and VAT
/// number.
/// </summary>
/// <remarks>
/// These three are the one part of a customer record that is not simply
/// "office work" the way the rest of it is (name, contact, phone) — a tax
/// number is the kind of thing a SuperAdmin wants to hand out on a
/// case-by-case basis rather than to every Admin by default. So unlike the
/// rest of <c>CustomerCommandBase</c>, which follows <c>ForemanAndAbove</c>
/// like the rest of the entity, these three fields have their own narrower
/// gate: a SuperAdmin always sees and edits them, and everyone else only once
/// a SuperAdmin has explicitly granted <c>User.CanViewCustomerTaxDetails</c>
/// — which is itself a read grant, not a write one; editing stays
/// SuperAdmin-only so the set of people who can plant a wrong tax number
/// stays small even as the set who can read one grows.
/// </remarks>
public static class CustomerRules
{
    /// <param name="granted">The caller's own <c>User.CanViewCustomerTaxDetails</c> flag.</param>
    public static bool CanViewTaxDetails(UserRole? role, bool granted) =>
        role == UserRole.SuperAdmin || granted;

    public static bool CanEditTaxDetails(UserRole? role) =>
        role == UserRole.SuperAdmin;

    /// <summary>
    /// Whether the currently signed-in caller may see a customer's tax
    /// details — the DB lookup <see cref="CanViewTaxDetails"/> needs, since
    /// the grant lives on the user's own row and not in the auth token.
    /// SuperAdmin never needs the lookup at all.
    /// </summary>
    public static async Task<bool> ResolveCanViewTaxDetailsAsync(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        CancellationToken cancellationToken)
    {
        if (currentUserService.Role == UserRole.SuperAdmin)
        {
            return true;
        }

        if (currentUserService.UserId is not { } userId)
        {
            return false;
        }

        return await context.Users
            .Where(u => u.Id == userId)
            .Select(u => u.CanViewCustomerTaxDetails)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
