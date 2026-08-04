using AutoMapper;
using AutoMapper.QueryableExtensions;
using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Materials.Models;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Construction.Application.Features.Materials.Commands.AdjustMaterialQuantity;

/// <summary>
/// Applies a relative stock correction (positive = found, negative = short).
/// </summary>
/// <remarks>
/// Kept as its own endpoint after movements arrived, because it is what the
/// stock screen's "+/-" does and rewriting that was not worth breaking the
/// clients over. It now writes a
/// <see cref="Domain.Enums.MaterialMovementKind.Adjustment"/> alongside the
/// update: a change to the quantity that left no movement behind would make
/// the running total stop matching the history it is supposed to summarise,
/// with nothing to say which of the two was wrong.
///
/// A correction rather than a delivery or an issue, deliberately. This path
/// has no site and no price, so counting it as consumption would put
/// unexplained losses onto somebody's project.
/// </remarks>
public record AdjustMaterialQuantityCommand : IRequest<MaterialDto>
{
    /// <summary>Set by the API layer from the route, never from the request body.</summary>
    public Guid Id { get; init; }

    public decimal Change { get; init; }

    public string? Reason { get; init; }
}

public class AdjustMaterialQuantityCommandValidator : AbstractValidator<AdjustMaterialQuantityCommand>
{
    public AdjustMaterialQuantityCommandValidator()
    {
        RuleFor(x => x.Change)
            .NotEqual(0).WithMessage("Change must not be zero.");

        RuleFor(x => x.Reason)
            .MaximumLength(500);
    }
}

public class AdjustMaterialQuantityCommandHandler
    : IRequestHandler<AdjustMaterialQuantityCommand, MaterialDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;
    private readonly ILogger<AdjustMaterialQuantityCommandHandler> _logger;

    public AdjustMaterialQuantityCommandHandler(
        IApplicationDbContext context,
        IDateTimeProvider dateTimeProvider,
        ICurrentUserService currentUserService,
        IMapper mapper,
        ILogger<AdjustMaterialQuantityCommandHandler> logger)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
        _currentUserService = currentUserService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<MaterialDto> Handle(
        AdjustMaterialQuantityCommand request,
        CancellationToken cancellationToken)
    {
        var utcNow = _dateTimeProvider.UtcNow;

        // The update and the movement land together, so the running total can
        // never drift from the history behind it.
        await _context.ExecuteInTransactionAsync(
            async token =>
            {
                // Conditional UPDATE: applies the delta only when the result
                // stays non-negative, making concurrent adjustments race-safe.
                var updated = await _context.Materials
                    .Where(m => m.Id == request.Id && m.Quantity + request.Change >= 0)
                    .ExecuteUpdateAsync(
                        setters => setters
                            .SetProperty(m => m.Quantity, m => m.Quantity + request.Change)
                            .SetProperty(m => m.LastUpdated, utcNow)
                            .SetProperty(m => m.UpdatedAt, utcNow),
                        token);

                if (updated == 0)
                {
                    var exists = await _context.Materials
                        .AnyAsync(m => m.Id == request.Id, token);

                    throw exists
                        ? new ConflictException(
                            "The adjustment would make the stock quantity negative.")
                        : new NotFoundException(nameof(Material), request.Id);
                }

                _context.MaterialMovements.Add(new MaterialMovement
                {
                    MaterialId = request.Id,
                    Kind = MaterialMovementKind.Adjustment,
                    Quantity = request.Change,
                    OccurredOn = DateOnly.FromDateTime(utcNow),
                    Note = request.Reason?.Trim(),
                    RecordedByUserId = _currentUserService.UserId
                });

                await _context.SaveChangesAsync(token);
            },
            cancellationToken);

        var material = await _context.Materials
            .AsNoTracking()
            .Where(m => m.Id == request.Id)
            .ProjectTo<MaterialDto>(_mapper.ConfigurationProvider)
            .FirstAsync(cancellationToken);

        _logger.LogInformation(
            "Material {MaterialId} adjusted by {Change} to {Quantity} by user {UserId}. Reason: {Reason}",
            request.Id, request.Change, material.Quantity,
            _currentUserService.UserId, request.Reason ?? "(none)");

        return material;
    }
}
