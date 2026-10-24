using System.Globalization;
using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Attachments;
using Construction.Application.Features.Outbox;
using Construction.Application.Features.WeeklySiteReports.Models;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.WeeklySiteReports.Commands.CreateWeeklySiteReport;

/// <summary>One picture or file of the report, as it arrives.</summary>
public record ReportFileInput(string FileName, long SizeBytes, Stream Content);

/// <summary>What one worker did in the week: hours for the whole week, and how many of them fall in the later month.</summary>
public record ReportLineInput(Guid EmployeeId, decimal Hours, decimal HoursSecondMonth = 0);

/// <summary>Submits one site's proof-of-work for one ISO week.</summary>
/// <remarks>
/// <para>
/// Bytes first, row second — same ordering <c>UploadAttachmentCommand</c> uses and for the same reason: a row
/// promising a file it cannot produce is worse than an orphaned object nobody can reach.
/// </para>
/// <para>
/// A site has one report for a week. A second submission for the same site and week is added to the first (more
/// pictures, more workers) so the office reviews it once; if the first was sent back, the new one replaces it.
/// </para>
/// </remarks>
public record CreateWeeklySiteReportCommand : IRequest<WeeklySiteReportDto>
{
    public Guid ProjectId { get; init; }

    public int IsoYear { get; init; }

    public int IsoWeek { get; init; }

    public WeeklyReportType Type { get; init; }

    /// <summary>
    /// Who to attribute the report to. Only ever honoured for
    /// SuperAdmin/Admin/ProjectManager, filing on behalf of a report that
    /// arrived by phone or in person rather than through the app — a
    /// Foreman or Worker always submits as themselves, whatever this holds.
    /// </summary>
    public Guid? SubmittedByEmployeeId { get; init; }

    /// <summary>The week's hours as one figure, for a submission that does not list the workers.</summary>
    public decimal? Quantity { get; init; }

    public string? Note { get; init; }

    /// <summary>For a report the office enters because it came by e-mail or on paper: where it came from.</summary>
    public string? SourceNote { get; init; }

    /// <summary>The pictures of the signed sheet: a week can be written on several.</summary>
    public IReadOnlyList<ReportFileInput> Files { get; init; } = [];

    /// <summary>The workers and their hours; when given, the report's total is their sum.</summary>
    public IReadOnlyList<ReportLineInput> Lines { get; init; } = [];
}

public class CreateWeeklySiteReportCommandValidator : AbstractValidator<CreateWeeklySiteReportCommand>
{
    /// <summary>More hours than a week holds are a typing mistake.</summary>
    public const decimal MaxWeekHours = 168m;

    public const int MaxFiles = 20;

    public CreateWeeklySiteReportCommandValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();

        RuleFor(x => x.IsoWeek).InclusiveBetween(1, 53);

        RuleFor(x => x.IsoYear).InclusiveBetween(2020, 2100);

        RuleFor(x => x.Type).IsInEnum();

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Hours must be greater than zero.")
            .When(x => x.Quantity is not null);

        RuleFor(x => x.Note).MaximumLength(1000);

        RuleFor(x => x.SourceNote).MaximumLength(500);

        RuleFor(x => x.Files)
            .NotEmpty().WithMessage("A file is required — the office cannot bill without proof.")
            .Must(files => files.Count <= MaxFiles).WithMessage($"At most {MaxFiles} pictures in one report.");

        RuleForEach(x => x.Files).ChildRules(file =>
        {
            file.RuleFor(f => f.FileName)
                .NotEmpty().WithMessage("A file is required — the office cannot bill without proof.");

            file.RuleFor(f => f.FileName)
                .Must(name => AttachmentRules.ResolveContentType(name) is not null)
                .WithMessage(
                    "That file type is not accepted. Allowed: " +
                    string.Join(", ", AttachmentRules.AllowedTypesByExtension.Keys))
                .When(f => !string.IsNullOrWhiteSpace(f.FileName));

            file.RuleFor(f => f.SizeBytes)
                .GreaterThan(0).WithMessage("The file is empty.")
                .LessThanOrEqualTo(AttachmentRules.MaxSizeBytes)
                .WithMessage(
                    $"The file is larger than the {AttachmentRules.MaxSizeBytes / (1024 * 1024)} MB limit.");
        });

        RuleFor(x => x.Lines)
            .Must(ReportLines.HaveNoDuplicates).WithMessage("The same worker is listed twice.");

        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.EmployeeId).NotEmpty();
            line.RuleFor(l => l.Hours).InclusiveBetween(0m, MaxWeekHours);
            line.RuleFor(l => l.HoursSecondMonth).GreaterThanOrEqualTo(0m);
            line.RuleFor(l => l).Must(l => l.HoursSecondMonth <= l.Hours)
                .WithMessage("More hours in the later month than in the whole week.");
        });
    }
}

/// <summary>Checks the lines of a report share between submitting it and correcting it.</summary>
public static class ReportLines
{
    public static bool HaveNoDuplicates(IReadOnlyList<ReportLineInput> lines) =>
        lines.Select(l => l.EmployeeId).Distinct().Count() == lines.Count;

    /// <summary>Hours may be put into the later month only in a week that runs over a month's end.</summary>
    public static void EnsureSplitFits(int isoYear, int isoWeek, IEnumerable<ReportLineInput> lines)
    {
        var (_, second) = WeeklyReportLedger.MonthsOf(isoYear, isoWeek);

        if (second is null && lines.Any(l => l.HoursSecondMonth > 0))
        {
            throw new ConflictException("This week does not run over into the next month, so no hours belong to it.");
        }
    }

    /// <summary>
    /// Puts the workers of a report in place of the ones it had. The total follows the lines, for a report of signed
    /// hours. Nothing is saved here.
    /// </summary>
    public static async Task ReplaceAsync(
        IApplicationDbContext context,
        WeeklySiteReport report,
        IReadOnlyList<ReportLineInput> lines,
        CancellationToken cancellationToken)
    {
        EnsureSplitFits(report.IsoYear, report.IsoWeek, lines);
        await EnsureEmployeesExistAsync(context, lines, cancellationToken);

        context.WeeklySiteReportLines.RemoveRange(report.Lines);
        report.Lines.Clear();

        var order = 0;

        foreach (var line in lines)
        {
            report.Lines.Add(new WeeklySiteReportLine
            {
                ReportId = report.Id,
                EmployeeId = line.EmployeeId,
                Hours = line.Hours,
                HoursSecondMonth = line.HoursSecondMonth,
                SortOrder = order++,
            });
        }

        if (report.Type == WeeklyReportType.SignedHours && lines.Count > 0)
        {
            report.Quantity = lines.Sum(l => l.Hours);
        }
    }

    public static async Task EnsureEmployeesExistAsync(
        IApplicationDbContext context, IEnumerable<ReportLineInput> lines, CancellationToken cancellationToken)
    {
        var ids = lines.Select(l => l.EmployeeId).Distinct().ToList();

        if (ids.Count == 0)
        {
            return;
        }

        var found = await context.Employees.CountAsync(e => ids.Contains(e.Id), cancellationToken);

        if (found != ids.Count)
        {
            throw new NotFoundException("One of the workers in the report does not exist.");
        }
    }
}

public class CreateWeeklySiteReportCommandHandler
    : IRequestHandler<CreateWeeklySiteReportCommand, WeeklySiteReportDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IFileStorage _storage;
    private readonly IOutbox _outbox;
    private readonly INotificationService _notifications;

    public CreateWeeklySiteReportCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IFileStorage storage,
        IOutbox outbox,
        INotificationService notifications)
    {
        _context = context;
        _currentUserService = currentUserService;
        _storage = storage;
        _outbox = outbox;
        _notifications = notifications;
    }

    public async Task<WeeklySiteReportDto> Handle(
        CreateWeeklySiteReportCommand request, CancellationToken cancellationToken)
    {
        var project = await _context.Projects
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.ProjectId, cancellationToken)
            ?? throw new NotFoundException(nameof(Project), request.ProjectId);

        var employeeId = _currentUserService.EmployeeId;
        var isOfficeRole = _currentUserService.Role is UserRole.SuperAdmin or UserRole.Admin or UserRole.ProjectManager;

        // A Foreman may only file for a site they are actually posted to;
        // everyone above them may file on a foreman's behalf, the same shape
        // AttachmentRules.CanUpload uses for the roles below it.
        if (!isOfficeRole)
        {
            var isAssigned = employeeId is not null && await _context.EmployeeProjects
                .AnyAsync(
                    a => a.EmployeeId == employeeId && a.ProjectId == request.ProjectId,
                    cancellationToken);

            if (!isAssigned)
            {
                throw new ForbiddenAccessException(
                    "You may only submit a report for a site you are assigned to.");
            }
        }
        else
        {
            // The office is filing this on someone's behalf — the report
            // reached them by phone, in person, however — and is not itself
            // an employee posted to the site. Whoever they named wins;
            // failing that, whoever is actually posted there; only refuse
            // once neither gives an answer.
            if (request.SubmittedByEmployeeId is { } named)
            {
                var exists = await _context.Employees
                    .AnyAsync(e => e.Id == named, cancellationToken);

                employeeId = exists
                    ? named
                    : throw new NotFoundException(nameof(Employee), named);
            }
            else
            {
                employeeId ??= await _context.EmployeeProjects
                    .Where(a => a.ProjectId == request.ProjectId)
                    .OrderByDescending(a => a.StartDate)
                    .Select(a => (Guid?)a.EmployeeId)
                    .FirstOrDefaultAsync(cancellationToken);
            }
        }

        if (employeeId is null)
        {
            throw new ConflictException(
                "No employee is assigned to this site — pick who to attribute the report to.");
        }

        // The workers may be anyone, not only those posted to the site: a foreman can have somebody with him who
        // was never entered in the planner.
        ReportLines.EnsureSplitFits(request.IsoYear, request.IsoWeek, request.Lines);
        await ReportLines.EnsureEmployeesExistAsync(_context, request.Lines, cancellationToken);

        var existing = await _context.WeeklySiteReports
            .Include(r => r.Pages)
            .Include(r => r.Lines)
            .Where(r => r.ProjectId == request.ProjectId && r.IsoYear == request.IsoYear && r.IsoWeek == request.IsoWeek)
            .OrderBy(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

        // A report sent back is answered by sending it again: the same row takes the new pictures and workers. Any
        // other report for the week is added to.
        var report = existing.FirstOrDefault(r => r.Status == WeeklyReportStatus.CorrectionRequested)
            ?? existing.FirstOrDefault();
        var replacing = report?.Status == WeeklyReportStatus.CorrectionRequested;
        var isNew = report is null;

        report ??= new WeeklySiteReport
        {
            Id = Guid.NewGuid(),
            ProjectId = request.ProjectId,
            IsoYear = request.IsoYear,
            IsoWeek = request.IsoWeek,
            SubmittedByEmployeeId = employeeId.Value,
            // An entry by the office is marked as such; the foreman's own submission never is.
            Source = isOfficeRole ? WeeklyReportSource.Office : WeeklyReportSource.App,
            EnteredByUserId = isOfficeRole ? _currentUserService.UserId : null,
        };

        var oldKeys = replacing ? report.Pages.Select(p => p.StorageKey).Append(report.StorageKey).ToList() : [];

        // Bytes first: every picture is stored before any row says it exists.
        var saved = new List<(ReportFileInput Input, string FileName, string ContentType, string Key)>();

        try
        {
            foreach (var input in request.Files)
            {
                var fileName = AttachmentRules.SanitiseFileName(input.FileName);
                var contentType = AttachmentRules.ResolveContentType(fileName)
                    ?? throw new ConflictException("That file type is not accepted.");
                var key = $"weekly-site-reports/{request.ProjectId:N}/{Guid.NewGuid():N}{Path.GetExtension(fileName)}";

                await _storage.SaveAsync(key, input.Content, contentType, cancellationToken);
                saved.Add((input, fileName, contentType, key));
            }

            if (replacing)
            {
                _context.WeeklySiteReportPages.RemoveRange(report.Pages);
                _context.WeeklySiteReportLines.RemoveRange(report.Lines);
                report.Pages.Clear();
                report.Lines.Clear();
            }

            var firstOrder = report.Pages.Count == 0 ? 0 : report.Pages.Max(p => p.SortOrder) + 1;

            foreach (var (input, fileName, contentType, key) in saved)
            {
                report.Pages.Add(new WeeklySiteReportPage
                {
                    ReportId = report.Id,
                    SortOrder = firstOrder++,
                    FileName = fileName,
                    ContentType = contentType,
                    SizeBytes = input.SizeBytes,
                    StorageKey = key,
                });
            }

            var lineOrder = report.Lines.Count == 0 ? 0 : report.Lines.Max(l => l.SortOrder) + 1;

            foreach (var line in request.Lines)
            {
                report.Lines.Add(new WeeklySiteReportLine
                {
                    ReportId = report.Id,
                    EmployeeId = line.EmployeeId,
                    Hours = line.Hours,
                    HoursSecondMonth = line.HoursSecondMonth,
                    SortOrder = lineOrder++,
                });
            }

            // The older single-file fields describe the first picture, for anything still reading them.
            var firstPage = report.Pages.OrderBy(p => p.SortOrder).First();
            report.FileName = firstPage.FileName;
            report.ContentType = firstPage.ContentType;
            report.SizeBytes = firstPage.SizeBytes;
            report.StorageKey = firstPage.StorageKey;

            report.Type = request.Type;
            report.Note = string.IsNullOrWhiteSpace(request.Note)
                ? (isNew || replacing ? null : report.Note)
                : request.Note.Trim();

            report.Quantity = request.Type != WeeklyReportType.SignedHours
                ? null
                : report.Lines.Count > 0
                    ? report.Lines.Sum(l => l.Hours)
                    : request.Quantity ?? report.Quantity;

            // Anything sent is waiting for the office again, also after an approval: what was added is not approved.
            report.Status = WeeklyReportStatus.Submitted;
            report.ProcessedAt = null;
            report.ProcessedByUserId = null;
            report.ReviewNote = null;

            if (replacing)
            {
                report.SubmittedByEmployeeId = employeeId.Value;
            }

            if (isOfficeRole)
            {
                if (isNew || replacing)
                {
                    report.Source = WeeklyReportSource.Office;
                    report.EnteredByUserId = _currentUserService.UserId;
                }

                if (!string.IsNullOrWhiteSpace(request.SourceNote))
                {
                    report.SourceNote = request.SourceNote.Trim();
                }
            }

            if (isNew)
            {
                _context.WeeklySiteReports.Add(report);
            }

            await ForwardAsync(report, project.Name, saved, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            foreach (var (_, _, _, key) in saved)
            {
                await _storage.DeleteAsync(key, CancellationToken.None);
            }

            throw;
        }

        foreach (var key in oldKeys.Distinct())
        {
            await _storage.DeleteAsync(key, CancellationToken.None);
        }

        var created = await _context.WeeklySiteReports
            .AsNoTracking()
            .Where(r => r.Id == report.Id)
            .Select(WeeklySiteReportMapping.Projection)
            .FirstAsync(cancellationToken);

        await WeeklyReportNotices.ToOfficeAsync(
            _context,
            _notifications,
            report,
            created.ProjectName,
            created.SubmittedByEmployeeName,
            _currentUserService.UserId,
            cancellationToken);

        // The foreman is told when the office entered the report in his name, so he knows the week is covered.
        if (isOfficeRole)
        {
            await WeeklyReportNotices.ToForemanEnteredAsync(
                _context,
                _notifications,
                report,
                created.ProjectName,
                _currentUserService.UserId,
                cancellationToken);
        }

        return created;
    }

    /// <summary>Where a submitted report is also forwarded: each picture goes out as an email of its own.</summary>
    private async Task ForwardAsync(
        WeeklySiteReport report,
        string projectName,
        List<(ReportFileInput Input, string FileName, string ContentType, string Key)> saved,
        CancellationToken cancellationToken)
    {
        var forwardTo = await _context.CompanySettings
            .Select(c => c.WeeklyReportsForwardEmail)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(forwardTo))
        {
            return;
        }

        var number = 0;

        foreach (var (_, fileName, contentType, key) in saved)
        {
            number++;

            // Re-read the bytes rather than reuse the request's stream: the storage write already consumed it.
            using var forwarded = await _storage.OpenReadAsync(key, cancellationToken);

            if (forwarded is null)
            {
                continue;
            }

            using var buffer = new MemoryStream();
            await forwarded.CopyToAsync(buffer, cancellationToken);

            var part = saved.Count > 1 ? $" ({number}/{saved.Count})" : "";

            _outbox.Enqueue(new EmailPayload(
                forwardTo,
                $"KW{report.IsoWeek:D2}/{report.IsoYear} — {projectName}{part}",
                $"<p>{fileName}</p>" +
                (report.Quantity is { } hours ? $"<p>{hours.ToString("0.##", CultureInfo.InvariantCulture)} h</p>" : "") +
                (string.IsNullOrWhiteSpace(report.Note) ? "" : $"<p>{report.Note}</p>"),
                new EmailAttachment(fileName, contentType, buffer.ToArray())));
        }
    }
}
