using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Notifications.Commands.RegisterDeviceToken;

/// <summary>
/// Registers (or refreshes) an FCM device token for the current user.
/// A token already registered to another account is re-assigned — the device
/// now belongs to whoever is logged in on it.
/// </summary>
public record RegisterDeviceTokenCommand : IRequest
{
    public string Token { get; init; } = null!;

    public DevicePlatform Platform { get; init; }
}

public class RegisterDeviceTokenCommandValidator : AbstractValidator<RegisterDeviceTokenCommand>
{
    public RegisterDeviceTokenCommandValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Device token is required.")
            .MaximumLength(512);

        RuleFor(x => x.Platform)
            .IsInEnum().WithMessage("Platform is required and must be a valid value.");
    }
}

public class RegisterDeviceTokenCommandHandler : IRequestHandler<RegisterDeviceTokenCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RegisterDeviceTokenCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task Handle(RegisterDeviceTokenCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedException("User is not authenticated.");

        var token = request.Token.Trim();

        var existing = await _context.DeviceTokens
            .FirstOrDefaultAsync(dt => dt.Token == token, cancellationToken);

        if (existing is null)
        {
            _context.DeviceTokens.Add(new DeviceToken
            {
                UserId = userId,
                Token = token,
                Platform = request.Platform,
                LastUsedAt = _dateTimeProvider.UtcNow
            });
        }
        else
        {
            existing.UserId = userId;
            existing.Platform = request.Platform;
            existing.LastUsedAt = _dateTimeProvider.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
