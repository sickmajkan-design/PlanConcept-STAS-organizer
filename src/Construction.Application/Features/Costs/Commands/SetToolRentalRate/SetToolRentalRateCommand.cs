using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Costs.Models;
using Construction.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Costs.Commands.SetToolRentalRate;

/// <summary>Puts a new rental/lease rate in force from a given date.</summary>
/// <remarks>
/// A renewed contract, expressed the way it happens: from the renewal date,
/// this tool costs a different amount per month. The open-ended rate that
/// was in force is closed off the day before, rather than edited — editing it
/// would rewrite what last month's tool report said the tool cost.
/// </remarks>
public record SetToolRentalRateCommand : IRequest<ToolRentalRateDto>
{
    public Guid ToolId { get; init; }

    public decimal MonthlyAmount { get; init; }

    public string? Provider { get; init; }

    /// <summary>Defaults to today.</summary>
    public DateOnly? StartDate { get; init; }

    /// <summary>Null leaves it open-ended, which is the usual case.</summary>
    public DateOnly? EndDate { get; init; }

    public string? Note { get; init; }
}

public class SetToolRentalRateCommandValidator : AbstractValidator<SetToolRentalRateCommand>
{
    public SetToolRentalRateCommandValidator()
    {
        RuleFor(x => x.ToolId).NotEmpty();

        RuleFor(x => x.MonthlyAmount)
            .GreaterThan(0).WithMessage("A month has to cost something.")
            .LessThanOrEqualTo(CostRules.MaxHourlyRate)
            .WithMessage("That amount looks like a typo rather than a monthly rate.");

        RuleFor(x => x.Provider).MaximumLength(200);

        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate!.Value)
            .WithMessage("The rate cannot end before it starts.")
            .When(x => x.StartDate is not null && x.EndDate is not null);

        RuleFor(x => x.Note).MaximumLength(500);
    }
}

public class SetToolRentalRateCommandHandler
    : IRequestHandler<SetToolRentalRateCommand, ToolRentalRateDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public SetToolRentalRateCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ToolRentalRateDto> Handle(
        SetToolRentalRateCommand request,
        CancellationToken cancellationToken)
    {
        if (!CostRules.CanRecordSpending(_currentUserService.Role))
        {
            throw new ForbiddenAccessException("You may not set rental rates.");
        }

        if (!await _context.Tools.AnyAsync(t => t.Id == request.ToolId, cancellationToken))
        {
            throw new NotFoundException(nameof(Tool), request.ToolId);
        }

        var startDate = request.StartDate ?? DateOnly.FromDateTime(_dateTimeProvider.UtcNow);
        var rate = new ToolRentalRate
        {
            ToolId = request.ToolId,
            MonthlyAmount = request.MonthlyAmount,
            Provider = request.Provider?.Trim(),
            StartDate = startDate,
            EndDate = request.EndDate,
            Note = request.Note?.Trim(),
            SetByUserId = _currentUserService.UserId
        };

        await _context.ExecuteInTransactionAsync(
            async token =>
            {
                // Close off the rate that ran up to this one, so a renewed
                // contract is one action rather than two the office has to
                // remember. Only an open-ended earlier rate: a closed one
                // already says what period it covered.
                var predecessor = await _context.ToolRentalRates
                    .Where(r => r.ToolId == request.ToolId
                        && r.EndDate == null
                        && r.StartDate < startDate)
                    .OrderByDescending(r => r.StartDate)
                    .FirstOrDefaultAsync(token);

                if (predecessor is not null)
                {
                    predecessor.EndDate = startDate.AddDays(-1);
                }

                _context.ToolRentalRates.Add(rate);

                var clashes = await _context.ToolRentalRates
                    .AnyAsync(
                        r => r.ToolId == request.ToolId
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

        return await _context.ToolRentalRates
            .AsNoTracking()
            .Where(r => r.Id == rate.Id)
            .Select(ToolRentalRateMapping.Projection)
            .FirstAsync(cancellationToken);
    }
}
