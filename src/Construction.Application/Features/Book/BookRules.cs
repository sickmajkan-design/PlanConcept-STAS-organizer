using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Finance;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Book;

/// <summary>
/// Who may see and change the book of money, and the keys of the categories the system relies on.
/// </summary>
/// <remarks>
/// Reading needs the full finance grant (<see cref="FinanceRules"/>), which a Super Admin always has.
/// Changing needs it too, and a role of Admin or above: a project manager or foreman who was
/// given the grant may read the book but not write into it.
/// </remarks>
public static class BookRules
{
    public static class Keys
    {
        public const string Collection = "Collection";
        public const string WorkerPay = "WorkerPay";
        public const string Subcontractors = "Subcontractors";
        public const string Office = "Office";
        public const string Fuel = "Fuel";
        public const string Housing = "Housing";
        public const string CarRental = "CarRental";
        public const string Tools = "Tools";
        public const string Materials = "Materials";
        public const string FixedCosts = "FixedCosts";
        public const string Insurance = "Insurance";
        public const string Other = "Other";
    }

    public static bool CanWrite(UserRole? role) => role is UserRole.SuperAdmin or UserRole.Admin;

    /// <summary>
    /// Whether the caller may open the book at all: a Super Admin always, anyone else only a role that may be
    /// given finance grants and only once a Super Admin has set <c>User.BookAccess</c>. Read from the database
    /// on each call, so taking the grant away applies to the very next request.
    /// </summary>
    public static async Task<bool> HasAccessAsync(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        CancellationToken cancellationToken)
    {
        if (currentUserService.Role == UserRole.SuperAdmin)
        {
            return true;
        }

        if (!FinanceRules.CanBeGranted(currentUserService.Role) || currentUserService.UserId is not { } userId)
        {
            return false;
        }

        return await context.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.BookAccess)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public static async Task EnsureReadAsync(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        CancellationToken cancellationToken)
    {
        if (!await HasAccessAsync(context, currentUserService, cancellationToken))
        {
            throw new ForbiddenAccessException("You may not open the book of money.");
        }
    }

    public static async Task EnsureWriteAsync(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        CancellationToken cancellationToken)
    {
        if (!CanWrite(currentUserService.Role))
        {
            throw new ForbiddenAccessException("You may not change the book of money.");
        }

        await EnsureReadAsync(context, currentUserService, cancellationToken);
    }
}
