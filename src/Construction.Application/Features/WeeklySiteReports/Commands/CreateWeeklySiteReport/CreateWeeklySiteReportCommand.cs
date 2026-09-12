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

/// <summary>Submits one site's proof-of-work for one ISO week.</summary>
/// <remarks>
/// Bytes first, row second — same ordering <c>UploadAttachmentCommand</c>
/// uses and for the same reason: a row promising a file it cannot produce is
/// worse than an orphaned object nobody can reach.
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

    public decimal? Quantity { get; init; }

    public string? Note { get; init; }

    public string FileName { get; init; } = null!;

    public long SizeBytes { get; init; }

    public Stream Content { get; init; } = Stream.Null;
}

public class CreateWeeklySiteReportCommandValidator : AbstractValidator<CreateWeeklySiteReportCommand>
{
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

        RuleFor(x => x.FileName)
            .NotEmpty().WithMessage("A file is required — the office cannot bill without proof.");

        RuleFor(x => x.FileName)
            .Must(name => AttachmentRules.ResolveContentType(name) is not null)
            .WithMessage(
                "That file type is not accepted. Allowed: " +
                string.Join(", ", AttachmentRules.AllowedTypesByExtension.Keys))
            .When(x => !string.IsNullOrWhiteSpace(x.FileName));

        RuleFor(x => x.SizeBytes)
            .GreaterThan(0).WithMessage("The file is empty.")
            .LessThanOrEqualTo(AttachmentRules.MaxSizeBytes)
            .WithMessage(
                $"The file is larger than the {AttachmentRules.MaxSizeBytes / (1024 * 1024)} MB limit.");
    }
}

public class CreateWeeklySiteReportCommandHandler
    : IRequestHandler<CreateWeeklySiteReportCommand, WeeklySiteReportDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IFileStorage _storage;
    private readonly IOutbox _outbox;

    public CreateWeeklySiteReportCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IFileStorage storage,
        IOutbox outbox)
    {
        _context = context;
        _currentUserService = currentUserService;
        _storage = storage;
        _outbox = outbox;
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

        var fileName = AttachmentRules.SanitiseFileName(request.FileName);

        var contentType = AttachmentRules.ResolveContentType(fileName)
            ?? throw new ConflictException("That file type is not accepted.");

        var report = new WeeklySiteReport
        {
            Id = Guid.NewGuid(),
            ProjectId = request.ProjectId,
            SubmittedByEmployeeId = employeeId.Value,
            IsoYear = request.IsoYear,
            IsoWeek = request.IsoWeek,
            Type = request.Type,
            Quantity = request.Type == WeeklyReportType.SignedHours ? request.Quantity : null,
            Note = request.Note?.Trim(),
            FileName = fileName,
            ContentType = contentType,
            SizeBytes = request.SizeBytes,
        };

        report.StorageKey =
            $"weekly-site-reports/{report.ProjectId:N}/{report.Id:N}{Path.GetExtension(fileName)}";

        await _storage.SaveAsync(report.StorageKey, request.Content, contentType, cancellationToken);

        try
        {
            _context.WeeklySiteReports.Add(report);

            var forwardTo = await _context.CompanySettings
                .Select(c => c.WeeklyReportsForwardEmail)
                .FirstOrDefaultAsync(cancellationToken);

            if (!string.IsNullOrWhiteSpace(forwardTo))
            {
                // Re-read the bytes rather than reuse `request.Content`: the
                // storage write already consumed that stream to the end.
                using var forwarded = await _storage.OpenReadAsync(report.StorageKey, cancellationToken);

                if (forwarded is not null)
                {
                    using var buffer = new MemoryStream();
                    await forwarded.CopyToAsync(buffer, cancellationToken);

                    _outbox.Enqueue(new EmailPayload(
                        forwardTo,
                        $"KW{report.IsoWeek:D2}/{report.IsoYear} — {project.Name}",
                        $"<p>{fileName}</p>" +
                        (report.Quantity is { } hours ? $"<p>{hours:0.##} h</p>" : "") +
                        (string.IsNullOrWhiteSpace(report.Note) ? "" : $"<p>{report.Note}</p>"),
                        new EmailAttachment(fileName, contentType, buffer.ToArray())));
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await _storage.DeleteAsync(report.StorageKey, CancellationToken.None);
            throw;
        }

        return await _context.WeeklySiteReports
            .AsNoTracking()
            .Where(r => r.Id == report.Id)
            .Select(WeeklySiteReportMapping.Projection)
            .FirstAsync(cancellationToken);
    }
}
