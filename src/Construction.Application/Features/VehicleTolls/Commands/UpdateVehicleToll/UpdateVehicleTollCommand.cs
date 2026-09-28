using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.VehicleTolls.Models;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.VehicleTolls.Commands.UpdateVehicleToll;

/// <summary>
/// Edits a toll's descriptive fields — type, country, route segment. Never
/// touches payment state; that is only ever changed through
/// <c>MarkVehicleTollPaidCommand</c>, so an edit can never accidentally
/// erase a paid toll's validity or history.
/// </summary>
public record UpdateVehicleTollCommand : IRequest<VehicleTollDto>
{
    /// <summary>Set by the API layer from the route, never from the request body.</summary>
    public Guid VehicleTollId { get; init; }

    public VehicleTollType Type { get; init; }

    public string Country { get; init; } = null!;

    public string? RouteSegment { get; init; }
}

public class UpdateVehicleTollCommandValidator : AbstractValidator<UpdateVehicleTollCommand>
{
    public UpdateVehicleTollCommandValidator()
    {
        RuleFor(x => x.VehicleTollId).NotEmpty();

        RuleFor(x => x.Type).IsInEnum();

        RuleFor(x => x.Country)
            .NotEmpty().WithMessage("Country is required.")
            .MaximumLength(100);

        RuleFor(x => x.RouteSegment).MaximumLength(200);
    }
}

public class UpdateVehicleTollCommandHandler : IRequestHandler<UpdateVehicleTollCommand, VehicleTollDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateVehicleTollCommandHandler(
        IApplicationDbContext context,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<VehicleTollDto> Handle(
        UpdateVehicleTollCommand request,
        CancellationToken cancellationToken)
    {
        var toll = await _context.VehicleTolls
            .Include(t => t.PaidByUser)
            .FirstOrDefaultAsync(t => t.Id == request.VehicleTollId, cancellationToken)
            ?? throw new NotFoundException(nameof(VehicleToll), request.VehicleTollId);

        toll.Type = request.Type;
        toll.Country = request.Country.Trim();
        toll.RouteSegment = string.IsNullOrWhiteSpace(request.RouteSegment)
            ? null
            : request.RouteSegment.Trim();

        await _context.SaveChangesAsync(cancellationToken);

        var today = DateOnly.FromDateTime(_dateTimeProvider.UtcNow);

        return VehicleTollMapping.ToDto(toll, today);
    }
}
