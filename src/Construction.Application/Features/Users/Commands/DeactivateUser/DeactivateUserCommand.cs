using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Common.Security;
using Construction.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Users.Commands.DeactivateUser;

/// <summary>
/// Offboards an account: revokes access and stops anything still being
/// delivered to the person's devices.
/// </summary>
public record DeactivateUserCommand(Guid Id) : IRequest;

public class DeactivateUserCommandHandler : IRequestHandler<DeactivateUserCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public DeactivateUserCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task Handle(DeactivateUserCommand request, CancellationToken cancellationToken)
    {
        var callerRole = _currentUserService.Role
            ?? throw new UnauthorizedException("User is not authenticated.");

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("User", request.Id);

        // Checked before rank so the reason given is the accurate one. Acting
        // on your own account fails the rank test too — nobody outranks
        // themselves — but "you cannot deactivate your own account" tells the
        // caller what actually happened.
        if (user.Id == _currentUserService.UserId)
        {
            throw new ConflictException("You cannot deactivate your own account.");
        }

        RoleAdministration.EnsureCanManage(callerRole, user.Role);

        await EnsureNotTheLastSuperAdminAsync(user.Id, user.Role, cancellationToken);

        if (!user.IsActive)
        {
            // Already offboarded. Doing the revocation again is harmless but
            // returning early keeps the audit trail honest about when access
            // was actually withdrawn.
            return;
        }

        var utcNow = _dateTimeProvider.UtcNow;

        user.IsActive = false;

        // Being inactive already blocks refresh and sign-in. Revoking anyway
        // is what makes the audit trail say when each session ended, and means
        // re-activating the account does not silently resurrect old sessions.
        var activeRefreshTokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == user.Id && rt.RevokedAt == null && rt.ExpiresAt > utcNow)
            .ToListAsync(cancellationToken);

        foreach (var token in activeRefreshTokens)
        {
            token.RevokedAt = utcNow;
            token.RevokedByIp = _currentUserService.IpAddress;
        }

        // A reset link already in their inbox must not become a way back in.
        var outstandingResets = await _context.PasswordResetTokens
            .Where(t => t.UserId == user.Id && t.UsedAt == null && t.ExpiresAt > utcNow)
            .ToListAsync(cancellationToken);

        foreach (var token in outstandingResets)
        {
            token.UsedAt = utcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Push goes to a device, not through an access check, so a token left
        // registered would keep delivering project notifications to someone
        // who no longer works here.
        await _context.DeviceTokens
            .Where(dt => dt.UserId == user.Id)
            .ExecuteDeleteAsync(cancellationToken);
    }

    /// <summary>
    /// Refuses to remove the last way into the system. Without this, an
    /// administrator can lock everyone out and the only repair is direct
    /// database access.
    /// </summary>
    private async Task EnsureNotTheLastSuperAdminAsync(
        Guid userId,
        UserRole role,
        CancellationToken cancellationToken)
    {
        if (role != UserRole.SuperAdmin)
        {
            return;
        }

        var othersRemain = await _context.Users
            .AnyAsync(u => u.Id != userId && u.Role == UserRole.SuperAdmin && u.IsActive, cancellationToken);

        if (!othersRemain)
        {
            throw new ConflictException(
                "This is the only active Super Admin. Promote another account first.");
        }
    }
}
