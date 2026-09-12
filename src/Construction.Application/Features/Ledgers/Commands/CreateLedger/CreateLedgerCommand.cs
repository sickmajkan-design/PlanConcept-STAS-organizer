using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Ledgers.Models;
using Construction.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Ledgers.Commands.CreateLedger;

/// <summary>
/// Starts a new month. When <see cref="CopyFromLedgerId"/> is set, the source
/// ledger's columns, sections and rows are duplicated into the new one with
/// blank cells — the "new month, same shape" the client's own workflow
/// already does by hand (copying last month's file).
/// </summary>
public record CreateLedgerCommand : IRequest<LedgerDetailDto>
{
    public string Name { get; init; } = null!;

    public int Year { get; init; }

    public int Month { get; init; }

    public string? Note { get; init; }

    public Guid? CopyFromLedgerId { get; init; }
}

public class CreateLedgerCommandValidator : AbstractValidator<CreateLedgerCommand>
{
    public CreateLedgerCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Year).InclusiveBetween(2000, 2100);
        RuleFor(x => x.Month).InclusiveBetween(1, 12);
        RuleFor(x => x.Note).MaximumLength(2000);
    }
}

public class CreateLedgerCommandHandler : IRequestHandler<CreateLedgerCommand, LedgerDetailDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CreateLedgerCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<LedgerDetailDto> Handle(
        CreateLedgerCommand request,
        CancellationToken cancellationToken)
    {
        var ledger = new Ledger
        {
            Name = request.Name.Trim(),
            Year = request.Year,
            Month = request.Month,
            Note = request.Note?.Trim(),
            CreatedByUserId = _currentUserService.UserId,
        };

        _context.Ledgers.Add(ledger);

        if (request.CopyFromLedgerId is { } sourceId)
        {
            var source = await _context.Ledgers
                .Include(l => l.Columns)
                .Include(l => l.Sections).ThenInclude(s => s.Rows)
                .FirstOrDefaultAsync(l => l.Id == sourceId, cancellationToken)
                ?? throw new NotFoundException(nameof(Ledger), sourceId);

            // Old column id -> new column id, so a copied row's cells (if we
            // ever copy values, not just structure) would still know which
            // new column they belong to. Structure only for now: rows get no
            // cells at all, since last month's numbers should not silently
            // reappear as this month's.
            foreach (var column in source.Columns.OrderBy(c => c.SortOrder))
            {
                ledger.Columns.Add(new LedgerColumn
                {
                    Name = column.Name,
                    DataType = column.DataType,
                    // A sourced column's whole point is that it needs no
                    // re-entry month to month — carrying this over is what
                    // makes "copy structure" actually save the SuperAdmin the
                    // work it promises for these columns specifically.
                    SourceMetric = column.SourceMetric,
                    SortOrder = column.SortOrder,
                });
            }

            foreach (var section in source.Sections.OrderBy(s => s.SortOrder))
            {
                var newSection = new LedgerSection
                {
                    Name = section.Name,
                    ProjectId = section.ProjectId,
                    SortOrder = section.SortOrder,
                };

                foreach (var row in section.Rows.OrderBy(r => r.SortOrder))
                {
                    newSection.Rows.Add(new LedgerRow
                    {
                        Label = row.Label,
                        EmployeeId = row.EmployeeId,
                        // Same reasoning as the column's SourceMetric above —
                        // the vehicle/tool/material a row represents doesn't
                        // change month to month, only the cost figure does.
                        VehicleId = row.VehicleId,
                        ToolId = row.ToolId,
                        MaterialId = row.MaterialId,
                        // Deliberately NOT copied: PromotedGeneralExpenseId/
                        // PromotedAccommodationRateId. Last month's row was
                        // pushed through to a real record; this month's copy
                        // is a fresh row that has not been, and should still
                        // offer the promote action.
                        SortOrder = row.SortOrder,
                    });
                }

                ledger.Sections.Add(newSection);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        return await _context.Ledgers
            .AsNoTracking()
            .Where(l => l.Id == ledger.Id)
            .Select(LedgerShellMapping.Projection)
            .FirstAsync(cancellationToken);
    }
}
