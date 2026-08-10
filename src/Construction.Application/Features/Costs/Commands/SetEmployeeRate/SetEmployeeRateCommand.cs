using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Costs.Models;
using Construction.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Costs.Commands.SetEmployeeRate;

/// <summary>Puts a new hourly rate in force from a given date.</summary>
/// <remarks>
/// A raise, expressed the way it happens: from Monday, this person costs
/// more. The open-ended rate that was in force is closed off the day before,
/// rather than edited — editing it would rewrite what last month cost.
/// </remarks>
public record SetEmployeeRateCommand : IRequest<EmployeeRateDto>
{
    public Guid EmployeeId { get; init; }

    public decimal HourlyRate { get; init; }

    /// <summary>Defaults to today.</summary>
    public DateOnly? StartDate { get; init; }

    /// <summary>Null leaves it open-ended, which is the usual case.</summary>
    public DateOnly? EndDate { get; init; }

    public string? Note { get; init; }
}

public class SetEmployeeRateCommandValidator : AbstractValidator<SetEmployeeRateCommand>
{
    public SetEmployeeRateCommandValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();

        RuleFor(x => x.HourlyRate)
            .GreaterThan(0).WithMessage("An hour has to cost something.")
            .LessThanOrEqualTo(CostRules.MaxHourlyRate)
            .WithMessage("That rate looks like a typo rather than a wage.");

        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate!.Value)
            .WithMessage("The rate cannot end before it starts.")
            .When(x => x.StartDate is not null && x.EndDate is not null);

        RuleFor(x => x.Note).MaximumLength(500);
    }
}

public class SetEmployeeRateCommandHandler
    : IRequestHandler<SetEmployeeRateCommand, EmployeeRateDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public SetEmployeeRateCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<EmployeeRateDto> Handle(
        SetEmployeeRateCommand request,
        CancellationToken cancellationToken)
    {
        if (!CostRules.CanSetLabourRate(_currentUserService.Role))
        {
            throw new ForbiddenAccessException("You may not set pay rates.");
        }

        if (!await _context.Employees.AnyAsync(e => e.Id == request.EmployeeId, cancellationToken))
        {
            throw new NotFoundException(nameof(Employee), request.EmployeeId);
        }

        var startDate = request.StartDate ?? DateOnly.FromDateTime(_dateTimeProvider.UtcNow);
        var rate = new EmployeeRate
        {
            EmployeeId = request.EmployeeId,
            HourlyRate = request.HourlyRate,
            StartDate = startDate,
            EndDate = request.EndDate,
            Note = request.Note?.Trim(),
            SetByUserId = _currentUserService.UserId
        };

        await _context.ExecuteInTransactionAsync(
            async token =>
            {
                // Close off the rate that ran up to this one, so a raise is
                // one action rather than two the office has to remember. Only
                // an open-ended earlier rate: a closed one already says what
                // period it covered, and moving its end would rewrite history.
                var predecessor = await _context.EmployeeRates
                    .Where(r => r.EmployeeId == request.EmployeeId
                        && r.EndDate == null
                        && r.StartDate < startDate)
                    .OrderByDescending(r => r.StartDate)
                    .FirstOrDefaultAsync(token);

                if (predecessor is not null)
                {
                    predecessor.EndDate = startDate.AddDays(-1);
                }

                _context.EmployeeRates.Add(rate);

                // Anything still overlapping after that is a genuine clash —
                // a backdated rate landing inside a period that is already
                // priced. The database refuses it; this turns the constraint
                // violation into a sentence.
                var clashes = await _context.EmployeeRates
                    .AnyAsync(
                        r => r.EmployeeId == request.EmployeeId
                            && (predecessor == null || r.Id != predecessor.Id)
                            && r.StartDate <= (request.EndDate ?? DateOnly.MaxValue)
                            && (r.EndDate == null || r.EndDate >= startDate),
                        token);

                if (clashes)
                {
                    throw new ConflictException(
                        "Another rate already covers those dates.");
                }

                await _context.SaveChangesAsync(token);
            },
            cancellationToken);

        return await _context.EmployeeRates
            .AsNoTracking()
            .Where(r => r.Id == rate.Id)
            .Select(EmployeeRateMapping.Projection)
            .FirstAsync(cancellationToken);
    }
}
