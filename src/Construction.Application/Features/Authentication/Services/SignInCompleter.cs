using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Authentication.Models;
using Construction.Application.Features.Maintenance.Commands.CatchUpReminders;
using Construction.Application.Features.Maintenance.Commands.PurgeOrphanedNotifications;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Construction.Application.Features.Authentication.Services;

/// <summary>
/// What happens once somebody has fully proved who they are: the counters
/// clear, tokens are issued, the sign-in is recorded. Shared by the password
/// step and, for accounts with a second factor, the code step — so there is one
/// place that decides what "signed in" means.
/// </summary>
public interface ISignInCompleter
{
    Task<AuthResponse> CompleteAsync(
        User user,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken);
}

public class SignInCompleter : ISignInCompleter
{
    private readonly IApplicationDbContext _context;
    private readonly IAuthTokenService _authTokenService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IMfaService _mfa;
    private readonly ISender _sender;
    private readonly ILogger<SignInCompleter> _logger;

    public SignInCompleter(
        IApplicationDbContext context,
        IAuthTokenService authTokenService,
        IDateTimeProvider dateTimeProvider,
        IMfaService mfa,
        ISender sender,
        ILogger<SignInCompleter> logger)
    {
        _context = context;
        _authTokenService = authTokenService;
        _dateTimeProvider = dateTimeProvider;
        _mfa = mfa;
        _sender = sender;
        _logger = logger;
    }

    public async Task<AuthResponse> CompleteAsync(
        User user,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken)
    {
        var utcNow = _dateTimeProvider.UtcNow;

        user.FailedLoginAttempts = 0;
        user.LockoutEndsAt = null;
        user.LastLoginAt = utcNow;

        var issued = _authTokenService.IssueTokens(user, ipAddress, out _);

        var response = new AuthResponse
        {
            AccessToken = issued.AccessToken,
            AccessTokenExpiresAt = issued.AccessTokenExpiresAt,
            RefreshToken = issued.RefreshToken,
            RefreshTokenExpiresAt = issued.RefreshTokenExpiresAt,
            User = issued.User,
            MfaEnrollmentRequired = !user.HasMfa && _mfa.IsAvailable && _mfa.IsRequiredFor(user.Role),
        };

        // One row per sign-in, not per request: this is the "who signed in
        // when" record, and its LastSeenAt is refreshed in the background.
        _context.UserSessions.Add(new UserSession
        {
            UserId = user.Id,
            IpAddress = ipAddress,
            UserAgent = userAgent is { Length: > 200 } agent ? agent[..200] : userAgent,
            StartedAt = utcNow,
            LastSeenAt = utcNow,
        });

        await _context.SaveChangesAsync(cancellationToken);

        await RunBestEffortAsync(new PurgeOrphanedNotificationsCommand(), "Notification reconciliation", cancellationToken);

        if (user.Role is UserRole.SuperAdmin or UserRole.Admin)
        {
            await RunBestEffortAsync(new CatchUpRemindersCommand(), "Reminder catch-up", cancellationToken);
        }

        return response;
    }

    /// <summary>Drops stale notifications and sends due reminders at first sign-in; a failure must never stop a login.</summary>
    private async Task RunBestEffortAsync(IRequest<int> request, string what, CancellationToken cancellationToken)
    {
        try
        {
            await _sender.Send(request, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogWarning(exception, "{Step} at login failed.", what);
        }
    }
}
