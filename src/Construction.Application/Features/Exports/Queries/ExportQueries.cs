using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Common.Security;
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

    /// <summary>
    /// Names a directory export after the day it was taken, not a period —
    /// these are a snapshot of what is in the system now, not a report over a
    /// stretch of time.
    /// </summary>
    public static ExportFile RenderSnapshot(
        this ISpreadsheetWriter writer,
        SpreadsheetSheet sheet,
        string prefix,
        DateOnly takenOn)
    {
        var fileName = $"{prefix}-{takenOn:yyyy-MM-dd}.xlsx";

        return new ExportFile(
            fileName,
            writer.ContentType,
            writer.Write(Spreadsheet.Of(sheet)));
    }
}

// ---- directories -----------------------------------------------------------

/// <summary>
/// Shared shape for a directory export: the same narrow filter the list
/// screen offers, and the language the headings should be in.
/// </summary>
/// <remarks>
/// No period: a directory is a roster of what exists right now, not a log of
/// events over a stretch of time, so there is nothing to bound it by. Every
/// matching row goes in the file — these lists run to the hundreds for a
/// construction firm, not the tens of thousands a date range would guard
/// against.
/// </remarks>
public abstract record DirectoryExportQueryBase
{
    public string? Search { get; init; }

    /// <summary>`sr` or `en`. Serbian when unset.</summary>
    public string? Language { get; init; }
}

public sealed record ExportEmployeesQuery : DirectoryExportQueryBase, IRequest<ExportFile>
{
    public EmployeeStatus? Status { get; init; }
}

public class ExportEmployeesQueryHandler : IRequestHandler<ExportEmployeesQuery, ExportFile>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ISpreadsheetWriter _writer;

    public ExportEmployeesQueryHandler(
        IApplicationDbContext context,
        IDateTimeProvider dateTimeProvider,
        ISpreadsheetWriter writer)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
        _writer = writer;
    }

    public async Task<ExportFile> Handle(ExportEmployeesQuery request, CancellationToken cancellationToken)
    {
        var english = ExportLabels.IsEnglish(request.Language);

        var query = _context.Employees.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var pattern = SearchPattern.Contains(request.Search);

            query = query.Where(e =>
                EF.Functions.Like((e.FirstName + " " + e.LastName).ToLower(), pattern, SearchPattern.Escape) ||
                EF.Functions.Like(e.EmployeeNumber.ToLower(), pattern, SearchPattern.Escape) ||
                EF.Functions.Like(e.Position.ToLower(), pattern, SearchPattern.Escape));
        }

        if (request.Status is { } status)
        {
            query = query.Where(e => e.Status == status);
        }

        var rows = await query
            .OrderBy(e => e.LastName).ThenBy(e => e.FirstName)
            .Select(e => new
            {
                e.EmployeeNumber,
                Name = e.FirstName + " " + e.LastName,
                e.Position,
                e.Status,
                e.Phone,
                e.Email,
                e.EmploymentDate
            })
            .ToListAsync(cancellationToken);

        var sheet = new SpreadsheetSheet(
            ExportLabels.Get("sheet.employees", english),
            [
                new(ExportLabels.Get("employeeNumber", english), SpreadsheetValueKind.Text),
                new(ExportLabels.Get("employee", english), SpreadsheetValueKind.Text),
                new(ExportLabels.Get("position", english), SpreadsheetValueKind.Text),
                new(ExportLabels.Get("status", english), SpreadsheetValueKind.Text),
                new(ExportLabels.Get("phone", english), SpreadsheetValueKind.Text),
                new(ExportLabels.Get("email", english), SpreadsheetValueKind.Text),
                new(ExportLabels.Get("employedOn", english), SpreadsheetValueKind.Date)
            ],
            rows.Select(r => (IReadOnlyList<object?>)
            [
                r.EmployeeNumber,
                r.Name,
                r.Position,
                r.Status.ToString(),
                r.Phone,
                r.Email,
                r.EmploymentDate
            ]).ToList());

        return _writer.RenderSnapshot(
            sheet, "employees", DateOnly.FromDateTime(_dateTimeProvider.UtcNow));
    }
}

public sealed record ExportProjectsQuery : DirectoryExportQueryBase, IRequest<ExportFile>
{
    public ProjectStatus? Status { get; init; }
}

public class ExportProjectsQueryHandler : IRequestHandler<ExportProjectsQuery, ExportFile>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ISpreadsheetWriter _writer;

    public ExportProjectsQueryHandler(
        IApplicationDbContext context,
        IDateTimeProvider dateTimeProvider,
        ISpreadsheetWriter writer)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
        _writer = writer;
    }

    public async Task<ExportFile> Handle(ExportProjectsQuery request, CancellationToken cancellationToken)
    {
        var english = ExportLabels.IsEnglish(request.Language);

        var query = _context.Projects.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var pattern = SearchPattern.Contains(request.Search);

            query = query.Where(p =>
                EF.Functions.Like(p.Name.ToLower(), pattern, SearchPattern.Escape) ||
                (p.Client != null && EF.Functions.Like(p.Client.ToLower(), pattern, SearchPattern.Escape)) ||
                (p.Address != null && EF.Functions.Like(p.Address.ToLower(), pattern, SearchPattern.Escape)));
        }

        if (request.Status is { } status)
        {
            query = query.Where(p => p.Status == status);
        }

        var rows = await query
            .OrderBy(p => p.Name)
            .Select(p => new
            {
                p.Name,
                p.Client,
                p.Status,
                p.Address,
                p.StartDate,
                p.EndDate,
                EmployeeCount = p.EmployeeAssignments.Count(a => a.EndDate == null)
            })
            .ToListAsync(cancellationToken);

        var sheet = new SpreadsheetSheet(
            ExportLabels.Get("sheet.projects", english),
            [
                new(ExportLabels.Get("project", english), SpreadsheetValueKind.Text),
                new(ExportLabels.Get("client", english), SpreadsheetValueKind.Text),
                new(ExportLabels.Get("status", english), SpreadsheetValueKind.Text),
                new(ExportLabels.Get("address", english), SpreadsheetValueKind.Text),
                new(ExportLabels.Get("startDate", english), SpreadsheetValueKind.Date),
                new(ExportLabels.Get("endDate", english), SpreadsheetValueKind.Date),
                new(ExportLabels.Get("crew", english), SpreadsheetValueKind.Integer)
            ],
            rows.Select(r => (IReadOnlyList<object?>)
            [
                r.Name,
                r.Client,
                r.Status.ToString(),
                r.Address,
                r.StartDate,
                r.EndDate,
                r.EmployeeCount
            ]).ToList());

        return _writer.RenderSnapshot(
            sheet, "projects", DateOnly.FromDateTime(_dateTimeProvider.UtcNow));
    }
}

public sealed record ExportVehiclesQuery : DirectoryExportQueryBase, IRequest<ExportFile>
{
    public VehicleStatus? Status { get; init; }
}

public class ExportVehiclesQueryHandler : IRequestHandler<ExportVehiclesQuery, ExportFile>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ISpreadsheetWriter _writer;

    public ExportVehiclesQueryHandler(
        IApplicationDbContext context,
        IDateTimeProvider dateTimeProvider,
        ISpreadsheetWriter writer)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
        _writer = writer;
    }

    public async Task<ExportFile> Handle(ExportVehiclesQuery request, CancellationToken cancellationToken)
    {
        var english = ExportLabels.IsEnglish(request.Language);

        var query = _context.Vehicles.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var pattern = SearchPattern.Contains(request.Search);

            query = query.Where(v =>
                EF.Functions.Like(v.Brand.ToLower(), pattern, SearchPattern.Escape) ||
                EF.Functions.Like(v.Model.ToLower(), pattern, SearchPattern.Escape) ||
                EF.Functions.Like(v.RegistrationNumber.ToLower(), pattern, SearchPattern.Escape));
        }

        if (request.Status is { } status)
        {
            query = query.Where(v => v.Status == status);
        }

        var rows = await query
            .OrderBy(v => v.Brand).ThenBy(v => v.Model)
            .Select(v => new
            {
                v.Brand,
                v.Model,
                v.RegistrationNumber,
                v.FuelType,
                v.Status,
                AssignedTo = v.AssignedEmployee != null
                    ? v.AssignedEmployee.FirstName + " " + v.AssignedEmployee.LastName
                    : null
            })
            .ToListAsync(cancellationToken);

        var sheet = new SpreadsheetSheet(
            ExportLabels.Get("sheet.vehicles", english),
            [
                new(ExportLabels.Get("vehicle", english), SpreadsheetValueKind.Text),
                new(ExportLabels.Get("registration", english), SpreadsheetValueKind.Text),
                new(ExportLabels.Get("fuelType", english), SpreadsheetValueKind.Text),
                new(ExportLabels.Get("status", english), SpreadsheetValueKind.Text),
                new(ExportLabels.Get("assignedTo", english), SpreadsheetValueKind.Text)
            ],
            rows.Select(r => (IReadOnlyList<object?>)
            [
                $"{r.Brand} {r.Model}",
                r.RegistrationNumber,
                r.FuelType.ToString(),
                r.Status.ToString(),
                r.AssignedTo
            ]).ToList());

        return _writer.RenderSnapshot(
            sheet, "vehicles", DateOnly.FromDateTime(_dateTimeProvider.UtcNow));
    }
}

public sealed record ExportToolsQuery : DirectoryExportQueryBase, IRequest<ExportFile>
{
    public ToolStatus? Status { get; init; }
}

public class ExportToolsQueryHandler : IRequestHandler<ExportToolsQuery, ExportFile>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ISpreadsheetWriter _writer;

    public ExportToolsQueryHandler(
        IApplicationDbContext context,
        IDateTimeProvider dateTimeProvider,
        ISpreadsheetWriter writer)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
        _writer = writer;
    }

    public async Task<ExportFile> Handle(ExportToolsQuery request, CancellationToken cancellationToken)
    {
        var english = ExportLabels.IsEnglish(request.Language);

        var query = _context.Tools.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var pattern = SearchPattern.Contains(request.Search);

            query = query.Where(tool =>
                EF.Functions.Like(tool.Name.ToLower(), pattern, SearchPattern.Escape) ||
                (tool.Category != null && EF.Functions.Like(tool.Category.ToLower(), pattern, SearchPattern.Escape)) ||
                (tool.SerialNumber != null && EF.Functions.Like(tool.SerialNumber.ToLower(), pattern, SearchPattern.Escape)));
        }

        if (request.Status is { } status)
        {
            query = query.Where(tool => tool.Status == status);
        }

        var rows = await query
            .OrderBy(tool => tool.Name)
            .Select(tool => new
            {
                tool.Name,
                tool.Category,
                tool.SerialNumber,
                tool.Status,
                AssignedTo = tool.AssignedEmployee != null
                    ? tool.AssignedEmployee.FirstName + " " + tool.AssignedEmployee.LastName
                    : tool.AssignedProject != null ? tool.AssignedProject.Name : null
            })
            .ToListAsync(cancellationToken);

        var sheet = new SpreadsheetSheet(
            ExportLabels.Get("sheet.tools", english),
            [
                new(ExportLabels.Get("tool", english), SpreadsheetValueKind.Text),
                new(ExportLabels.Get("category", english), SpreadsheetValueKind.Text),
                new(ExportLabels.Get("serialNumber", english), SpreadsheetValueKind.Text),
                new(ExportLabels.Get("status", english), SpreadsheetValueKind.Text),
                new(ExportLabels.Get("heldBy", english), SpreadsheetValueKind.Text)
            ],
            rows.Select(r => (IReadOnlyList<object?>)
            [
                r.Name,
                r.Category,
                r.SerialNumber,
                r.Status.ToString(),
                r.AssignedTo
            ]).ToList());

        return _writer.RenderSnapshot(
            sheet, "tools", DateOnly.FromDateTime(_dateTimeProvider.UtcNow));
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

// ---- absences ----------------------------------------------------------

/// <summary>
/// Time off over a period, row by row.
/// </summary>
/// <remarks>
/// No extra role check: the route already requires <c>ForemanAndAbove</c>,
/// and every one of those roles sees every employee's leave on the list
/// screen too — only a worker is narrowed to their own, and a worker cannot
/// reach this endpoint at all.
/// </remarks>
public sealed record ExportAbsencesQuery : ExportQueryBase, IRequest<ExportFile>
{
    public Guid? EmployeeId { get; init; }

    public AbsenceStatus? Status { get; init; }

    public AbsenceType? Type { get; init; }
}

public class ExportAbsencesQueryValidator : ExportQueryValidator<ExportAbsencesQuery>;

public class ExportAbsencesQueryHandler : IRequestHandler<ExportAbsencesQuery, ExportFile>
{
    private readonly IApplicationDbContext _context;
    private readonly ISpreadsheetWriter _writer;

    public ExportAbsencesQueryHandler(IApplicationDbContext context, ISpreadsheetWriter writer)
    {
        _context = context;
        _writer = writer;
    }

    public async Task<ExportFile> Handle(
        ExportAbsencesQuery request,
        CancellationToken cancellationToken)
    {
        var english = ExportLabels.IsEnglish(request.Language);

        // Overlap, not containment, matching the list screen: a stretch of
        // leave that started before the window but runs into it belongs here.
        var query = _context.Absences
            .AsNoTracking()
            .Where(a => a.EndDate >= request.From && a.StartDate <= request.To);

        if (request.EmployeeId is { } employeeId)
        {
            query = query.Where(a => a.EmployeeId == employeeId);
        }

        if (request.Status is { } status)
        {
            query = query.Where(a => a.Status == status);
        }

        if (request.Type is { } type)
        {
            query = query.Where(a => a.Type == type);
        }

        var rows = await query
            .OrderBy(a => a.StartDate)
            .Select(a => new
            {
                Employee = a.Employee.FirstName + " " + a.Employee.LastName,
                a.Type,
                a.Status,
                a.StartDate,
                a.EndDate,
                DayCount = a.EndDate.DayNumber - a.StartDate.DayNumber + 1,
                a.Reason,
                RequestedBy = a.RequestedByUser != null ? a.RequestedByUser.Email : null,
                ReviewedBy = a.ReviewedByUser != null ? a.ReviewedByUser.Email : null,
                a.ReviewNote
            })
            .ToListAsync(cancellationToken);

        var sheet = new SpreadsheetSheet(
            ExportLabels.Get("sheet.absences", english),
            [
                new(ExportLabels.Get("employee", english), SpreadsheetValueKind.Text),
                new(ExportLabels.Get("absenceType", english), SpreadsheetValueKind.Text),
                new(ExportLabels.Get("status", english), SpreadsheetValueKind.Text),
                new(ExportLabels.Get("startDate", english), SpreadsheetValueKind.Date),
                new(ExportLabels.Get("endDate", english), SpreadsheetValueKind.Date),
                new(ExportLabels.Get("days", english), SpreadsheetValueKind.Integer),
                new(ExportLabels.Get("reason", english), SpreadsheetValueKind.Text),
                new(ExportLabels.Get("requestedBy", english), SpreadsheetValueKind.Text),
                new(ExportLabels.Get("reviewedBy", english), SpreadsheetValueKind.Text),
                new(ExportLabels.Get("note", english), SpreadsheetValueKind.Text)
            ],
            rows.Select(r => (IReadOnlyList<object?>)
            [
                r.Employee,
                r.Type.ToString(),
                r.Status.ToString(),
                r.StartDate,
                r.EndDate,
                r.DayCount,
                r.Reason,
                r.RequestedBy,
                r.ReviewedBy,
                r.ReviewNote
            ]).ToList());

        return _writer.Render(sheet, "absences", request);
    }
}

// ---- finance entries -----------------------------------------------------

public sealed record ExportFinanceEntriesQuery : ExportQueryBase, IRequest<ExportFile>
{
    public Guid? EmployeeId { get; init; }

    public Guid? ProjectId { get; init; }

    public FinanceEntryKind? Kind { get; init; }
}

public class ExportFinanceEntriesQueryValidator : ExportQueryValidator<ExportFinanceEntriesQuery>;

public class ExportFinanceEntriesQueryHandler
    : IRequestHandler<ExportFinanceEntriesQuery, ExportFile>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ISpreadsheetWriter _writer;

    public ExportFinanceEntriesQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ISpreadsheetWriter writer)
    {
        _context = context;
        _currentUserService = currentUserService;
        _writer = writer;
    }

    public async Task<ExportFile> Handle(
        ExportFinanceEntriesQuery request,
        CancellationToken cancellationToken)
    {
        // Same tier as pay rates: this is somebody's wage, not site spending.
        if (!CostRules.CanSeeLabourCost(_currentUserService.Role))
        {
            throw new ForbiddenAccessException("You may not export pay entries.");
        }

        var english = ExportLabels.IsEnglish(request.Language);

        var query = _context.FinanceEntries
            .AsNoTracking()
            .Where(e => e.OccurredOn >= request.From && e.OccurredOn <= request.To);

        if (request.EmployeeId is { } employeeId)
        {
            query = query.Where(e => e.EmployeeId == employeeId);
        }

        if (request.ProjectId is { } projectId)
        {
            query = query.Where(e => e.ProjectId == projectId);
        }

        if (request.Kind is { } kind)
        {
            query = query.Where(e => e.Kind == kind);
        }

        var rows = await query
            .OrderBy(e => e.OccurredOn)
            .ThenBy(e => e.CreatedAt)
            .Select(e => new
            {
                Employee = e.Employee.FirstName + " " + e.Employee.LastName,
                e.Kind,
                e.Amount,
                e.OccurredOn,
                Project = e.Project != null ? e.Project.Name : null,
                e.HoursWorked,
                e.Note,
                RecordedBy = e.RecordedByUser != null ? e.RecordedByUser.Email : null
            })
            .ToListAsync(cancellationToken);

        var sheet = new SpreadsheetSheet(
            ExportLabels.Get("sheet.financeEntries", english),
            [
                new(ExportLabels.Get("employee", english), SpreadsheetValueKind.Text),
                new(ExportLabels.Get("kind", english), SpreadsheetValueKind.Text),
                new(ExportLabels.Get("amount", english), SpreadsheetValueKind.Money),
                new(ExportLabels.Get("date", english), SpreadsheetValueKind.Date),
                new(ExportLabels.Get("project", english), SpreadsheetValueKind.Text),
                new(ExportLabels.Get("hours", english), SpreadsheetValueKind.Quantity),
                new(ExportLabels.Get("note", english), SpreadsheetValueKind.Text),
                new(ExportLabels.Get("recordedBy", english), SpreadsheetValueKind.Text)
            ],
            rows.Select(r => (IReadOnlyList<object?>)
            [
                r.Employee,
                r.Kind.ToString(),
                r.Amount,
                r.OccurredOn,
                r.Project,
                r.HoursWorked,
                r.Note,
                r.RecordedBy
            ]).ToList());

        return _writer.Render(sheet, "finance-entries", request);
    }
}
