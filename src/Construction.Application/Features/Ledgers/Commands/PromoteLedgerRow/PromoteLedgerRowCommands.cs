using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Costs.Commands.RecordGeneralExpense;
using Construction.Application.Features.Costs.Commands.SetAccommodationRate;
using Construction.Application.Features.Ledgers.Models;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Ledgers.Commands.PromoteLedgerRow;

/// <summary>
/// Pushes a manually-typed ledger row through the real General Expense
/// form — an explicit, one-shot action the SuperAdmin takes, not a silent
/// sync. Goes through <see cref="RecordGeneralExpenseCommand"/>'s own
/// validation exactly as the Costs screen would, so a promoted row is
/// indistinguishable from one entered there directly.
/// </summary>
public record PromoteLedgerRowToGeneralExpenseCommand : IRequest<LedgerRowDto>
{
    public Guid RowId { get; init; }

    public GeneralExpenseCategory Category { get; init; }

    public decimal Amount { get; init; }

    public DateOnly? OccurredOn { get; init; }

    public Guid? ProjectId { get; init; }

    public Guid? EmployeeId { get; init; }

    public string? Supplier { get; init; }

    public string? Note { get; init; }
}

public class PromoteLedgerRowToGeneralExpenseCommandValidator
    : AbstractValidator<PromoteLedgerRowToGeneralExpenseCommand>
{
    public PromoteLedgerRowToGeneralExpenseCommandValidator()
    {
        RuleFor(x => x.RowId).NotEmpty();
        RuleFor(x => x.Category).IsInEnum();
        RuleFor(x => x.Amount).GreaterThanOrEqualTo(0);
    }
}

public class PromoteLedgerRowToGeneralExpenseCommandHandler
    : IRequestHandler<PromoteLedgerRowToGeneralExpenseCommand, LedgerRowDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IMediator _mediator;

    public PromoteLedgerRowToGeneralExpenseCommandHandler(
        IApplicationDbContext context, IMediator mediator)
    {
        _context = context;
        _mediator = mediator;
    }

    public async Task<LedgerRowDto> Handle(
        PromoteLedgerRowToGeneralExpenseCommand request,
        CancellationToken cancellationToken)
    {
        var row = await LedgerRowPromotion.LoadUnpromotedRowAsync(
            _context, request.RowId, cancellationToken);

        var expense = await _mediator.Send(
            new RecordGeneralExpenseCommand
            {
                Category = request.Category,
                Amount = request.Amount,
                OccurredOn = request.OccurredOn,
                ProjectId = request.ProjectId,
                EmployeeId = request.EmployeeId,
                Supplier = request.Supplier,
                Note = request.Note,
            },
            cancellationToken);

        row.PromotedGeneralExpenseId = expense.Id;
        await _context.SaveChangesAsync(cancellationToken);

        return await LedgerRowPromotion.ToDtoAsync(_context, row.Id, cancellationToken);
    }
}

/// <summary>
/// Pushes a manually-typed ledger row through the real Accommodation-rate
/// form — see <see cref="PromoteLedgerRowToGeneralExpenseCommand"/> for why
/// this is explicit rather than automatic. Unlike a general expense, this
/// puts a *recurring* monthly rate in force against an accommodation that
/// must already exist — creating one is out of scope for this action.
/// </summary>
public record PromoteLedgerRowToAccommodationRateCommand : IRequest<LedgerRowDto>
{
    public Guid RowId { get; init; }

    public Guid AccommodationId { get; init; }

    public decimal MonthlyAmount { get; init; }

    public string? Provider { get; init; }

    public DateOnly? StartDate { get; init; }

    public DateOnly? EndDate { get; init; }
}

public class PromoteLedgerRowToAccommodationRateCommandValidator
    : AbstractValidator<PromoteLedgerRowToAccommodationRateCommand>
{
    public PromoteLedgerRowToAccommodationRateCommandValidator()
    {
        RuleFor(x => x.RowId).NotEmpty();
        RuleFor(x => x.AccommodationId).NotEmpty();
        RuleFor(x => x.MonthlyAmount).GreaterThanOrEqualTo(0);
    }
}

public class PromoteLedgerRowToAccommodationRateCommandHandler
    : IRequestHandler<PromoteLedgerRowToAccommodationRateCommand, LedgerRowDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IMediator _mediator;

    public PromoteLedgerRowToAccommodationRateCommandHandler(
        IApplicationDbContext context, IMediator mediator)
    {
        _context = context;
        _mediator = mediator;
    }

    public async Task<LedgerRowDto> Handle(
        PromoteLedgerRowToAccommodationRateCommand request,
        CancellationToken cancellationToken)
    {
        var row = await LedgerRowPromotion.LoadUnpromotedRowAsync(
            _context, request.RowId, cancellationToken);

        var rate = await _mediator.Send(
            new SetAccommodationRateCommand
            {
                AccommodationId = request.AccommodationId,
                MonthlyAmount = request.MonthlyAmount,
                Provider = request.Provider,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
            },
            cancellationToken);

        row.PromotedAccommodationRateId = rate.Id;
        await _context.SaveChangesAsync(cancellationToken);

        return await LedgerRowPromotion.ToDtoAsync(_context, row.Id, cancellationToken);
    }
}

file static class LedgerRowPromotion
{
    public static async Task<LedgerRow> LoadUnpromotedRowAsync(
        IApplicationDbContext context, Guid rowId, CancellationToken cancellationToken)
    {
        var row = await context.LedgerRows
            .FirstOrDefaultAsync(r => r.Id == rowId, cancellationToken)
            ?? throw new NotFoundException(nameof(LedgerRow), rowId);

        if (row.PromotedGeneralExpenseId is not null || row.PromotedAccommodationRateId is not null)
        {
            throw new ConflictException("This row has already been promoted to a real record.");
        }

        return row;
    }

    public static async Task<LedgerRowDto> ToDtoAsync(
        IApplicationDbContext context, Guid rowId, CancellationToken cancellationToken)
    {
        var row = await context.LedgerRows
            .AsNoTracking()
            .Where(r => r.Id == rowId)
            .Select(r => new LedgerRowDto
            {
                Id = r.Id,
                Label = r.Label,
                EmployeeId = r.EmployeeId,
                EmployeeName = r.Employee != null ? r.Employee.FirstName + " " + r.Employee.LastName : null,
                VehicleId = r.VehicleId,
                VehicleName = r.Vehicle != null
                    ? r.Vehicle.Brand + " " + r.Vehicle.Model + " (" + r.Vehicle.RegistrationNumber + ")"
                    : null,
                ToolId = r.ToolId,
                ToolName = r.Tool != null ? r.Tool.Name : null,
                MaterialId = r.MaterialId,
                MaterialName = r.Material != null ? r.Material.Name : null,
                PromotedGeneralExpenseId = r.PromotedGeneralExpenseId,
                PromotedAccommodationRateId = r.PromotedAccommodationRateId,
                ColorTag = r.ColorTag,
                SortOrder = r.SortOrder,
                Cells = r.Cells
                    .Select(c => new LedgerCellDto { Id = c.Id, ColumnId = c.ColumnId, Value = c.Value, ColorTag = c.ColorTag })
                    .ToList(),
            })
            .FirstAsync(cancellationToken);

        return row;
    }
}
