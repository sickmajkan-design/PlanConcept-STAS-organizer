using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Costs.Models;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Costs.Commands.UpdateEmployeeRate;

/// <summary>
/// Corrects a data-entry mistake on an existing rate — the wrong amount or
/// date typed in, not a new period starting.
/// </summary>
/// <remarks>
/// Deliberately narrower than <c>SetEmployeeRateCommand</c>: it changes only
/// this one row's own fields and never touches a neighbouring rate. A raise
/// is still recorded by adding a new rate, which closes off the one before it
/// — that chaining behaviour stays untouched here. This command exists only
/// to fix what was typed wrong, checked against every *other* rate the
/// employee has so a correction cannot silently open a gap or an overlap.
/// </remarks>
public record UpdateEmployeeRateCommand : IRequest<EmployeeRateDto>
{
    public Guid Id { get; init; }

    public RateType RateType { get; init; } = RateType.Hourly;

    /// <summary>Required when <see cref="RateType"/> is Hourly; ignored otherwise.</summary>
    public decimal? HourlyRate { get; init; }

    /// <summary>Cost per hour on a Saturday or Sunday. Null means no premium. Hourly only.</summary>
    public decimal? WeekendHourlyRate { get; init; }

    /// <summary>Cost per hour on a listed public holiday. Null means no premium. Hourly only.</summary>
    public decimal? HolidayHourlyRate { get; init; }

    /// <summary>Cost per hour for a shift tagged Overtime. Null means no premium. Hourly only.</summary>
    public decimal? OvertimeHourlyRate { get; init; }

    /// <summary>Cost per hour for a shift tagged Travel. Null means no premium. Hourly only.</summary>
    public decimal? TravelHourlyRate { get; init; }

    /// <summary>Required when <see cref="RateType"/> is Daily; ignored otherwise.</summary>
    public decimal? DailyRate { get; init; }

    public DateOnly StartDate { get; init; }

    public DateOnly? EndDate { get; init; }

    public string? Note { get; init; }
}

public class UpdateEmployeeRateCommandValidator : AbstractValidator<UpdateEmployeeRateCommand>
{
    public UpdateEmployeeRateCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.RateType).IsInEnum();

        RuleFor(x => x.HourlyRate)
            .NotNull().WithMessage("An hourly rate is required.")
            .GreaterThan(0).WithMessage("An hour has to cost something.")
            .LessThanOrEqualTo(CostRules.MaxHourlyRate)
            .WithMessage("That rate looks like a typo rather than a wage.")
            .When(x => x.RateType == RateType.Hourly);

        RuleFor(x => x.WeekendHourlyRate)
            .GreaterThan(0).LessThanOrEqualTo(CostRules.MaxHourlyRate)
            .WithMessage("That rate looks like a typo rather than a wage.")
            .When(x => x.WeekendHourlyRate is not null);

        RuleFor(x => x.HolidayHourlyRate)
            .GreaterThan(0).LessThanOrEqualTo(CostRules.MaxHourlyRate)
            .WithMessage("That rate looks like a typo rather than a wage.")
            .When(x => x.HolidayHourlyRate is not null);

        RuleFor(x => x.OvertimeHourlyRate)
            .GreaterThan(0).LessThanOrEqualTo(CostRules.MaxHourlyRate)
            .WithMessage("That rate looks like a typo rather than a wage.")
            .When(x => x.OvertimeHourlyRate is not null);

        RuleFor(x => x.TravelHourlyRate)
            .GreaterThan(0).LessThanOrEqualTo(CostRules.MaxHourlyRate)
            .WithMessage("That rate looks like a typo rather than a wage.")
            .When(x => x.TravelHourlyRate is not null);

        RuleFor(x => x.DailyRate)
            .NotNull().WithMessage("A daily rate is required.")
            .GreaterThan(0).LessThanOrEqualTo(CostRules.MaxHourlyRate)
            .WithMessage("That rate looks like a typo rather than a day's pay.")
            .When(x => x.RateType == RateType.Daily);

        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate)
            .WithMessage("The rate cannot end before it starts.")
            .When(x => x.EndDate is not null);

        RuleFor(x => x.Note).MaximumLength(500);
    }
}

public class UpdateEmployeeRateCommandHandler
    : IRequestHandler<UpdateEmployeeRateCommand, EmployeeRateDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public UpdateEmployeeRateCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<EmployeeRateDto> Handle(
        UpdateEmployeeRateCommand request,
        CancellationToken cancellationToken)
    {
        if (!CostRules.CanSetLabourRate(_currentUserService.Role))
        {
            throw new ForbiddenAccessException("You may not correct pay rates.");
        }

        var rate = await _context.EmployeeRates
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(EmployeeRate), request.Id);

        // Every other rate this employee has — the chain this row must still
        // fit into without overlapping, but without re-triggering the
        // close-off-the-predecessor behaviour SetEmployeeRateCommand does.
        var clashes = await _context.EmployeeRates
            .AnyAsync(
                r => r.EmployeeId == rate.EmployeeId
                    && r.Id != rate.Id
                    && r.StartDate <= (request.EndDate ?? DateOnly.MaxValue)
                    && (r.EndDate == null || r.EndDate >= request.StartDate),
                cancellationToken);

        if (clashes)
        {
            throw new ConflictException("Another rate already covers those dates.");
        }

        var isHourly = request.RateType == RateType.Hourly;

        rate.RateType = request.RateType;
        rate.HourlyRate = isHourly ? request.HourlyRate : null;
        rate.WeekendHourlyRate = isHourly ? request.WeekendHourlyRate : null;
        rate.HolidayHourlyRate = isHourly ? request.HolidayHourlyRate : null;
        rate.OvertimeHourlyRate = isHourly ? request.OvertimeHourlyRate : null;
        rate.TravelHourlyRate = isHourly ? request.TravelHourlyRate : null;
        rate.DailyRate = isHourly ? null : request.DailyRate;
        rate.StartDate = request.StartDate;
        rate.EndDate = request.EndDate;
        rate.Note = request.Note?.Trim();

        await _context.SaveChangesAsync(cancellationToken);

        return await _context.EmployeeRates
            .AsNoTracking()
            .Where(r => r.Id == rate.Id)
            .Select(EmployeeRateMapping.Projection)
            .FirstAsync(cancellationToken);
    }
}
