using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Costs.Models;
using Construction.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Costs.Commands.UpdateToolRentalRate;

/// <summary>
/// Corrects a data-entry mistake on an existing rental rate — the wrong
/// amount or date typed in, not a renewal starting.
/// </summary>
/// <remarks>
/// Deliberately narrower than <c>SetToolRentalRateCommand</c>: it changes
/// only this one row's own fields and never touches a neighbouring rate. A
/// renewal is still recorded by adding a new rate, which closes off the one
/// before it — that chaining behaviour stays untouched here.
/// </remarks>
public record UpdateToolRentalRateCommand : IRequest<ToolRentalRateDto>
{
    public Guid Id { get; init; }

    public decimal MonthlyAmount { get; init; }

    public string? Provider { get; init; }

    public DateOnly StartDate { get; init; }

    public DateOnly? EndDate { get; init; }

    public string? Note { get; init; }
}

public class UpdateToolRentalRateCommandValidator : AbstractValidator<UpdateToolRentalRateCommand>
{
    public UpdateToolRentalRateCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.MonthlyAmount)
            .GreaterThan(0).WithMessage("A month has to cost something.")
            .LessThanOrEqualTo(CostRules.MaxHourlyRate)
            .WithMessage("That amount looks like a typo rather than a monthly rate.");

        RuleFor(x => x.Provider).MaximumLength(200);

        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate)
            .WithMessage("The rate cannot end before it starts.")
            .When(x => x.EndDate is not null);

        RuleFor(x => x.Note).MaximumLength(500);
    }
}

public class UpdateToolRentalRateCommandHandler
    : IRequestHandler<UpdateToolRentalRateCommand, ToolRentalRateDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public UpdateToolRentalRateCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<ToolRentalRateDto> Handle(
        UpdateToolRentalRateCommand request,
        CancellationToken cancellationToken)
    {
        if (!CostRules.CanRecordSpending(_currentUserService.Role))
        {
            throw new ForbiddenAccessException("You may not correct rental rates.");
        }

        var rate = await _context.ToolRentalRates
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(ToolRentalRate), request.Id);

        var clashes = await _context.ToolRentalRates
            .AnyAsync(
                r => r.ToolId == rate.ToolId
                    && r.Id != rate.Id
                    && r.StartDate <= (request.EndDate ?? DateOnly.MaxValue)
                    && (r.EndDate == null || r.EndDate >= request.StartDate),
                cancellationToken);

        if (clashes)
        {
            throw new ConflictException("Another rate already covers those dates.");
        }

        rate.MonthlyAmount = request.MonthlyAmount;
        rate.Provider = request.Provider?.Trim();
        rate.StartDate = request.StartDate;
        rate.EndDate = request.EndDate;
        rate.Note = request.Note?.Trim();

        await _context.SaveChangesAsync(cancellationToken);

        return await _context.ToolRentalRates
            .AsNoTracking()
            .Where(r => r.Id == rate.Id)
            .Select(ToolRentalRateMapping.Projection)
            .FirstAsync(cancellationToken);
    }
}
