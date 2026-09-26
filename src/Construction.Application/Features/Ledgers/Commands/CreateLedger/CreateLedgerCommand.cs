using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Ledgers.Models;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
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

    /// <summary>
    /// Starts from a ready-made layout (currently "Payroll") instead of an empty
    /// table. Ignored when a previous month is copied, which brings its own.
    /// </summary>
    public string? Template { get; init; }

    /// <summary>
    /// With a template: also creates a section for every live project that has
    /// people on it this month, with a row for each of them.
    /// </summary>
    public bool PopulateFromProjects { get; init; }
}

public class CreateLedgerCommandValidator : AbstractValidator<CreateLedgerCommand>
{
    public CreateLedgerCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Year).InclusiveBetween(2000, 2100);
        RuleFor(x => x.Month).InclusiveBetween(1, 12);
        RuleFor(x => x.Note).MaximumLength(2000);

        RuleFor(x => x.Template)
            .Must(LedgerTemplates.IsKnown).WithMessage("That template does not exist.")
            .When(x => !string.IsNullOrWhiteSpace(x.Template));

        RuleFor(x => x.Template)
            .Empty().WithMessage("Choose either a previous month to copy or a template, not both.")
            .When(x => x.CopyFromLedgerId is not null);
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
                .Include(l => l.Sections).ThenInclude(s => s.Rows).ThenInclude(r => r.Cells)
                .FirstOrDefaultAsync(l => l.Id == sourceId, cancellationToken)
                ?? throw new NotFoundException(nameof(Ledger), sourceId);

            // Old column id -> new column id, so a copied row's cells (if we
            // ever copy values, not just structure) would still know which
            // new column they belong to. Structure only for now: rows get no
            // cells at all, since last month's numbers should not silently
            // reappear as this month's.
            var columnMap = new Dictionary<Guid, Guid>();

            foreach (var column in source.Columns.OrderBy(c => c.SortOrder))
            {
                var copy = new LedgerColumn
                {
                    Id = Guid.CreateVersion7(),
                    Name = column.Name,
                    DataType = column.DataType,
                    // A sourced column's whole point is that it needs no
                    // re-entry month to month — carrying this over is what
                    // makes "copy structure" actually save the SuperAdmin the
                    // work it promises for these columns specifically.
                    SourceMetric = column.SourceMetric,
                    SystemKey = column.SystemKey,
                    FormulaJson = column.FormulaJson,
                    SortOrder = column.SortOrder,
                };

                columnMap[column.Id] = copy.Id;
                ledger.Columns.Add(copy);
            }

            // A formula names columns by id, and the copies have new ids. A column that
            // reads the system's figures (hours in a week, an hourly rate) must read
            // them for the new month, not the old one.
            foreach (var old in source.Columns)
            {
                var column = ledger.Columns.Single(c => c.Id == columnMap[old.Id]);

                // An hour column is named for the calendar week it covers, unless the
                // owner renamed it. Whether or not it is filled from the app: a month
                // with hours typed by hand still has October's weeks, not September's.
                if (old.SystemKey is not null
                    && old.SystemKey.StartsWith("week", StringComparison.Ordinal)
                    && int.TryParse(old.SystemKey.AsSpan(4), out var number)
                    && old.Name == LedgerTemplates.WeekName(number, source.Year, source.Month))
                {
                    column.Name = LedgerTemplates.WeekName(number, request.Year, request.Month);
                }

                var formula = LedgerFormula.Parse(column.FormulaJson)?.Remap(columnMap);

                if (formula is null)
                {
                    continue;
                }

                if (formula.Source is not null && LedgerTemplates.SourceFor(old.SystemKey, request.Year, request.Month) is { } fresh)
                {
                    formula = formula with { Source = fresh };
                }

                column.FormulaJson = formula.ToJson();
            }

            var recurringColumns = source.Columns
                .Where(c => c.SystemKey is not null && LedgerTemplates.Keys.Recurring.Contains(c.SystemKey))
                .Select(c => c.Id)
                .ToHashSet();

            // The boxes are part of the layout — without them a copied month
            // would have its columns but lose its totals. Their typed figures
            // do not carry over, for the same reason the cells do not.
            var sourceBoxes = await _context.LedgerSummaryBoxes
                .AsNoTracking()
                .Where(b => b.LedgerId == sourceId)
                .OrderBy(b => b.SortOrder)
                .ToListAsync(cancellationToken);

            foreach (var box in sourceBoxes)
            {
                ledger.SummaryBoxes.Add(new LedgerSummaryBox
                {
                    Label = box.Label,
                    Sign = box.Sign,
                    Color = box.Color,
                    SortOrder = box.SortOrder,
                    SourceColumnId = box.SourceColumnId is { } from && columnMap.TryGetValue(from, out var to) ? to : null,
                    ManualValue = box.SourceColumnId is null ? 0m : null,
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
                    var newRow = new LedgerRow
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
                    };

                    // Rates and the fixed per-person amounts are the same next month;
                    // everything else starts empty.
                    foreach (var cell in row.Cells.Where(c => recurringColumns.Contains(c.ColumnId) && !string.IsNullOrWhiteSpace(c.Value)))
                    {
                        newRow.Cells.Add(new LedgerCell { ColumnId = columnMap[cell.ColumnId], Value = cell.Value });
                    }

                    newSection.Rows.Add(newRow);
                }

                ledger.Sections.Add(newSection);
            }
        }

        if (request.CopyFromLedgerId is null && LedgerTemplates.IsKnown(request.Template))
        {
            LedgerTemplates.ApplyPayroll(ledger, LedgerTemplates.HoursFromApp(request.Template));

            if (request.PopulateFromProjects)
            {
                await AddSectionsFromProjectsAsync(ledger, cancellationToken);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        return await _context.Ledgers
            .AsNoTracking()
            .Where(l => l.Id == ledger.Id)
            .Select(LedgerShellMapping.Projection)
            .FirstAsync(cancellationToken);
    }

    /// <summary>
    /// A section per live project that has people on it during the month, with
    /// a row for each — the first draft of a payroll, before anything is typed.
    /// </summary>
    private async Task AddSectionsFromProjectsAsync(Ledger ledger, CancellationToken cancellationToken)
    {
        var monthStart = new DateOnly(ledger.Year, ledger.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

        var postings = await _context.EmployeeProjects
            .AsNoTracking()
            .Where(ep => ep.StartDate <= monthEnd && (ep.EndDate == null || ep.EndDate >= monthStart))
            .Where(ep => ep.Project.Status == ProjectStatus.Planned || ep.Project.Status == ProjectStatus.Active)
            .Select(ep => new
            {
                ep.ProjectId,
                ProjectName = ep.Project.Name,
                ep.EmployeeId,
                EmployeeName = ep.Employee.FirstName + " " + ep.Employee.LastName,
            })
            .ToListAsync(cancellationToken);

        var sectionOrder = 0;

        foreach (var project in postings.GroupBy(p => new { p.ProjectId, p.ProjectName }).OrderBy(g => g.Key.ProjectName))
        {
            var section = new LedgerSection
            {
                Name = project.Key.ProjectName,
                ProjectId = project.Key.ProjectId,
                SortOrder = sectionOrder++,
            };

            var rowOrder = 0;

            foreach (var person in project.DistinctBy(p => p.EmployeeId).OrderBy(p => p.EmployeeName))
            {
                section.Rows.Add(new LedgerRow
                {
                    Label = person.EmployeeName,
                    EmployeeId = person.EmployeeId,
                    SortOrder = rowOrder++,
                });
            }

            ledger.Sections.Add(section);
        }
    }
}
