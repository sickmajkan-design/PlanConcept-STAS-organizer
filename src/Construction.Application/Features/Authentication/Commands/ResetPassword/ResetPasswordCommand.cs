using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Common.Security;
using Construction.Application.Common.Validation;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Authentication.Commands.ResetPassword;

/// <summary>
/// Completes the password-reset flow using the token emailed to the user.
/// </summary>
public record ResetPasswordCommand : IRequest
{
    public string Email { get; init; } = null!;

    public string Token { get; init; } = null!;

    public string NewPassword { get; init; } = null!;
}

public class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email is not a valid email address.");

        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Reset token is required.");

        RuleFor(x => x.NewPassword)
            .StrongPassword();
    }
}

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand>
{
    private const string InvalidTokenMessage = "The password reset link is invalid or has expired.";

    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ResetPasswordCommandHandler(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var tokenHash = TokenHasher.Sha256(request.Token);
        var utcNow = _dateTimeProvider.UtcNow;

        var resetToken = await _context.PasswordResetTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(
                t => t.TokenHash == tokenHash && t.User.Email == normalizedEmail,
                cancellationToken);

        if (resetToken is null || !resetToken.IsValid(utcNow) || !resetToken.User.IsActive)
        {
            throw new UnauthorizedException(InvalidTokenMessage);
        }

        resetToken.User.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        resetToken.UsedAt = utcNow;

        // Force every existing session to re-authenticate with the new password.
        var activeRefreshTokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == resetToken.UserId && rt.RevokedAt == null && rt.ExpiresAt > utcNow)
            .ToListAsync(cancellationToken);

        foreach (var refreshToken in activeRefreshTokens)
        {
            refreshToken.RevokedAt = utcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
