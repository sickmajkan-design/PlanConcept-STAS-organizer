using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Costs.Models;
using Construction.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Costs.Commands.UpdateToolRentalOut;

/// <summary>Corrects a data-entry mistake on an existing loan-out. See <c>UpdateVehicleRentalOutCommand</c> for the full rationale.</summary>
public record UpdateToolRentalOutCommand : IRequest<ToolRentalOutDto>
{
    public Guid Id { get; init; }

    public Guid? CustomerId { get; init; }

    public string RenterName { get; init; } = null!;

    public decimal DailyRate { get; init; }

    public DateOnly StartDate { get; init; }

    public string? Note { get; init; }
}

public class UpdateToolRentalOutCommandValidator : AbstractValidator<UpdateToolRentalOutCommand>
{
    public UpdateToolRentalOutCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.RenterName)
            .NotEmpty().WithMessage("Say who has the tool.")
            .MaximumLength(200);

        RuleFor(x => x.DailyRate)
            .GreaterThan(0).WithMessage("A day out has to cost something.")
            .LessThanOrEqualTo(CostRules.MaxHourlyRate)
            .WithMessage("That amount looks like a typo rather than a daily rate.");

        RuleFor(x => x.Note).MaximumLength(500);
    }
}

public class UpdateToolRentalOutCommandHandler
    : IRequestHandler<UpdateToolRentalOutCommand, ToolRentalOutDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public UpdateToolRentalOutCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<ToolRentalOutDto> Handle(
        UpdateToolRentalOutCommand request,
        CancellationToken cancellationToken)
    {
        if (!CostRules.CanRecordSpending(_currentUserService.Role))
        {
            throw new ForbiddenAccessException("You may not correct tool rentals.");
        }

        var rental = await _context.ToolRentalsOut
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(ToolRentalOut), request.Id);

        if (rental.EndDate is not null && request.StartDate > rental.EndDate)
        {
            throw new ConflictException("The loan cannot start after it ended.");
        }

        if (request.CustomerId is { } customerId
            && !await _context.Customers.AnyAsync(c => c.Id == customerId, cancellationToken))
        {
            throw new NotFoundException(nameof(Customer), customerId);
        }

        rental.CustomerId = request.CustomerId;
        rental.RenterName = request.RenterName.Trim();
        rental.DailyRate = request.DailyRate;
        rental.StartDate = request.StartDate;
        rental.Note = request.Note?.Trim();

        await _context.SaveChangesAsync(cancellationToken);

        return await _context.ToolRentalsOut
            .AsNoTracking()
            .Where(r => r.Id == rental.Id)
            .Select(ToolRentalOutMapping.Projection)
            .FirstAsync(cancellationToken);
    }
}
