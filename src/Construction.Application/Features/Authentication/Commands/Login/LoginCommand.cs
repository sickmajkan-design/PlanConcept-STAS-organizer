using System.Security.Cryptography;
using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Common.Security;
using Construction.Application.Features.Authentication.Models;
using Construction.Application.Features.Authentication.Services;
using Construction.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Authentication.Commands.Login;

public record LoginCommand : IRequest<AuthResponse>
{
    public string Email { get; init; } = null!;

    public string Password { get; init; } = null!;

    /// <summary>Set by the API layer from the connection, never from the request body.</summary>
    public string? IpAddress { get; init; }

    /// <summary>Set by the API layer from the User-Agent header, for the sign-in record.</summary>
    public string? UserAgent { get; init; }
}

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email is not a valid email address.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.");
    }
}

public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponse>
{
    private const string InvalidCredentialsMessage = "Invalid email or password.";

    /// <summary>Consecutive failures that trigger a lockout.</summary>
    public const int MaxFailedAttempts = 10;

    /// <summary>How long a locked account stays locked. Clears itself.</summary>
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    /// <summary>How long somebody has to type the code after the password was accepted.</summary>
    public static readonly TimeSpan ChallengeLifetime = TimeSpan.FromMinutes(5);

    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ISignInCompleter _signIn;

    public LoginCommandHandler(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IDateTimeProvider dateTimeProvider,
        ISignInCompleter signIn)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _dateTimeProvider = dateTimeProvider;
        _signIn = signIn;
    }

    public async Task<AuthResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var utcNow = _dateTimeProvider.UtcNow;

        var user = await _context.Users
            .Include(u => u.Employee)
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

        // Always derive a hash, even when no account matched, so an unknown
        // address costs the same as a known one. Short-circuiting here made
        // unknown addresses ~13x faster, which is a reliable oracle for
        // enumerating who works here.
        var passwordMatches = _passwordHasher.Verify(
            request.Password,
            user?.PasswordHash ?? _passwordHasher.DummyHash);

        if (user is null)
        {
            await RecordFailureAsync(request, null, "UnknownAccount", utcNow, cancellationToken);
            throw new UnauthorizedException(InvalidCredentialsMessage);
        }

        // Checked after the hash so a locked account is not faster than an
        // unlocked one, and reported with the same message so the lockout
        // state cannot be used to confirm an address exists.
        if (user.IsLockedOut(utcNow))
        {
            await RecordFailureAsync(request, user, "LockedOut", utcNow, cancellationToken);
            throw new UnauthorizedException(InvalidCredentialsMessage);
        }

        if (!passwordMatches)
        {
            RegisterFailedAttempt(user, utcNow);
            await RecordFailureAsync(request, user, "WrongPassword", utcNow, cancellationToken);

            throw new UnauthorizedException(InvalidCredentialsMessage);
        }

        if (!user.IsActive)
        {
            await RecordFailureAsync(request, user, "Deactivated", utcNow, cancellationToken);
            throw new UnauthorizedException("This account has been deactivated.");
        }

        // The plaintext is in hand only now. Saved with the same
        // SaveChangesAsync as the rest of the sign-in below.
        if (_passwordHasher.NeedsRehash(user.PasswordHash))
        {
            user.PasswordHash = _passwordHasher.Hash(request.Password);
        }

        // A second factor is still owed: park the sign-in in a challenge instead
        // of issuing tokens. The failure counter is deliberately NOT cleared
        // here — a thief who knows the password must not get an endless supply
        // of fresh code guesses by signing in again and again.
        if (user.HasMfa)
        {
            var rawToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

            _context.MfaChallenges.Add(new MfaChallenge
            {
                UserId = user.Id,
                TokenHash = TokenHasher.Sha256(rawToken),
                ExpiresAt = utcNow.Add(ChallengeLifetime),
            });

            await _context.SaveChangesAsync(cancellationToken);

            return new AuthResponse { MfaRequired = true, MfaToken = rawToken };
        }

        return await _signIn.CompleteAsync(user, request.IpAddress, request.UserAgent, cancellationToken);
    }

    /// <summary>
    /// Counts a failed attempt and locks the account once too many pile up.
    ///
    /// <para>
    /// Tracked per account rather than per address on purpose: an attacker
    /// picks their own source address — and can spoof a forwarded one if a
    /// proxy is misconfigured — but cannot pick the account they are trying to
    /// break into. The lockout is short and self-clearing, so the denial of
    /// service it enables against a known user is bounded.
    /// </para>
    /// </summary>
    /// <summary>
    /// Keeps a record of a refused sign-in (and saves any pending lockout
    /// counter change with it). Every refusal writes exactly one row, so an
    /// unknown address costs the same as a known one. Purged after
    /// <c>Retention:FailedLoginDays</c>; the credential endpoints are rate
    /// limited, which bounds how fast this table can grow.
    /// </summary>
    private async Task RecordFailureAsync(
        LoginCommand request,
        User? user,
        string reason,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        _context.FailedLogins.Add(new FailedLogin
        {
            UserId = user?.Id,
            Email = email.Length > 256 ? email[..256] : email,
            Reason = reason,
            IpAddress = request.IpAddress,
            UserAgent = request.UserAgent is { Length: > 200 } agent
                ? agent[..200]
                : request.UserAgent,
            OccurredAt = utcNow,
        });

        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Counts a failed attempt and locks the account at the limit. Public so the code step counts toward the same lockout.</summary>
    public static void RegisterFailedAttempt(User user, DateTime utcNow)
    {
        user.FailedLoginAttempts++;

        if (user.FailedLoginAttempts >= MaxFailedAttempts)
        {
            user.LockoutEndsAt = utcNow.Add(LockoutDuration);
            user.FailedLoginAttempts = 0;
        }
    }
}
