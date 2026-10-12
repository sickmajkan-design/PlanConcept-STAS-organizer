using System.Globalization;
using System.Text;
using System.Text.Json;
using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Ledgers.Commands.CreateLedger;
using Construction.Application.Features.Ledgers.Models;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.BookImport;

/// <summary>
/// One person's row of the old monthly sheet, as the browser read it. Numbers arrive as the text the sheet
/// held, so the server decides what is a number and says which row was not, and the preview and the real
/// import are worked out by the same code.
/// </summary>
public class BookImportRowInput
{
    /// <summary>The client's name: the heading the row sits under.</summary>
    public string Section { get; init; } = "";

    /// <summary>The worker's name, or a label such as "Kooperanti".</summary>
    public string Label { get; init; } = "";

    /// <summary>Hours per week of the sheet, up to six.</summary>
    public IReadOnlyList<string?> Weeks { get; init; } = [];

    public string? WorkerRate { get; init; }

    public string? ClientRate { get; init; }

    public string? Advance { get; init; }

    public string? Difference { get; init; }

    public string? Contributions { get; init; }

    public string? Fuel { get; init; }

    public string? Housing { get; init; }

    public string? Holiday { get; init; }

    public string? Bonus { get; init; }
}

public class BookImportInput
{
    public string FileName { get; init; } = "";

    public int Year { get; init; }

    public int Month { get; init; }

    /// <summary>
    /// For a month that already has a settlement: write only the cells that are still empty. Without it an
    /// import into a month that has entries is refused.
    /// </summary>
    public bool FillEmptyOnly { get; init; }

    public IReadOnlyList<BookImportRowInput> Rows { get; init; } = [];

    /// <summary>The person's choice of project for a section name, where the name did not match one.</summary>
    public IReadOnlyDictionary<string, Guid?> SectionProjects { get; init; } = new Dictionary<string, Guid?>();
}

public class BookImportPlanRowDto
{
    public int Index { get; init; }

    public string Section { get; init; } = null!;

    public string Label { get; init; } = null!;

    public Guid? EmployeeId { get; init; }

    public string? EmployeeName { get; init; }

    public Guid? ProjectId { get; init; }

    public string? ProjectName { get; init; }

    /// <summary>"Ok", "Warning" or "Error". An error row is left out of the import.</summary>
    public string Status { get; init; } = "Ok";

    public IReadOnlyList<string> Messages { get; init; } = [];

    public decimal Hours { get; init; }
}

public class BookImportPlanDto
{
    public int Year { get; init; }

    public int Month { get; init; }

    /// <summary>The settlement of the month that already exists, when there is one.</summary>
    public Guid? ExistingLedgerId { get; init; }

    public bool ExistingHasEntries { get; init; }

    public IReadOnlyList<BookImportPlanRowDto> Rows { get; init; } = [];

    public IReadOnlyList<string> Sections { get; init; } = [];

    /// <summary>Sections whose client could not be found among the projects, for the person to choose.</summary>
    public IReadOnlyList<string> UnmatchedSections { get; init; } = [];

    public int OkCount { get; init; }

    public int WarningCount { get; init; }

    public int ErrorCount { get; init; }

    /// <summary>The sheet's own total, to be compared with the settlement after the import.</summary>
    public decimal SheetHours { get; init; }

    public decimal SheetBilling { get; init; }
}

public class BookImportResultDto
{
    public Guid BatchId { get; init; }

    public Guid LedgerId { get; init; }

    public bool LedgerCreated { get; init; }

    public int Imported { get; init; }

    public int Skipped { get; init; }

    public decimal SheetHours { get; init; }

    public decimal LedgerHours { get; init; }

    public decimal SheetBilling { get; init; }

    public decimal LedgerBilling { get; init; }

    /// <summary>Ledger minus sheet; zero when the import carried every hour and price over.</summary>
    public decimal HoursDifference => LedgerHours - SheetHours;

    public decimal BillingDifference => LedgerBilling - SheetBilling;

    public BookImportPlanDto Plan { get; init; } = null!;
}

public class BookImportBatchDto
{
    public Guid Id { get; init; }

    public string FileName { get; init; } = null!;

    public int Year { get; init; }

    public int Month { get; init; }

    public string Status { get; init; } = null!;

    public int RowCount { get; init; }

    public int ImportedCount { get; init; }

    public int SkippedCount { get; init; }

    public Guid? LedgerId { get; init; }

    public bool LedgerCreated { get; init; }

    public DateTime CreatedAt { get; init; }

    /// <summary>Row by row what became of the file; null once it has been cleared by the retention sweep.</summary>
    public BookImportPlanDto? Report { get; init; }
}

internal static class ImportNames
{
    /// <summary>Lower case, no diacritics, only letters and digits, single spaces.</summary>
    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var decomposed = value.Trim().Replace('đ', 'd').Replace('Đ', 'D').Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        var lastSpace = true;

        foreach (var ch in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(ch))
            {
                builder.Append(char.ToLowerInvariant(ch));
                lastSpace = false;
            }
            else if (!lastSpace)
            {
                builder.Append(' ');
                lastSpace = true;
            }
        }

        return builder.ToString().Trim();
    }

    public static decimal? ParseNumber(string? text, out bool invalid)
    {
        invalid = false;

        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var cleaned = text.Trim().Replace(" ", string.Empty).Replace("€", string.Empty);

        // "1.234,50" and "1,234.50" are both met in sheets; the last separator is the decimal one.
        var lastComma = cleaned.LastIndexOf(',');
        var lastDot = cleaned.LastIndexOf('.');

        if (lastComma >= 0 && lastDot >= 0)
        {
            cleaned = lastComma > lastDot
                ? cleaned.Replace(".", string.Empty).Replace(',', '.')
                : cleaned.Replace(",", string.Empty);
        }
        else if (lastComma >= 0)
        {
            cleaned = cleaned.Replace(',', '.');
        }

        if (decimal.TryParse(cleaned, NumberStyles.Number | NumberStyles.AllowLeadingSign,
                CultureInfo.InvariantCulture, out var value))
        {
            return value;
        }

        invalid = true;

        return null;
    }
}

/// <summary>
/// Works out, row by row, what an import would do. The preview shows it and the import writes it, from this
/// one function, so what a person sees in the check is exactly what is written.
/// </summary>
internal static class BookImportPlanner
{
    public const decimal ExpectedMonthlyHours = 176m;

    public const int WeekColumns = 6;

    internal sealed record Values(
        decimal?[] Weeks, decimal? WorkerRate, decimal? ClientRate, decimal? Advance, decimal? Difference,
        decimal? Contributions, decimal? Fuel, decimal? Housing, decimal? Holiday, decimal? Bonus);

    internal sealed record PlannedRow(BookImportPlanRowDto Row, Values Values, bool Skipped);

    public static async Task<(BookImportPlanDto Plan, List<PlannedRow> Rows)> BuildAsync(
        IApplicationDbContext context, BookImportInput input, CancellationToken cancellationToken)
    {
        var employees = await context.Employees
            .AsNoTracking()
            .Select(e => new { e.Id, e.FirstName, e.LastName })
            .ToListAsync(cancellationToken);

        // First and last name in either order, and the short form "Marko H.", all point at the person; a
        // name that points at two people points at nobody.
        var byName = new Dictionary<string, List<Guid>>();

        void Index(string key, Guid id)
        {
            if (key.Length == 0) return;
            if (!byName.TryGetValue(key, out var list)) byName[key] = list = [];
            if (!list.Contains(id)) list.Add(id);
        }

        foreach (var e in employees)
        {
            var first = ImportNames.Normalize(e.FirstName);
            var last = ImportNames.Normalize(e.LastName);
            Index($"{first} {last}", e.Id);
            Index($"{last} {first}", e.Id);

            if (last.Length > 0)
            {
                Index($"{first} {last[0]}", e.Id);
                Index($"{last[0]} {first}", e.Id);
            }
        }

        var employeeNames = employees.ToDictionary(e => e.Id, e => $"{e.FirstName} {e.LastName}");

        var projects = await context.Projects
            .AsNoTracking()
            .Select(p => new { p.Id, p.Name, p.ClientHourlyRate })
            .ToListAsync(cancellationToken);

        var projectByName = projects
            .GroupBy(p => ImportNames.Normalize(p.Name))
            .Where(g => g.Key.Length > 0 && g.Count() == 1)
            .ToDictionary(g => g.Key, g => g.Single());

        var projectById = projects.ToDictionary(p => p.Id);

        var sectionProjects = new Dictionary<string, Guid?>();
        var unmatched = new List<string>();

        foreach (var section in input.Rows.Select(r => r.Section.Trim()).Where(s => s.Length > 0).Distinct())
        {
            Guid? chosen = null;

            if (input.SectionProjects.TryGetValue(section, out var picked) && picked is { } pickedId
                && projectById.ContainsKey(pickedId))
            {
                chosen = pickedId;
            }
            else if (projectByName.TryGetValue(ImportNames.Normalize(section), out var match))
            {
                chosen = match.Id;
            }

            sectionProjects[section] = chosen;

            if (chosen is null)
            {
                unmatched.Add(section);
            }
        }

        var seenEmployee = new HashSet<(string, Guid)>();
        var planned = new List<PlannedRow>();
        decimal sheetHours = 0m, sheetBilling = 0m;

        for (var index = 0; index < input.Rows.Count; index++)
        {
            var source = input.Rows[index];
            var messages = new List<string>();
            var error = false;
            var warning = false;

            decimal? Read(string? text, string field)
            {
                var value = ImportNames.ParseNumber(text, out var invalid);

                if (invalid)
                {
                    messages.Add($"\"{text}\" is not a number ({field}).");
                    error = true;
                }

                return value;
            }

            var weeks = new decimal?[WeekColumns];

            for (var w = 0; w < WeekColumns; w++)
            {
                weeks[w] = w < source.Weeks.Count ? Read(source.Weeks[w], $"week {w + 1}") : null;
            }

            var values = new Values(
                weeks,
                Read(source.WorkerRate, "worker rate"),
                Read(source.ClientRate, "client rate"),
                Read(source.Advance, "advance"),
                Read(source.Difference, "difference"),
                Read(source.Contributions, "contributions"),
                Read(source.Fuel, "fuel"),
                Read(source.Housing, "housing"),
                Read(source.Holiday, "holiday"),
                Read(source.Bonus, "bonus"));

            var label = source.Label.Trim();
            var section = source.Section.Trim();

            if (label.Length == 0)
            {
                messages.Add("The row has no name.");
                error = true;
            }

            Guid? employeeId = null;

            var normalized = ImportNames.Normalize(label.TrimEnd('.'));

            if (normalized.Length > 0 && byName.TryGetValue(normalized, out var matches))
            {
                if (matches.Count == 1)
                {
                    employeeId = matches[0];
                }
                else
                {
                    messages.Add("The name fits more than one person, so the row is left unlinked.");
                    warning = true;
                }
            }
            else if (label.Length > 0)
            {
                messages.Add("This person is not in the system; the row is written with the name only.");
                warning = true;
            }

            // A row that is already an error does not take the person: the next row is the one that counts.
            if (employeeId is { } linked && !error && !seenEmployee.Add((ImportNames.Normalize(section), linked)))
            {
                messages.Add("The same person appears twice under this client.");
                error = true;
            }

            sectionProjects.TryGetValue(section, out var projectId);

            if (section.Length > 0 && projectId is null)
            {
                messages.Add("The client was not found among the projects.");
                warning = true;
            }

            var hours = weeks.Sum(w => w ?? 0m);

            if (hours > ExpectedMonthlyHours)
            {
                messages.Add($"{hours:0.##} hours is more than a month has ({ExpectedMonthlyHours:0}).");
                warning = true;
            }

            var clientRate = values.ClientRate
                ?? (projectId is { } p && projectById[p].ClientHourlyRate is { } rate ? rate : (decimal?)null);

            if (hours > 0 && clientRate is null)
            {
                messages.Add("There are hours but no price for the client.");
                warning = true;
            }

            if (hours > 0 && values.WorkerRate is null && employeeId is null)
            {
                messages.Add("There are hours but no worker rate, and the person is not in the system.");
                warning = true;
            }

            if (!error)
            {
                sheetHours += hours;
                sheetBilling += hours * (clientRate ?? 0m);
            }

            planned.Add(new PlannedRow(
                new BookImportPlanRowDto
                {
                    Index = index,
                    Section = section,
                    Label = label,
                    EmployeeId = employeeId,
                    EmployeeName = employeeId is { } id ? employeeNames[id] : null,
                    ProjectId = projectId,
                    ProjectName = projectId is { } pid ? projectById[pid].Name : null,
                    Status = error ? "Error" : warning ? "Warning" : "Ok",
                    Messages = messages,
                    Hours = hours,
                },
                values,
                Skipped: error));
        }

        var ledger = await context.Ledgers
            .AsNoTracking()
            .Where(l => l.Year == input.Year && l.Month == input.Month)
            .OrderBy(l => l.CreatedAt)
            .Select(l => new { l.Id })
            .FirstOrDefaultAsync(cancellationToken);

        var hasEntries = ledger is not null
            && await context.LedgerCells.AsNoTracking()
                .AnyAsync(c => c.Row.Section.LedgerId == ledger.Id && c.Value != null && c.Value != "", cancellationToken);

        var plan = new BookImportPlanDto
        {
            Year = input.Year,
            Month = input.Month,
            ExistingLedgerId = ledger?.Id,
            ExistingHasEntries = hasEntries,
            Rows = planned.Select(p => p.Row).ToList(),
            Sections = sectionProjects.Keys.ToList(),
            UnmatchedSections = unmatched,
            OkCount = planned.Count(p => p.Row.Status == "Ok"),
            WarningCount = planned.Count(p => p.Row.Status == "Warning"),
            ErrorCount = planned.Count(p => p.Row.Status == "Error"),
            SheetHours = sheetHours,
            SheetBilling = Math.Round(sheetBilling, 2),
        };

        return (plan, planned);
    }
}

internal static class BookImportRules
{
    public const int MaxRows = 5000;

    public static void RequireSuperAdmin(ICurrentUserService currentUserService)
    {
        if (currentUserService.Role != UserRole.SuperAdmin)
        {
            throw new ForbiddenAccessException("Only the Super Admin may import whole months.");
        }
    }
}

public class BookImportInputValidator : AbstractValidator<BookImportInput>
{
    public BookImportInputValidator()
    {
        RuleFor(x => x.Year).InclusiveBetween(2000, 2100);
        RuleFor(x => x.Month).InclusiveBetween(1, 12);
        RuleFor(x => x.FileName).MaximumLength(256);
        RuleFor(x => x.Rows).NotEmpty().WithMessage("The file has no rows to import.");
        RuleFor(x => x.Rows.Count).LessThanOrEqualTo(BookImportRules.MaxRows)
            .WithMessage($"A file can hold at most {BookImportRules.MaxRows} rows.")
            .OverridePropertyName("Rows");
    }
}

/// <summary>Shows what an import would do, row by row, without writing anything.</summary>
public record PreviewBookImportCommand(BookImportInput Input) : IRequest<BookImportPlanDto>;

public class PreviewBookImportCommandValidator : AbstractValidator<PreviewBookImportCommand>
{
    public PreviewBookImportCommandValidator()
    {
        RuleFor(x => x.Input).SetValidator(new BookImportInputValidator());
    }
}

public class PreviewBookImportCommandHandler : IRequestHandler<PreviewBookImportCommand, BookImportPlanDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public PreviewBookImportCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<BookImportPlanDto> Handle(PreviewBookImportCommand request, CancellationToken cancellationToken)
    {
        BookImportRules.RequireSuperAdmin(_currentUserService);

        var (plan, _) = await BookImportPlanner.BuildAsync(_context, request.Input, cancellationToken);

        return plan;
    }
}

/// <summary>
/// Writes the file into the month's settlement, making it from the payroll template when there is none. Rows
/// with an error are left out and reported. Running it again over the same file writes the same cells, so it
/// does not duplicate anything.
/// </summary>
public record CommitBookImportCommand(BookImportInput Input) : IRequest<BookImportResultDto>;

public class CommitBookImportCommandValidator : AbstractValidator<CommitBookImportCommand>
{
    public CommitBookImportCommandValidator()
    {
        RuleFor(x => x.Input).SetValidator(new BookImportInputValidator());
    }
}

public class CommitBookImportCommandHandler : IRequestHandler<CommitBookImportCommand, BookImportResultDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ISender _sender;

    public CommitBookImportCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        ISender sender)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
        _sender = sender;
    }

    public async Task<BookImportResultDto> Handle(CommitBookImportCommand request, CancellationToken cancellationToken)
    {
        BookImportRules.RequireSuperAdmin(_currentUserService);

        var input = request.Input;
        var (plan, planned) = await BookImportPlanner.BuildAsync(_context, input, cancellationToken);

        if (plan.ExistingLedgerId is not null && plan.ExistingHasEntries && !input.FillEmptyOnly)
        {
            throw new ConflictException(
                "This month already has entries. Import only into its empty cells, or choose another month.");
        }

        var created = false;
        Guid ledgerId;

        if (plan.ExistingLedgerId is { } existing)
        {
            ledgerId = existing;
        }
        else
        {
            var ledger = await _sender.Send(new CreateLedgerCommand
            {
                Name = $"Uvoz {input.FileName} {input.Month:00}/{input.Year}".Trim(),
                Year = input.Year,
                Month = input.Month,
                Template = LedgerTemplates.Payroll,
            }, cancellationToken);

            ledgerId = ledger.Id;
            created = true;
        }

        var columns = await _context.LedgerColumns
            .AsNoTracking()
            .Where(c => c.LedgerId == ledgerId && c.SystemKey != null)
            .Select(c => new { c.Id, c.SystemKey })
            .ToListAsync(cancellationToken);

        Guid? Column(string key) => columns.FirstOrDefault(c => c.SystemKey == key)?.Id;

        var sections = await _context.LedgerSections
            .Include(s => s.Rows).ThenInclude(r => r.Cells)
            .Where(s => s.LedgerId == ledgerId)
            .ToListAsync(cancellationToken);

        var nextSection = sections.Count == 0 ? 0 : sections.Max(s => s.SortOrder) + 1;
        var imported = 0;

        foreach (var item in planned.Where(p => !p.Skipped))
        {
            var row = item.Row;
            var name = row.Section.Length > 0 ? row.Section : "Uvoz";

            var section = sections.FirstOrDefault(s =>
                    row.ProjectId is { } pid ? s.ProjectId == pid : false)
                ?? sections.FirstOrDefault(s =>
                    ImportNames.Normalize(s.Name) == ImportNames.Normalize(name));

            if (section is null)
            {
                section = new LedgerSection
                {
                    LedgerId = ledgerId,
                    Name = name,
                    ProjectId = row.ProjectId,
                    SortOrder = nextSection++,
                };

                _context.LedgerSections.Add(section);
                sections.Add(section);
            }

            var ledgerRow = section.Rows.FirstOrDefault(r =>
                row.EmployeeId is { } eid
                    ? r.EmployeeId == eid
                    : r.EmployeeId == null && ImportNames.Normalize(r.Label) == ImportNames.Normalize(row.Label));

            if (ledgerRow is null)
            {
                ledgerRow = new LedgerRow
                {
                    SectionId = section.Id,
                    Section = section,
                    Label = row.Label,
                    EmployeeId = row.EmployeeId,
                    SortOrder = section.Rows.Count == 0 ? 0 : section.Rows.Max(r => r.SortOrder) + 1,
                };

                section.Rows.Add(ledgerRow);
            }

            void Put(string key, decimal? value)
            {
                if (value is null || Column(key) is not { } columnId)
                {
                    return;
                }

                var cell = ledgerRow.Cells.FirstOrDefault(c => c.ColumnId == columnId);
                var text = LedgerColumnTotals.Format(value.Value);

                if (cell is null)
                {
                    ledgerRow.Cells.Add(new LedgerCell { ColumnId = columnId, Value = text });
                }
                else if (!input.FillEmptyOnly || string.IsNullOrWhiteSpace(cell.Value))
                {
                    cell.Value = text;
                }
            }

            for (var w = 0; w < item.Values.Weeks.Length; w++)
            {
                Put(LedgerTemplates.Keys.Week(w + 1), item.Values.Weeks[w]);
            }

            Put(LedgerTemplates.Keys.WorkerRate, item.Values.WorkerRate);
            Put(LedgerTemplates.Keys.ClientRate, item.Values.ClientRate);
            Put(LedgerTemplates.Keys.Advance, item.Values.Advance);
            Put(LedgerTemplates.Keys.Difference, item.Values.Difference);
            Put(LedgerTemplates.Keys.Contributions, item.Values.Contributions);
            Put(LedgerTemplates.Keys.Fuel, item.Values.Fuel);
            Put(LedgerTemplates.Keys.Housing, item.Values.Housing);
            Put(LedgerTemplates.Keys.Holiday, item.Values.Holiday);
            Put(LedgerTemplates.Keys.Bonus, item.Values.Bonus);

            imported++;
        }

        var skipped = planned.Count - imported;

        var batch = new BookImportBatch
        {
            FileName = input.FileName,
            TargetYear = input.Year,
            TargetMonth = input.Month,
            Status = "Completed",
            RowCount = planned.Count,
            ImportedCount = imported,
            SkippedCount = skipped,
            LedgerId = ledgerId,
            LedgerCreated = created,
            ReportJson = JsonSerializer.Serialize(plan),
            RecordedByUserId = _currentUserService.UserId,
        };

        _context.BookImportBatches.Add(batch);
        await _context.SaveChangesAsync(cancellationToken);

        batch.CompletedAt = _dateTimeProvider.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        // The check that matters: what the sheet said against what the settlement now says.
        var hoursColumn = Column(LedgerTemplates.Keys.Hours);
        var billingColumn = Column(LedgerTemplates.Keys.Billing);
        var ids = new[] { hoursColumn, billingColumn }.Where(i => i is not null).Select(i => i!.Value).ToList();

        var totals = await LedgerColumnTotals.ComputeAsync(_context, ledgerId, ids, cancellationToken);

        return new BookImportResultDto
        {
            BatchId = batch.Id,
            LedgerId = ledgerId,
            LedgerCreated = created,
            Imported = imported,
            Skipped = skipped,
            SheetHours = plan.SheetHours,
            LedgerHours = hoursColumn is { } h ? totals[h] : 0m,
            SheetBilling = plan.SheetBilling,
            LedgerBilling = billingColumn is { } b ? Math.Round(totals[b], 2) : 0m,
            Plan = plan,
        };
    }
}

/// <summary>
/// Takes back an import that made its settlement, while nobody has changed it since and it is not closed.
/// The whole settlement goes (it is hidden, not erased).
/// </summary>
public record UndoBookImportCommand(Guid BatchId) : IRequest;

public class UndoBookImportCommandHandler : IRequestHandler<UndoBookImportCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public UndoBookImportCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task Handle(UndoBookImportCommand request, CancellationToken cancellationToken)
    {
        BookImportRules.RequireSuperAdmin(_currentUserService);

        var batch = await _context.BookImportBatches
            .FirstOrDefaultAsync(b => b.Id == request.BatchId, cancellationToken)
            ?? throw new NotFoundException(nameof(BookImportBatch), request.BatchId);

        if (batch.Status == "Undone")
        {
            throw new ConflictException("This import has already been undone.");
        }

        if (!batch.LedgerCreated || batch.LedgerId is not { } ledgerId)
        {
            throw new ConflictException(
                "This import wrote into a month that already existed, so it cannot be taken back as a whole.");
        }

        var ledger = await _context.Ledgers
            .FirstOrDefaultAsync(l => l.Id == ledgerId, cancellationToken)
            ?? throw new ConflictException("The settlement this import made no longer exists.");

        if (ledger.LockedAt is not null)
        {
            throw new ConflictException("The month is closed. Reopen it first.");
        }

        var completed = batch.CompletedAt ?? batch.CreatedAt;

        var changedSince = await _context.LedgerCells.AsNoTracking()
            .AnyAsync(c => c.Row.Section.LedgerId == ledgerId && c.UpdatedAt != null && c.UpdatedAt > completed,
                cancellationToken);

        if (changedSince)
        {
            throw new ConflictException(
                "The month has been changed by hand since it was imported, so it cannot be taken back.");
        }

        _context.Ledgers.Remove(ledger);
        batch.Status = "Undone";

        await _context.SaveChangesAsync(cancellationToken);
    }
}

public record GetBookImportsQuery : IRequest<IReadOnlyList<BookImportBatchDto>>;

public class GetBookImportsQueryHandler : IRequestHandler<GetBookImportsQuery, IReadOnlyList<BookImportBatchDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetBookImportsQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<BookImportBatchDto>> Handle(
        GetBookImportsQuery request, CancellationToken cancellationToken)
    {
        BookImportRules.RequireSuperAdmin(_currentUserService);

        var batches = await _context.BookImportBatches
            .AsNoTracking()
            .OrderByDescending(b => b.CreatedAt)
            .Take(100)
            .ToListAsync(cancellationToken);

        return batches.Select(b => new BookImportBatchDto
        {
            Id = b.Id,
            FileName = b.FileName,
            Year = b.TargetYear,
            Month = b.TargetMonth,
            Status = b.Status,
            RowCount = b.RowCount,
            ImportedCount = b.ImportedCount,
            SkippedCount = b.SkippedCount,
            LedgerId = b.LedgerId,
            LedgerCreated = b.LedgerCreated,
            CreatedAt = b.CreatedAt,
            Report = string.IsNullOrWhiteSpace(b.ReportJson)
                ? null
                : JsonSerializer.Deserialize<BookImportPlanDto>(b.ReportJson),
        }).ToList();
    }
}
