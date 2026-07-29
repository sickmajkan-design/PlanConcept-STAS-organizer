using Construction.Application.Common.Interfaces;
using Construction.Application.Common.Security;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Authentication.Commands.Logout;

/// <summary>
/// Revokes the presented refresh token. Idempotent: logging out with an
/// unknown or already-revoked token succeeds silently so clients can always
/// clear their local session.
/// </summary>
public record LogoutCommand : IRequest
{
    public string RefreshToken { get; init; } = null!;

    public string? IpAddress { get; init; }
}

public class LogoutCommandValidator : AbstractValidator<LogoutCommand>
{
    public LogoutCommandValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh token is required.");
    }
}

public class LogoutCommandHandler : IRequestHandler<LogoutCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public LogoutCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = TokenHasher.Sha256(request.RefreshToken);

        var storedToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, cancellationToken);

        // Only the owner of the token (or an unauthenticated client presenting
        // the raw token) may revoke it; never let one user revoke another's token.
        if (storedToken is null || storedToken.RevokedAt is not null)
        {
            return;
        }

        if (_currentUserService.UserId is { } userId && storedToken.UserId != userId)
        {
            return;
        }

        storedToken.RevokedAt = _dateTimeProvider.UtcNow;
        storedToken.RevokedByIp = request.IpAddress;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
