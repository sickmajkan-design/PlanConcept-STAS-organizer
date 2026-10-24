using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.WeeklySiteReports.Queries.GetWeeklySiteReportContent;

public record WeeklySiteReportContent(Stream Content, string ContentType, string FileName);

/// <summary>One picture of a report: the one asked for, or the first when none is named.</summary>
public record GetWeeklySiteReportContentQuery(Guid Id, Guid? PageId = null) : IRequest<WeeklySiteReportContent>;

public class GetWeeklySiteReportContentQueryHandler
    : IRequestHandler<GetWeeklySiteReportContentQuery, WeeklySiteReportContent>
{
    private readonly IApplicationDbContext _context;
    private readonly IFileStorage _storage;

    public GetWeeklySiteReportContentQueryHandler(IApplicationDbContext context, IFileStorage storage)
    {
        _context = context;
        _storage = storage;
    }

    public async Task<WeeklySiteReportContent> Handle(
        GetWeeklySiteReportContentQuery request, CancellationToken cancellationToken)
    {
        var report = await _context.WeeklySiteReports
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(WeeklySiteReport), request.Id);

        var page = await _context.WeeklySiteReportPages
            .AsNoTracking()
            .Where(p => p.ReportId == request.Id && (request.PageId == null || p.Id == request.PageId))
            .OrderBy(p => p.SortOrder)
            .FirstOrDefaultAsync(cancellationToken);

        if (request.PageId is not null && page is null)
        {
            throw new NotFoundException(nameof(WeeklySiteReportPage), request.PageId.Value);
        }

        // A report with no pages row is one made before pages existed: its own file fields are the picture.
        var key = page?.StorageKey ?? report.StorageKey;
        var contentType = page?.ContentType ?? report.ContentType;
        var fileName = page?.FileName ?? report.FileName;

        var content = await _storage.OpenReadAsync(key, cancellationToken)
            ?? throw new NotFoundException(
                "The report is recorded but its file is missing from storage.");

        return new WeeklySiteReportContent(content, contentType, fileName);
    }
}
