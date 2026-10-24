using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.WeeklySiteReports.Models;
using Construction.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.WeeklySiteReports.Queries.GetWeeklySiteReport;

/// <summary>One report with its pictures and workers, and what the monthly sheet holds for each of them now.</summary>
public record GetWeeklySiteReportQuery(Guid Id) : IRequest<WeeklySiteReportDetailDto>;

public record WeeklyReportPageDto
{
    public Guid Id { get; init; }

    public string FileName { get; init; } = null!;

    public string ContentType { get; init; } = null!;

    public long SizeBytes { get; init; }
}

public record WeeklyReportLineDto
{
    public Guid Id { get; init; }

    public Guid EmployeeId { get; init; }

    public string EmployeeName { get; init; } = null!;

    /// <summary>Hours for the whole week.</summary>
    public decimal Hours { get; init; }

    /// <summary>Of those, the hours that fall in the later month of a week that runs over a month's end.</summary>
    public decimal HoursSecondMonth { get; init; }

    /// <summary>What the monthly sheet holds now for the worker in the first month, or null when the cell is empty.</summary>
    public decimal? ExistingFirstMonth { get; init; }

    public decimal? ExistingSecondMonth { get; init; }
}

public record WeeklyReportMonthDto
{
    public int Year { get; init; }

    public int Month { get; init; }

    /// <summary>Whether there is a monthly sheet to enter the hours into, and what is wrong if not.</summary>
    public bool LedgerFound { get; init; }

    public string? LedgerName { get; init; }

    public bool Locked { get; init; }
}

public record WeeklySiteReportDetailDto : WeeklySiteReportDto
{
    public IReadOnlyList<WeeklyReportPageDto> Pages { get; init; } = [];

    public IReadOnlyList<WeeklyReportLineDto> Lines { get; init; } = [];

    /// <summary>The month the week begins in, and the next one when the week runs over a month's end.</summary>
    public WeeklyReportMonthDto FirstMonth { get; init; } = null!;

    public WeeklyReportMonthDto? SecondMonth { get; init; }
}

public class GetWeeklySiteReportQueryHandler : IRequestHandler<GetWeeklySiteReportQuery, WeeklySiteReportDetailDto>
{
    private readonly IApplicationDbContext _context;

    public GetWeeklySiteReportQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<WeeklySiteReportDetailDto> Handle(GetWeeklySiteReportQuery request, CancellationToken cancellationToken)
    {
        var head = await _context.WeeklySiteReports
            .AsNoTracking()
            .Where(r => r.Id == request.Id)
            .Select(WeeklySiteReportMapping.Projection)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(WeeklySiteReport), request.Id);

        var pages = await _context.WeeklySiteReportPages
            .AsNoTracking()
            .Where(p => p.ReportId == request.Id)
            .OrderBy(p => p.SortOrder)
            .Select(p => new WeeklyReportPageDto { Id = p.Id, FileName = p.FileName, ContentType = p.ContentType, SizeBytes = p.SizeBytes })
            .ToListAsync(cancellationToken);

        var lines = await _context.WeeklySiteReportLines
            .AsNoTracking()
            .Where(l => l.ReportId == request.Id)
            .OrderBy(l => l.SortOrder)
            .Select(l => new { l.Id, l.EmployeeId, Name = l.Employee.FirstName + " " + l.Employee.LastName, l.Hours, l.HoursSecondMonth })
            .ToListAsync(cancellationToken);

        var (first, second) = WeeklyReportLedger.MonthsOf(head.IsoYear, head.IsoWeek);
        var firstTarget = await WeeklyReportLedger.FindAsync(_context, head.ProjectId, head.IsoYear, head.IsoWeek, first, cancellationToken);
        var secondTarget = second is { } s
            ? await WeeklyReportLedger.FindAsync(_context, head.ProjectId, head.IsoYear, head.IsoWeek, s, cancellationToken)
            : null;

        var lineDtos = new List<WeeklyReportLineDto>();

        foreach (var line in lines)
        {
            lineDtos.Add(new WeeklyReportLineDto
            {
                Id = line.Id,
                EmployeeId = line.EmployeeId,
                EmployeeName = line.Name,
                Hours = line.Hours,
                HoursSecondMonth = line.HoursSecondMonth,
                ExistingFirstMonth = firstTarget is null
                    ? null
                    : await WeeklyReportLedger.ExistingAsync(_context, firstTarget, head.ProjectId, line.EmployeeId, cancellationToken),
                ExistingSecondMonth = secondTarget is null
                    ? null
                    : await WeeklyReportLedger.ExistingAsync(_context, secondTarget, head.ProjectId, line.EmployeeId, cancellationToken),
            });
        }

        return new WeeklySiteReportDetailDto
        {
            Id = head.Id,
            ProjectId = head.ProjectId,
            ProjectName = head.ProjectName,
            SubmittedByEmployeeId = head.SubmittedByEmployeeId,
            SubmittedByEmployeeName = head.SubmittedByEmployeeName,
            IsoYear = head.IsoYear,
            IsoWeek = head.IsoWeek,
            Type = head.Type,
            Quantity = head.Quantity,
            Note = head.Note,
            FileName = head.FileName,
            PageCount = head.PageCount,
            LineCount = head.LineCount,
            Status = head.Status,
            ProcessedAt = head.ProcessedAt,
            ProcessedByEmail = head.ProcessedByEmail,
            ReviewNote = head.ReviewNote,
            CreatedAt = head.CreatedAt,
            Pages = pages,
            Lines = lineDtos,
            FirstMonth = ToDto(first, firstTarget),
            SecondMonth = second is { } sm ? ToDto(sm, secondTarget) : null,
        };
    }

    private static WeeklyReportMonthDto ToDto(WeeklyReportLedger.YearMonth month, WeeklyReportLedger.Target? target) =>
        new()
        {
            Year = month.Year,
            Month = month.Number,
            LedgerFound = target?.WeekColumnId is not null,
            LedgerName = target?.LedgerName,
            Locked = target?.Locked ?? false,
        };
}
