using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Common.Security;
using Construction.Application.Features.Authentication.Models;
using Construction.Application.Features.Authentication.Services;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Authentication.Commands.RefreshToken;

public record RefreshTokenCommand : IRequest<AuthResponse>
{
    public string RefreshToken { get; init; } = null!;

    /// <summary>Set by the API layer from the connection, never from the request body.</summary>
    public string? IpAddress { get; init; }
}

public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh token is required.");
    }
}

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResponse>
{
    private const string InvalidTokenMessage = "Invalid or expired refresh token.";

    private readonly IApplicationDbContext _context;
    private readonly IAuthTokenService _authTokenService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RefreshTokenCommandHandler(
        IApplicationDbContext context,
        IAuthTokenService authTokenService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _authTokenService = authTokenService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<AuthResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = TokenHasher.Sha256(request.RefreshToken);
        var utcNow = _dateTimeProvider.UtcNow;

        var storedToken = await _context.RefreshTokens
            .Include(rt => rt.User)
            .ThenInclude(u => u.Employee)
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, cancellationToken);

        if (storedToken is null)
        {
            throw new UnauthorizedException(InvalidTokenMessage);
        }

        if (storedToken.RevokedAt is not null)
        {
            // Reuse of a rotated token indicates the token may have been stolen.
            // Revoke every active token for the user to force a fresh login everywhere.
            await RevokeAllActiveTokensAsync(storedToken.UserId, request.IpAddress, utcNow, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            throw new UnauthorizedException(InvalidTokenMessage);
        }

        if (storedToken.ExpiresAt <= utcNow || !storedToken.User.IsActive)
        {
            throw new UnauthorizedException(InvalidTokenMessage);
        }

        var response = _authTokenService.IssueTokens(storedToken.User, request.IpAddress, out var newToken);

        storedToken.RevokedAt = utcNow;
        storedToken.RevokedByIp = request.IpAddress;
        storedToken.ReplacedByTokenHash = newToken.TokenHash;

        await _context.SaveChangesAsync(cancellationToken);

        return response;
    }

    private async Task RevokeAllActiveTokensAsync(
        Guid userId,
        string? ipAddress,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var activeTokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null && rt.ExpiresAt > utcNow)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokens)
        {
            token.RevokedAt = utcNow;
            token.RevokedByIp = ipAddress;
        }
    }
}
