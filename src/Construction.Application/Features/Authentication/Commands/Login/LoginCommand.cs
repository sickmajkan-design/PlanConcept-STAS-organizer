using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
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

    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuthTokenService _authTokenService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public LoginCommandHandler(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IAuthTokenService authTokenService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _authTokenService = authTokenService;
        _dateTimeProvider = dateTimeProvider;
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
            throw new UnauthorizedException(InvalidCredentialsMessage);
        }

        // Checked after the hash so a locked account is not faster than an
        // unlocked one, and reported with the same message so the lockout
        // state cannot be used to confirm an address exists.
        if (user.IsLockedOut(utcNow))
        {
            throw new UnauthorizedException(InvalidCredentialsMessage);
        }

        if (!passwordMatches)
        {
            RegisterFailedAttempt(user, utcNow);
            await _context.SaveChangesAsync(cancellationToken);

            throw new UnauthorizedException(InvalidCredentialsMessage);
        }

        if (!user.IsActive)
        {
            throw new UnauthorizedException("This account has been deactivated.");
        }

        user.FailedLoginAttempts = 0;
        user.LockoutEndsAt = null;
        user.LastLoginAt = utcNow;

        var response = _authTokenService.IssueTokens(user, request.IpAddress, out _);

        await _context.SaveChangesAsync(cancellationToken);

        return response;
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
    private static void RegisterFailedAttempt(User user, DateTime utcNow)
    {
        user.FailedLoginAttempts++;

        if (user.FailedLoginAttempts >= MaxFailedAttempts)
        {
            user.LockoutEndsAt = utcNow.Add(LockoutDuration);
            user.FailedLoginAttempts = 0;
        }
    }
}
