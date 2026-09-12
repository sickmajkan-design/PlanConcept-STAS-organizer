using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Costs.Models;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Costs.Commands.ReturnToolRentalOut;

/// <summary>Closes an open loan-out — the tool came back. See <c>ReturnVehicleRentalOutCommand</c> for the full rationale.</summary>
public record ReturnToolRentalOutCommand : IRequest<ToolRentalOutDto>
{
    public Guid Id { get; init; }

    /// <summary>Defaults to today.</summary>
    public DateOnly? EndDate { get; init; }
}

public class ReturnToolRentalOutCommandValidator : AbstractValidator<ReturnToolRentalOutCommand>
{
    public ReturnToolRentalOutCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class ReturnToolRentalOutCommandHandler
    : IRequestHandler<ReturnToolRentalOutCommand, ToolRentalOutDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ReturnToolRentalOutCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ToolRentalOutDto> Handle(
        ReturnToolRentalOutCommand request,
        CancellationToken cancellationToken)
    {
        if (!CostRules.CanRecordSpending(_currentUserService.Role))
        {
            throw new ForbiddenAccessException("You may not return tool rentals.");
        }

        var rental = await _context.ToolRentalsOut
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(ToolRentalOut), request.Id);

        await _context.ExecuteInTransactionAsync(
            async token =>
            {
                if (rental.EndDate is not null)
                {
                    throw new ConflictException("This loan has already been returned.");
                }

                var endDate = request.EndDate ?? DateOnly.FromDateTime(_dateTimeProvider.UtcNow);

                if (endDate < rental.StartDate)
                {
                    throw new ConflictException("The loan cannot end before it started.");
                }

                var tool = await _context.Tools
                    .FirstAsync(t => t.Id == rental.ToolId, token);

                rental.EndDate = endDate;
                tool.Status = ToolStatus.Available;

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
