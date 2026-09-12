using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Costs.Models;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Costs.Commands.RecordToolRentalOut;

/// <summary>Records the tool going out to another company. See <c>RecordVehicleRentalOutCommand</c> for the full rationale.</summary>
public record RecordToolRentalOutCommand : IRequest<ToolRentalOutDto>
{
    public Guid ToolId { get; init; }

    /// <summary>Optional cross-reference to a tracked customer.</summary>
    public Guid? CustomerId { get; init; }

    public string RenterName { get; init; } = null!;

    public decimal DailyRate { get; init; }

    /// <summary>Defaults to today.</summary>
    public DateOnly? StartDate { get; init; }

    public string? Note { get; init; }
}

public class RecordToolRentalOutCommandValidator : AbstractValidator<RecordToolRentalOutCommand>
{
    // Same reasoning as RecordVehicleRentalOutCommandValidator: a loan-out
    // may reasonably be booked a little ahead of the day the tool leaves.
    private const int MaxFutureDays = 30;

    public RecordToolRentalOutCommandValidator(IDateTimeProvider dateTimeProvider)
    {
        var today = DateOnly.FromDateTime(dateTimeProvider.UtcNow);

        RuleFor(x => x.ToolId).NotEmpty();

        RuleFor(x => x.RenterName)
            .NotEmpty().WithMessage("Say who has the tool.")
            .MaximumLength(200);

        RuleFor(x => x.DailyRate)
            .GreaterThan(0).WithMessage("A day out has to cost something.")
            .LessThanOrEqualTo(CostRules.MaxHourlyRate)
            .WithMessage("That amount looks like a typo rather than a daily rate.");

        RuleFor(x => x.StartDate)
            .GreaterThanOrEqualTo(today.AddDays(-CostRules.MaxBackdatingDays))
            .WithMessage(
                $"A loan cannot be recorded more than {CostRules.MaxBackdatingDays} days back.")
            .LessThanOrEqualTo(today.AddDays(MaxFutureDays))
            .WithMessage($"A loan cannot start more than {MaxFutureDays} days from now.")
            .When(x => x.StartDate is not null);

        RuleFor(x => x.Note).MaximumLength(500);
    }
}

public class RecordToolRentalOutCommandHandler
    : IRequestHandler<RecordToolRentalOutCommand, ToolRentalOutDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RecordToolRentalOutCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ToolRentalOutDto> Handle(
        RecordToolRentalOutCommand request,
        CancellationToken cancellationToken)
    {
        if (!CostRules.CanRecordSpending(_currentUserService.Role))
        {
            throw new ForbiddenAccessException("You may not record tool rentals.");
        }

        var tool = await _context.Tools
            .FirstOrDefaultAsync(t => t.Id == request.ToolId, cancellationToken)
            ?? throw new NotFoundException(nameof(Tool), request.ToolId);

        if (request.CustomerId is { } customerId
            && !await _context.Customers.AnyAsync(c => c.Id == customerId, cancellationToken))
        {
            throw new NotFoundException(nameof(Customer), customerId);
        }

        var rental = new ToolRentalOut
        {
            ToolId = request.ToolId,
            CustomerId = request.CustomerId,
            RenterName = request.RenterName.Trim(),
            DailyRate = request.DailyRate,
            StartDate = request.StartDate ?? DateOnly.FromDateTime(_dateTimeProvider.UtcNow),
            Note = request.Note?.Trim(),
            SetByUserId = _currentUserService.UserId
        };

        await _context.ExecuteInTransactionAsync(
            async token =>
            {
                if (tool.Status != ToolStatus.Available)
                {
                    throw new ConflictException("The tool is not available to rent out.");
                }

                tool.Status = ToolStatus.RentedOut;

                _context.ToolRentalsOut.Add(rental);
                await _context.SaveChangesAsync(token);
            },
            cancellationToken);

        return await _context.ToolRentalsOut
            .AsNoTracking()
            .Where(r => r.Id == rental.Id)
            .Select(ToolRentalOutMapping.Projection)
            .FirstAsync(cancellationToken);
    }
}
