using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Notifications.Commands.UnregisterDeviceToken;

/// <summary>
/// Removes a device token (called on logout). Idempotent, and only the
/// token's current owner can remove it.
/// </summary>
public record UnregisterDeviceTokenCommand : IRequest
{
    public string Token { get; init; } = null!;
}

public class UnregisterDeviceTokenCommandValidator : AbstractValidator<UnregisterDeviceTokenCommand>
{
    public UnregisterDeviceTokenCommandValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Device token is required.")
            .MaximumLength(512);
    }
}

public class UnregisterDeviceTokenCommandHandler : IRequestHandler<UnregisterDeviceTokenCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public UnregisterDeviceTokenCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task Handle(UnregisterDeviceTokenCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedException("User is not authenticated.");

        await _context.DeviceTokens
            .Where(dt => dt.Token == request.Token.Trim() && dt.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
