using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Common.Security;
using Construction.Application.Features.Authentication.Commands.Login;
using Construction.Application.Features.Authentication.Mfa;
using Construction.Application.Features.Authentication.Models;
using Construction.Application.Features.Authentication.Services;
using Construction.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Authentication.Commands.VerifyMfa;

/// <summary>Second step of a sign-in: the challenge from the password step plus a code.</summary>
public record VerifyMfaCommand : IRequest<AuthResponse>
{
    public string MfaToken { get; init; } = null!;

    /// <summary>A six-digit code from the authenticator, or one of the recovery codes.</summary>
    public string Code { get; init; } = null!;

    /// <summary>Set by the API layer from the connection, never from the request body.</summary>
    public string? IpAddress { get; init; }

    public string? UserAgent { get; init; }
}

public class VerifyMfaCommandValidator : AbstractValidator<VerifyMfaCommand>
{
    public VerifyMfaCommandValidator()
    {
        RuleFor(x => x.MfaToken).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(32);
    }
}

public class VerifyMfaCommandHandler : IRequestHandler<VerifyMfaCommand, AuthResponse>
{
    private const string InvalidMessage = "The code is not valid.";

    private readonly IApplicationDbContext _context;
    private readonly IMfaService _mfa;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ISignInCompleter _signIn;

    public VerifyMfaCommandHandler(
        IApplicationDbContext context,
        IMfaService mfa,
        IDateTimeProvider dateTimeProvider,
        ISignInCompleter signIn)
    {
        _context = context;
        _mfa = mfa;
        _dateTimeProvider = dateTimeProvider;
        _signIn = signIn;
    }

    public async Task<AuthResponse> Handle(VerifyMfaCommand request, CancellationToken cancellationToken)
    {
        var utcNow = _dateTimeProvider.UtcNow;
        var tokenHash = TokenHasher.Sha256(request.MfaToken);

        var challenge = await _context.MfaChallenges
            .Include(c => c.User).ThenInclude(u => u.Employee)
            .FirstOrDefaultAsync(c => c.TokenHash == tokenHash, cancellationToken);

        if (challenge is null || !challenge.IsOpen(utcNow))
        {
            throw new UnauthorizedException(InvalidMessage);
        }

        var user = challenge.User;

        if (!user.IsActive || user.IsLockedOut(utcNow) || !user.HasMfa)
        {
            throw new UnauthorizedException(InvalidMessage);
        }

        if (await CodeIsGoodAsync(user, request.Code, utcNow, cancellationToken))
        {
            challenge.UsedAt = utcNow;
            return await _signIn.CompleteAsync(user, request.IpAddress, request.UserAgent, cancellationToken);
        }

        challenge.Attempts++;

        if (challenge.Attempts >= MfaChallenge.MaxAttempts)
        {
            // Spent: the person has to start over with the password, and the
            // misses count toward the account lockout so this cannot go on forever.
            challenge.UsedAt = utcNow;
            LoginCommandHandler.RegisterFailedAttempt(user, utcNow);
        }

        _context.FailedLogins.Add(new FailedLogin
        {
            UserId = user.Id,
            Email = user.Email.Length > 256 ? user.Email[..256] : user.Email,
            Reason = "WrongMfaCode",
            IpAddress = request.IpAddress,
            UserAgent = request.UserAgent is { Length: > 200 } agent ? agent[..200] : request.UserAgent,
            OccurredAt = utcNow,
        });

        await _context.SaveChangesAsync(cancellationToken);

        throw new UnauthorizedException(InvalidMessage);
    }

    private async Task<bool> CodeIsGoodAsync(
        User user,
        string code,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        if (RecoveryCodes.LooksLikeRecoveryCode(code))
        {
            var hash = TokenHasher.Sha256(RecoveryCodes.Normalize(code));

            var recovery = await _context.MfaRecoveryCodes
                .FirstOrDefaultAsync(
                    c => c.UserId == user.Id && c.CodeHash == hash && c.UsedAt == null,
                    cancellationToken);

            if (recovery is null)
            {
                return false;
            }

            recovery.UsedAt = utcNow;
            return true;
        }

        var secret = user.MfaSecretProtected is null ? null : _mfa.Unprotect(user.MfaSecretProtected, user.Id);
        if (secret is null)
        {
            return false;
        }

        var step = Totp.Verify(secret, code, utcNow, user.MfaLastUsedStep);
        if (step is null)
        {
            return false;
        }

        user.MfaLastUsedStep = step;
        return true;
    }
}
