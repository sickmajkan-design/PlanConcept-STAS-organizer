using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Common.Security;
using Construction.Application.Common.Validation;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Users.Commands.SetUserPassword;

/// <summary>
/// Sets an account's password directly.
///
/// <para>
/// The self-service reset needs a mailbox, and most site workers do not have a
/// company one. Without this, the only recovery for a forgotten password is a
/// database edit — which is how shared accounts and written-down passwords
/// start.
/// </para>
/// </summary>
public record SetUserPasswordCommand : IRequest
{
    public Guid Id { get; init; }

    public string NewPassword { get; init; } = null!;
}

public class SetUserPasswordCommandValidator : AbstractValidator<SetUserPasswordCommand>
{
    public SetUserPasswordCommandValidator()
    {
        RuleFor(x => x.NewPassword).StrongPassword();
    }
}

public class SetUserPasswordCommandHandler : IRequestHandler<SetUserPasswordCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IDateTimeProvider _dateTimeProvider;

    public SetUserPasswordCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IPasswordHasher passwordHasher,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _passwordHasher = passwordHasher;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task Handle(SetUserPasswordCommand request, CancellationToken cancellationToken)
    {
        var callerRole = _currentUserService.Role
            ?? throw new UnauthorizedException("User is not authenticated.");

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("User", request.Id);

        // Checked before rank so the reason given is the accurate one.
        // Changing your own password requires knowing the current one, which
        // this endpoint does not ask for. Route it to the normal flow rather
        // than offering a way around that check.
        if (user.Id == _currentUserService.UserId)
        {
            throw new ConflictException(
                "Use the change-password endpoint to set your own password.");
        }

        RoleAdministration.EnsureCanManage(callerRole, user.Role);

        var utcNow = _dateTimeProvider.UtcNow;

        user.PasswordHash = _passwordHasher.Hash(request.NewPassword);

        // A password the holder did not choose should not leave older sessions
        // running, and it clears a lockout so they can actually sign in.
        user.FailedLoginAttempts = 0;
        user.LockoutEndsAt = null;

        var activeTokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == user.Id && rt.RevokedAt == null && rt.ExpiresAt > utcNow)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokens)
        {
            token.RevokedAt = utcNow;
            token.RevokedByIp = _currentUserService.IpAddress;
        }

        // Any reset link already sent is superseded.
        var outstandingResets = await _context.PasswordResetTokens
            .Where(t => t.UserId == user.Id && t.UsedAt == null && t.ExpiresAt > utcNow)
            .ToListAsync(cancellationToken);

        foreach (var token in outstandingResets)
        {
            token.UsedAt = utcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
