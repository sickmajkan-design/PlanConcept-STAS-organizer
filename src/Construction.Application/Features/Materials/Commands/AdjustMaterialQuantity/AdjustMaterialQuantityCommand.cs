using AutoMapper;
using AutoMapper.QueryableExtensions;
using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Materials.Models;
using Construction.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Construction.Application.Features.Materials.Commands.AdjustMaterialQuantity;

/// <summary>
/// Applies a relative stock movement (positive = received, negative =
/// consumed). Executed as a single conditional UPDATE so concurrent
/// adjustments can never push the stock below zero.
/// </summary>
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
            .MaximumLength(512);
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

        // Single conditional UPDATE: applies the delta only when the result
        // stays non-negative, making concurrent adjustments race-safe.
        var updated = await _context.Materials
            .Where(m => m.Id == request.Id && m.Quantity + request.Change >= 0)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(m => m.Quantity, m => m.Quantity + request.Change)
                    .SetProperty(m => m.LastUpdated, utcNow)
                    .SetProperty(m => m.UpdatedAt, utcNow),
                cancellationToken);

        if (updated == 0)
        {
            var exists = await _context.Materials
                .AnyAsync(m => m.Id == request.Id, cancellationToken);

            if (!exists)
            {
                throw new NotFoundException(nameof(Material), request.Id);
            }

            throw new ConflictException(
                "The adjustment would make the stock quantity negative.");
        }

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
