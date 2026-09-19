using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Costs.Models;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Costs.Commands.UpdateAccommodationRate;

/// <summary>
/// Corrects a data-entry mistake on an existing accommodation rate — the
/// wrong amount or date typed in, not a renewal starting.
/// </summary>
/// <remarks>
/// Deliberately narrower than <c>SetAccommodationRateCommand</c>: it changes
/// only this one row's own fields and never touches a neighbouring rate. A
/// renewal is still recorded by adding a new rate, which closes off the one
/// before it — that chaining behaviour stays untouched here.
/// </remarks>
public record UpdateAccommodationRateCommand : IRequest<AccommodationRateDto>
{
    public Guid Id { get; init; }

    public decimal Amount { get; init; }

    public string? Provider { get; init; }

    public DateOnly StartDate { get; init; }

    public DateOnly? EndDate { get; init; }

    public string? Note { get; init; }
}

public class UpdateAccommodationRateCommandValidator : AbstractValidator<UpdateAccommodationRateCommand>
{
    public UpdateAccommodationRateCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("A charge has to cost something.")
            .LessThanOrEqualTo(CostRules.MaxHourlyRate)
            .WithMessage("That amount looks like a typo.");

        RuleFor(x => x.Provider).MaximumLength(200);

        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate)
            .WithMessage("The rate cannot end before it starts.")
            .When(x => x.EndDate is not null);

        RuleFor(x => x.Note).MaximumLength(500);
    }
}

public class UpdateAccommodationRateCommandHandler
    : IRequestHandler<UpdateAccommodationRateCommand, AccommodationRateDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public UpdateAccommodationRateCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<AccommodationRateDto> Handle(
        UpdateAccommodationRateCommand request,
        CancellationToken cancellationToken)
    {
        if (!CostRules.CanRecordSpending(_currentUserService.Role))
        {
            throw new ForbiddenAccessException("You may not correct accommodation rates.");
        }

        var rate = await _context.AccommodationRates
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(AccommodationRate), request.Id);

        var isOneOff = rate.Kind == AccommodationChargeKind.OneOff;
        var endDate = isOneOff ? request.StartDate : request.EndDate;

        var clashes = !isOneOff && await _context.AccommodationRates
            .AnyAsync(
                r => r.AccommodationId == rate.AccommodationId
                    && r.Kind == rate.Kind
                    && r.Id != rate.Id
                    && r.StartDate <= (request.EndDate ?? DateOnly.MaxValue)
                    && (r.EndDate == null || r.EndDate >= request.StartDate),
                cancellationToken);

        if (clashes)
        {
            throw new ConflictException("Another rate already covers those dates.");
        }

        rate.Amount = request.Amount;
        rate.Provider = request.Provider?.Trim();
        rate.StartDate = request.StartDate;
        rate.EndDate = endDate;
        rate.Note = request.Note?.Trim();

        await _context.SaveChangesAsync(cancellationToken);

        return await _context.AccommodationRates
            .AsNoTracking()
            .Where(r => r.Id == rate.Id)
            .Select(AccommodationRateMapping.Projection)
            .FirstAsync(cancellationToken);
    }
}
