using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Common.Security;
using Construction.Application.Features.Authentication.Mfa;
using Construction.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Authentication.Commands.Mfa;

// The four things a person (or a Super Admin on their behalf) does with a
// second factor: start enrolling, confirm it, switch it off, and — for somebody
// who lost their phone and their recovery codes — have it cleared.

public record MfaSetupDto(string Secret, string ProvisioningUri);

public record MfaEnabledDto(IReadOnlyList<string> RecoveryCodes);

/// <summary>Creates a secret and returns it for the authenticator. Nothing is required at sign-in until it is confirmed.</summary>
public record BeginMfaSetupCommand : IRequest<MfaSetupDto>;

public class BeginMfaSetupCommandHandler : IRequestHandler<BeginMfaSetupCommand, MfaSetupDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IMfaService _mfa;

    public BeginMfaSetupCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser, IMfaService mfa)
    {
        _context = context;
        _currentUser = currentUser;
        _mfa = mfa;
    }

    public async Task<MfaSetupDto> Handle(BeginMfaSetupCommand request, CancellationToken cancellationToken)
    {
        if (!_mfa.IsAvailable)
        {
            throw new ConflictException("A second factor is not set up on this server.");
        }

        var userId = _currentUser.UserId ?? throw new UnauthorizedException("User is not authenticated.");
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new UnauthorizedException("User is not authenticated.");

        if (user.HasMfa)
        {
            throw new ConflictException("A second factor is already switched on. Switch it off first to set it up again.");
        }

        var secret = Totp.NewSecret();
        user.MfaSecretProtected = _mfa.Protect(secret, user.Id);
        user.MfaEnabledAt = null;
        user.MfaLastUsedStep = null;

        await _context.SaveChangesAsync(cancellationToken);

        return new MfaSetupDto(Totp.ToBase32(secret), Totp.ProvisioningUri(_mfa.Issuer, user.Email, secret));
    }
}

/// <summary>Confirms the secret with a first code, switches the factor on and hands out the recovery codes — once.</summary>
public record EnableMfaCommand : IRequest<MfaEnabledDto>
{
    public string Code { get; init; } = null!;
}

public class EnableMfaCommandValidator : AbstractValidator<EnableMfaCommand>
{
    public EnableMfaCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(16);
    }
}

public class EnableMfaCommandHandler : IRequestHandler<EnableMfaCommand, MfaEnabledDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IMfaService _mfa;
    private readonly IDateTimeProvider _clock;

    public EnableMfaCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IMfaService mfa,
        IDateTimeProvider clock)
    {
        _context = context;
        _currentUser = currentUser;
        _mfa = mfa;
        _clock = clock;
    }

    public async Task<MfaEnabledDto> Handle(EnableMfaCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedException("User is not authenticated.");
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new UnauthorizedException("User is not authenticated.");

        if (user.HasMfa)
        {
            throw new ConflictException("A second factor is already switched on.");
        }

        var secret = user.MfaSecretProtected is null ? null : _mfa.Unprotect(user.MfaSecretProtected, user.Id);
        if (secret is null)
        {
            throw new ConflictException("Start the setup first.");
        }

        var utcNow = _clock.UtcNow;
        var step = Totp.Verify(secret, request.Code, utcNow, null)
            ?? throw new UnauthorizedException("The code is not valid.");

        user.MfaEnabledAt = utcNow;
        user.MfaLastUsedStep = step;

        var old = await _context.MfaRecoveryCodes.Where(c => c.UserId == user.Id).ToListAsync(cancellationToken);
        _context.MfaRecoveryCodes.RemoveRange(old);

        var codes = RecoveryCodes.Generate();
        foreach (var code in codes)
        {
            _context.MfaRecoveryCodes.Add(new MfaRecoveryCode
            {
                UserId = user.Id,
                CodeHash = TokenHasher.Sha256(RecoveryCodes.Normalize(code)),
            });
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new MfaEnabledDto(codes);
    }
}

/// <summary>Switches the factor off. Needs the password and a current code, so a borrowed open session is not enough.</summary>
public record DisableMfaCommand : IRequest
{
    public string Password { get; init; } = null!;

    public string Code { get; init; } = null!;
}

public class DisableMfaCommandValidator : AbstractValidator<DisableMfaCommand>
{
    public DisableMfaCommandValidator()
    {
        RuleFor(x => x.Password).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(32);
    }
}

public class DisableMfaCommandHandler : IRequestHandler<DisableMfaCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IMfaService _mfa;
    private readonly IDateTimeProvider _clock;

    public DisableMfaCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IPasswordHasher passwordHasher,
        IMfaService mfa,
        IDateTimeProvider clock)
    {
        _context = context;
        _currentUser = currentUser;
        _passwordHasher = passwordHasher;
        _mfa = mfa;
        _clock = clock;
    }

    public async Task Handle(DisableMfaCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedException("User is not authenticated.");
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new UnauthorizedException("User is not authenticated.");

        if (!user.HasMfa)
        {
            return;
        }

        if (_mfa.IsRequiredFor(user.Role))
        {
            throw new ConflictException("This role must use a second factor. Ask a Super Admin to reset it if it was lost.");
        }

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedException("Current password is incorrect.");
        }

        var secret = _mfa.Unprotect(user.MfaSecretProtected!, user.Id);
        if (secret is null || Totp.Verify(secret, request.Code, _clock.UtcNow, user.MfaLastUsedStep) is null)
        {
            throw new UnauthorizedException("The code is not valid.");
        }

        await ClearAsync(_context, user, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    internal static async Task ClearAsync(IApplicationDbContext context, User user, CancellationToken cancellationToken)
    {
        user.MfaSecretProtected = null;
        user.MfaEnabledAt = null;
        user.MfaLastUsedStep = null;

        var codes = await context.MfaRecoveryCodes.Where(c => c.UserId == user.Id).ToListAsync(cancellationToken);
        context.MfaRecoveryCodes.RemoveRange(codes);

        var challenges = await context.MfaChallenges.Where(c => c.UserId == user.Id).ToListAsync(cancellationToken);
        context.MfaChallenges.RemoveRange(challenges);
    }
}

/// <summary>A Super Admin clears somebody's second factor — the phone was lost and so were the codes — and ends their sessions.</summary>
public record ResetUserMfaCommand(Guid UserId) : IRequest;

public class ResetUserMfaCommandHandler : IRequestHandler<ResetUserMfaCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeProvider _clock;

    public ResetUserMfaCommandHandler(IApplicationDbContext context, IDateTimeProvider clock)
    {
        _context = context;
        _clock = clock;
    }

    public async Task Handle(ResetUserMfaCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), request.UserId);

        await DisableMfaCommandHandler.ClearAsync(_context, user, cancellationToken);

        // Whoever held the old factor may be the reason it is being reset.
        var utcNow = _clock.UtcNow;
        var sessions = await _context.RefreshTokens
            .Where(rt => rt.UserId == user.Id && rt.RevokedAt == null && rt.ExpiresAt > utcNow)
            .ToListAsync(cancellationToken);

        foreach (var session in sessions)
        {
            session.RevokedAt = utcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
