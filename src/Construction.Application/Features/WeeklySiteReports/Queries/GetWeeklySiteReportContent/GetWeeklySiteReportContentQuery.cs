using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.WeeklySiteReports.Queries.GetWeeklySiteReportContent;

public record WeeklySiteReportContent(Stream Content, string ContentType, string FileName);

public record GetWeeklySiteReportContentQuery(Guid Id) : IRequest<WeeklySiteReportContent>;

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

        var content = await _storage.OpenReadAsync(report.StorageKey, cancellationToken)
            ?? throw new NotFoundException(
                "The report is recorded but its file is missing from storage.");

        return new WeeklySiteReportContent(content, report.ContentType, report.FileName);
    }
}
