using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Common.Spreadsheets;
using Construction.Application.Features.Costs;
using Construction.Application.Features.Costs.Queries.GetProjectCosts;
using Construction.Application.Features.Costs.Queries.GetVehicleCosts;
using Construction.Application.Features.TimeEntries;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Exports.Queries;

/// <summary>A rendered file, ready to be returned as a download.</summary>
public sealed record ExportFile(string FileName, string ContentType, byte[] Content);

/// <summary>
/// Shared shape for the exports: a period, an optional narrowing, and the
/// language the headings should be in.
/// </summary>
/// <remarks>
/// The language is a parameter rather than an <c>Accept-Language</c> header
/// because the file outlives the request. Somebody emails the spreadsheet to
/// the accountant, and the accountant's browser never had an opinion about it.
/// Making it explicit also means the client can offer the choice, which it
/// should: the person exporting is not always the person reading.
/// </remarks>
public abstract record ExportQueryBase
{
    public DateOnly From { get; init; }

    public DateOnly To { get; init; }

    /// <summary>`sr` or `en`. Serbian when unset.</summary>
    public string? Language { get; init; }
}

public class ExportQueryValidator<T> : AbstractValidator<T> where T : ExportQueryBase
{
    /// <summary>
    /// Widest period an export will cover.
    /// </summary>
    /// <remarks>
    /// Two years, matching the cost reports. An export is heavier than a
    /// report — it materialises every row rather than a total per site — so
    /// this is the bound that stops one request from reading the whole table.
    /// </remarks>
    public const int MaxDays = 732;

    public ExportQueryValidator()
    {
        RuleFor(x => x.From)
            .NotEqual(default(DateOnly)).WithMessage("A start date is required.");

        RuleFor(x => x.To)
            .NotEqual(default(DateOnly)).WithMessage("An end date is required.")
            .GreaterThanOrEqualTo(x => x.From)
            .WithMessage("The end of the period must not be before its start.")
            .Must((query, to) => to.DayNumber - query.From.DayNumber + 1 <= MaxDays)
            .WithMessage($"The period must not exceed {MaxDays} days.")
            .When(x => x.From != default);
    }
}

// ---- timesheets ------------------------------------------------------------

/// <summary>
/// The hours, row by row. The export payroll is actually run from.
/// </summary>
public sealed record ExportTimeEntriesQuery : ExportQueryBase, IRequest<ExportFile>
{
    public Guid? EmployeeId { get; init; }

    public Guid? ProjectId { get; init; }

    /// <summary>
    /// Only hours somebody has signed off. On by default, because an export
    /// that mixes approved and unreviewed hours is not a payroll document.
    /// </summary>
    public bool ApprovedOnly { get; init; } = true;
}

public class ExportTimeEntriesQueryValidator
    : ExportQueryValidator<ExportTimeEntriesQuery>;

public class ExportTimeEntriesQueryHandler
    : IRequestHandler<ExportTimeEntriesQuery, ExportFile>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ISpreadsheetWriter _writer;

    public ExportTimeEntriesQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ISpreadsheetWriter writer)
    {
        _context = context;
        _currentUserService = currentUserService;
        _writer = writer;
    }

    public async Task<ExportFile> Handle(
        ExportTimeEntriesQuery request,
        CancellationToken cancellationToken)
    {
        // Refused rather than narrowed to the caller's own hours: a worker
        // wanting their own timesheet has it on the phone, and a spreadsheet
        // of one person's shifts is not what this endpoint is for.
        if (TimeEntryAccess.IsRestrictedToOwnEntries(_currentUserService.Role))
        {
            throw new ForbiddenAccessException("You may not export timesheets.");
        }

        var english = ExportLabels.IsEnglish(request.Language);

        var query = _context.TimeEntries
            .AsNoTracking()
            .Where(t => t.EndedAt != null)
            .Where(t => DateOnly.FromDateTime(t.StartedAt) >= request.From
                && DateOnly.FromDateTime(t.StartedAt) <= request.To);

        if (request.ApprovedOnly)
        {
            query = query.Where(t => t.Status == TimeEntryStatus.Approved);
        }

        if (request.EmployeeId is { } employeeId)
        {
            query = query.Where(t => t.EmployeeId == employeeId);
        }

        if (request.ProjectId is { } projectId)
        {
            query = query.Where(t => t.ProjectId == projectId);
        }

        var rows = await query
            .OrderBy(t => t.StartedAt)
            .Select(t => new
            {
                Employee = t.Employee.FirstName + " " + t.Employee.LastName,
                Project = t.Project != null ? t.Project.Name : null,
                t.StartedAt,
                EndedAt = t.EndedAt!.Value,
                t.BreakMinutes,
                t.WorkType,
                t.Status,
                t.Note
            })
            .ToListAsync(cancellationToken);

        var sheet = new SpreadsheetSheet(
            ExportLabels.Get("sheet.timeEntries", english),
            [
                new(ExportLabels.Get("employee", english), SpreadsheetValueKind.Text),
                new(ExportLabels.Get("project", english), SpreadsheetValueKind.Text),
                new(ExportLabels.Get("date", english), SpreadsheetValueKind.Date),
                new(ExportLabels.Get("started", english), SpreadsheetValueKind.Text),
                new(ExportLabels.Get("ended", english), SpreadsheetValueKind.Text),
                new(ExportLabels.Get("break", english), SpreadsheetValueKind.Integer),
                new(ExportLabels.Get("worked", english), SpreadsheetValueKind.Duration),
                new(ExportLabels.Get("workType", english), SpreadsheetValueKind.Text),
                new(ExportLabels.Get("status", english), SpreadsheetValueKind.Text),
                new(ExportLabels.Get("note", english), SpreadsheetValueKind.Text)
            ],
            rows.Select(r => (IReadOnlyList<object?>)
            [
                r.Employee,
                r.Project,
                DateOnly.FromDateTime(r.StartedAt),
                r.StartedAt.ToString("HH:mm"),
                r.EndedAt.ToString("HH:mm"),
                r.BreakMinutes,
                // Truncated the same way WorkedMinutes is, so a row here and
                // the same row on the timesheet screen cannot disagree by a
                // minute of rounding.
                (int)(r.EndedAt - r.StartedAt).TotalMinutes - r.BreakMinutes,
                r.WorkType.ToString(),
                r.Status.ToString(),
                r.Note
            ]).ToList());

        return _writer.Render(sheet, "work-hours", request);
    }
}

/// <summary>Turns one sheet into a downloadable file with a useful name.</summary>
internal static class ExportFileFactory
{
    /// <summary>
    /// Names the file after what it holds and the period it covers, so a
    /// folder of exports is still readable a month later. ASCII only: a
    /// filename crossing an email gateway with Serbian diacritics comes out
    /// the other side mangled or quoted.
    /// </summary>
    public static ExportFile Render(
        this ISpreadsheetWriter writer,
        SpreadsheetSheet sheet,
        string prefix,
        ExportQueryBase request)
    {
        var fileName = $"{prefix}-{request.From:yyyy-MM-dd}-{request.To:yyyy-MM-dd}.xlsx";

        return new ExportFile(
            fileName,
            writer.ContentType,
            writer.Write(Spreadsheet.Of(sheet)));
    }
}

// ---- cost reports ----------------------------------------------------------

/// <summary>
/// The project cost report as a spreadsheet.
/// </summary>
/// <remarks>
/// Built on top of <see cref="GetProjectCostsQuery"/> rather than repeating
/// its arithmetic. The report and the export must never disagree, and the only
/// way to guarantee that is for one to be the other.
/// </remarks>
public sealed record ExportProjectCostsQuery : ExportQueryBase, IRequest<ExportFile>
{
    public Guid? ProjectId { get; init; }
}

public class ExportProjectCostsQueryValidator
    : ExportQueryValidator<ExportProjectCostsQuery>;

public class ExportProjectCostsQueryHandler
    : IRequestHandler<ExportProjectCostsQuery, ExportFile>
{
    private readonly IMediator _mediator;
    private readonly ISpreadsheetWriter _writer;

    public ExportProjectCostsQueryHandler(IMediator mediator, ISpreadsheetWriter writer)
    {
        _mediator = mediator;
        _writer = writer;
    }

    public async Task<ExportFile> Handle(
        ExportProjectCostsQuery request,
        CancellationToken cancellationToken)
    {
        // Authorisation, the labour split and the totals all come from the
        // report, so a foreman's export withholds exactly what their screen
        // does.
        var report = await _mediator.Send(
            new GetProjectCostsQuery
            {
                From = request.From,
                To = request.To,
                ProjectId = request.ProjectId
            },
            cancellationToken);

        var english = ExportLabels.IsEnglish(request.Language);

        List<SpreadsheetColumn> columns =
        [
            new(ExportLabels.Get("project", english), SpreadsheetValueKind.Text)
        ];

        if (report.IncludesLabour)
        {
            columns.Add(new(ExportLabels.Get("hours", english), SpreadsheetValueKind.Duration));
            columns.Add(new(ExportLabels.Get("labourCost", english), SpreadsheetValueKind.Money));
            columns.Add(new(ExportLabels.Get("unpricedHours", english), SpreadsheetValueKind.Duration));
        }

        columns.Add(new(ExportLabels.Get("materialCost", english), SpreadsheetValueKind.Money));
        columns.Add(new(ExportLabels.Get("total", english), SpreadsheetValueKind.Money));

        var rows = new List<IReadOnlyList<object?>>();

        foreach (var row in report.Rows)
        {
            List<object?> cells = [row.ProjectName];

            if (report.IncludesLabour)
            {
                cells.Add(row.LabourMinutes);
                cells.Add(row.LabourCost);
                // Left empty rather than zero when everything was priced, so
                // the column draws the eye only when it has something to say.
                cells.Add(row.UnpricedMinutes == 0 ? null : row.UnpricedMinutes);
            }

            cells.Add(row.MaterialCost);
            cells.Add(row.Total);

            rows.Add(cells);
        }

        // The total sits in the sheet rather than being left to the reader:
        // an exported report that does not add up to the screen it came from
        // is the first thing anyone would query.
        if (rows.Count > 0)
        {
            List<object?> totals = [ExportLabels.Get("grandTotal", english)];

            if (report.IncludesLabour)
            {
                totals.Add(report.Rows.Sum(r => r.LabourMinutes));
                totals.Add(report.TotalLabourCost);
                totals.Add(null);
            }

            totals.Add(report.TotalMaterialCost);
            totals.Add(report.Total);

            rows.Add(totals);
        }

        var sheet = new SpreadsheetSheet(
            ExportLabels.Get("sheet.projectCosts", english), columns, rows);

        return _writer.Render(sheet, "project-costs", request);
    }
}

public sealed record ExportVehicleCostsQuery : ExportQueryBase, IRequest<ExportFile>
{
    public Guid? VehicleId { get; init; }
}

public class ExportVehicleCostsQueryValidator
    : ExportQueryValidator<ExportVehicleCostsQuery>;

public class ExportVehicleCostsQueryHandler
    : IRequestHandler<ExportVehicleCostsQuery, ExportFile>
{
    private readonly IMediator _mediator;
    private readonly ISpreadsheetWriter _writer;

    public ExportVehicleCostsQueryHandler(IMediator mediator, ISpreadsheetWriter writer)
    {
        _mediator = mediator;
        _writer = writer;
    }

    public async Task<ExportFile> Handle(
        ExportVehicleCostsQuery request,
        CancellationToken cancellationToken)
    {
        var report = await _mediator.Send(
            new GetVehicleCostsQuery
            {
                From = request.From,
                To = request.To,
                VehicleId = request.VehicleId
            },
            cancellationToken);

        var english = ExportLabels.IsEnglish(request.Language);

        var sheet = new SpreadsheetSheet(
            ExportLabels.Get("sheet.vehicleCosts", english),
            [
                new(ExportLabels.Get("vehicle", english), SpreadsheetValueKind.Text),
                new(ExportLabels.Get("fuelCost", english), SpreadsheetValueKind.Money),
                new(ExportLabels.Get("litres", english), SpreadsheetValueKind.Quantity),
                new(ExportLabels.Get("distance", english), SpreadsheetValueKind.Integer),
                new(ExportLabels.Get("consumption", english), SpreadsheetValueKind.Quantity),
                new(ExportLabels.Get("serviceCost", english), SpreadsheetValueKind.Money),
                new(ExportLabels.Get("otherCost", english), SpreadsheetValueKind.Money),
                new(ExportLabels.Get("total", english), SpreadsheetValueKind.Money)
            ],
            report.Rows
                .Select(r => (IReadOnlyList<object?>)
                [
                    r.VehicleName,
                    r.FuelCost,
                    r.Litres,
                    r.DistanceKm,
                    r.LitresPer100Km,
                    r.ServiceCost,
                    r.OtherCost,
                    r.Total
                ])
                .Concat(report.Rows.Count == 0
                    ? []
                    : new[]
                    {
                        (IReadOnlyList<object?>)
                        [
                            ExportLabels.Get("grandTotal", english),
                            report.TotalFuelCost,
                            report.TotalLitres,
                            null,
                            // Deliberately blank: an average of averages is
                            // not the fleet's consumption, and putting a
                            // plausible wrong number here is worse than a gap.
                            null,
                            null,
                            null,
                            report.Total
                        ]
                    })
                .ToList());

        return _writer.Render(sheet, "vehicle-costs", request);
    }
}

// ---- stock -----------------------------------------------------------------

public sealed record ExportMaterialMovementsQuery : ExportQueryBase, IRequest<ExportFile>
{
    public Guid? MaterialId { get; init; }

    public Guid? ProjectId { get; init; }
}

public class ExportMaterialMovementsQueryValidator
    : ExportQueryValidator<ExportMaterialMovementsQuery>;

public class ExportMaterialMovementsQueryHandler
    : IRequestHandler<ExportMaterialMovementsQuery, ExportFile>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ISpreadsheetWriter _writer;

    public ExportMaterialMovementsQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ISpreadsheetWriter writer)
    {
        _context = context;
        _currentUserService = currentUserService;
        _writer = writer;
    }

    public async Task<ExportFile> Handle(
        ExportMaterialMovementsQuery request,
        CancellationToken cancellationToken)
    {
        if (!CostRules.CanSeeSpending(_currentUserService.Role))
        {
            throw new ForbiddenAccessException("You may not export stock movements.");
        }

        var english = ExportLabels.IsEnglish(request.Language);

        var query = _context.MaterialMovements
            .AsNoTracking()
            .Where(m => m.OccurredOn >= request.From && m.OccurredOn <= request.To);

        if (request.MaterialId is { } materialId)
        {
            query = query.Where(m => m.MaterialId == materialId);
        }

        if (request.ProjectId is { } projectId)
        {
            query = query.Where(m => m.ProjectId == projectId);
        }

        var rows = await query
            .OrderBy(m => m.OccurredOn)
            .ThenBy(m => m.CreatedAt)
            .Select(m => new
            {
                m.OccurredOn,
                Material = m.Material.Name,
                m.Material.Unit,
                m.Kind,
                m.Quantity,
                m.UnitPrice,
                Project = m.Project != null ? m.Project.Name : null,
                RecordedBy = m.RecordedByUser != null ? m.RecordedByUser.Email : null,
                m.Note
            })
            .ToListAsync(cancellationToken);

        var sheet = new SpreadsheetSheet(
            ExportLabels.Get("sheet.materialMovements", english),
            [
                new(ExportLabels.Get("date", english), SpreadsheetValueKind.Date),
                new(ExportLabels.Get("material", english), SpreadsheetValueKind.Text),
                new(ExportLabels.Get("kind", english), SpreadsheetValueKind.Text),
                new(ExportLabels.Get("quantity", english), SpreadsheetValueKind.Quantity),
                new(ExportLabels.Get("unit", english), SpreadsheetValueKind.Text),
                new(ExportLabels.Get("unitPrice", english), SpreadsheetValueKind.Money),
                new(ExportLabels.Get("value", english), SpreadsheetValueKind.Money),
                new(ExportLabels.Get("project", english), SpreadsheetValueKind.Text),
                new(ExportLabels.Get("recordedBy", english), SpreadsheetValueKind.Text),
                new(ExportLabels.Get("note", english), SpreadsheetValueKind.Text)
            ],
            rows.Select(r => (IReadOnlyList<object?>)
            [
                r.OccurredOn,
                r.Material,
                r.Kind.ToString(),
                r.Quantity,
                r.Unit,
                r.UnitPrice,
                // Absolute, because a correction's quantity can be negative
                // and a negative value is not something anyone wants summed.
                r.UnitPrice is { } price ? price * Math.Abs(r.Quantity) : null,
                r.Project,
                r.RecordedBy,
                r.Note
            ]).ToList());

        return _writer.Render(sheet, "stock-movements", request);
    }
}
