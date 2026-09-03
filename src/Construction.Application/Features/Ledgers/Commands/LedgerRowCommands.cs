using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Ledgers.Models;
using Construction.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Ledgers.Commands;

public record AddLedgerRowCommand : IRequest<LedgerRowDto>
{
    public Guid SectionId { get; init; }

    public string Label { get; init; } = null!;

    public Guid? EmployeeId { get; init; }
}

public class AddLedgerRowCommandValidator : AbstractValidator<AddLedgerRowCommand>
{
    public AddLedgerRowCommandValidator()
    {
        RuleFor(x => x.SectionId).NotEmpty();
        RuleFor(x => x.Label).NotEmpty().MaximumLength(256);
    }
}

public class AddLedgerRowCommandHandler : IRequestHandler<AddLedgerRowCommand, LedgerRowDto>
{
    private readonly IApplicationDbContext _context;

    public AddLedgerRowCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<LedgerRowDto> Handle(
        AddLedgerRowCommand request,
        CancellationToken cancellationToken)
    {
        if (!await _context.LedgerSections.AnyAsync(s => s.Id == request.SectionId, cancellationToken))
        {
            throw new NotFoundException(nameof(LedgerSection), request.SectionId);
        }

        string? employeeName = null;
        if (request.EmployeeId is { } employeeId)
        {
            var employee = await _context.Employees
                .Where(e => e.Id == employeeId)
                .Select(e => new { e.FirstName, e.LastName })
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundException(nameof(Employee), employeeId);
            employeeName = $"{employee.FirstName} {employee.LastName}";
        }

        var nextOrder = await _context.LedgerRows
            .Where(r => r.SectionId == request.SectionId)
            .Select(r => (int?)r.SortOrder)
            .MaxAsync(cancellationToken) ?? -1;

        var row = new LedgerRow
        {
            SectionId = request.SectionId,
            Label = request.Label.Trim(),
            EmployeeId = request.EmployeeId,
            SortOrder = nextOrder + 1,
        };

        _context.LedgerRows.Add(row);
        await _context.SaveChangesAsync(cancellationToken);

        return new LedgerRowDto
        {
            Id = row.Id,
            Label = row.Label,
            EmployeeId = row.EmployeeId,
            EmployeeName = employeeName,
            SortOrder = row.SortOrder,
            Cells = Array.Empty<LedgerCellDto>(),
        };
    }
}

public record UpdateLedgerRowCommand : IRequest<LedgerRowDto>
{
    public Guid Id { get; init; }

    public string Label { get; init; } = null!;

    public Guid? EmployeeId { get; init; }
}

public class UpdateLedgerRowCommandValidator : AbstractValidator<UpdateLedgerRowCommand>
{
    public UpdateLedgerRowCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Label).NotEmpty().MaximumLength(256);
    }
}

public class UpdateLedgerRowCommandHandler : IRequestHandler<UpdateLedgerRowCommand, LedgerRowDto>
{
    private readonly IApplicationDbContext _context;

    public UpdateLedgerRowCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<LedgerRowDto> Handle(
        UpdateLedgerRowCommand request,
        CancellationToken cancellationToken)
    {
        var row = await _context.LedgerRows
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(LedgerRow), request.Id);

        string? employeeName = null;
        if (request.EmployeeId is { } employeeId)
        {
            var employee = await _context.Employees
                .Where(e => e.Id == employeeId)
                .Select(e => new { e.FirstName, e.LastName })
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundException(nameof(Employee), employeeId);
            employeeName = $"{employee.FirstName} {employee.LastName}";
        }

        row.Label = request.Label.Trim();
        row.EmployeeId = request.EmployeeId;

        await _context.SaveChangesAsync(cancellationToken);

        var cells = await _context.LedgerCells
            .AsNoTracking()
            .Where(c => c.RowId == row.Id)
            .Select(c => new LedgerCellDto { ColumnId = c.ColumnId, Value = c.Value })
            .ToListAsync(cancellationToken);

        return new LedgerRowDto
        {
            Id = row.Id,
            Label = row.Label,
            EmployeeId = row.EmployeeId,
            EmployeeName = employeeName,
            SortOrder = row.SortOrder,
            Cells = cells,
        };
    }
}

/// <summary>Removes a row and every cell recorded under it.</summary>
public record DeleteLedgerRowCommand(Guid Id) : IRequest;

public class DeleteLedgerRowCommandHandler : IRequestHandler<DeleteLedgerRowCommand>
{
    private readonly IApplicationDbContext _context;

    public DeleteLedgerRowCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DeleteLedgerRowCommand request, CancellationToken cancellationToken)
    {
        var row = await _context.LedgerRows
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(LedgerRow), request.Id);

        _context.LedgerRows.Remove(row);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>Reorders every row of one section in one move.</summary>
public record ReorderLedgerRowsCommand : IRequest
{
    public Guid SectionId { get; init; }

    public IReadOnlyList<Guid> OrderedRowIds { get; init; } = Array.Empty<Guid>();
}

public class ReorderLedgerRowsCommandHandler : IRequestHandler<ReorderLedgerRowsCommand>
{
    private readonly IApplicationDbContext _context;

    public ReorderLedgerRowsCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(ReorderLedgerRowsCommand request, CancellationToken cancellationToken)
    {
        var rows = await _context.LedgerRows
            .Where(r => r.SectionId == request.SectionId)
            .ToDictionaryAsync(r => r.Id, cancellationToken);

        for (var i = 0; i < request.OrderedRowIds.Count; i++)
        {
            if (rows.TryGetValue(request.OrderedRowIds[i], out var row))
            {
                row.SortOrder = i;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
