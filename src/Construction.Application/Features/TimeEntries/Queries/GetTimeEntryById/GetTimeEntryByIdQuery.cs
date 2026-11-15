using Construction.Application.Common;
using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.TimeEntries.Models;
using Construction.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.TimeEntries.Queries.GetTimeEntryById;

public record GetTimeEntryByIdQuery(Guid Id) : IRequest<TimeEntryDto>;

public class GetTimeEntryByIdQueryHandler : IRequestHandler<GetTimeEntryByIdQuery, TimeEntryDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _clock;

    public GetTimeEntryByIdQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider clock)
    {
        _clock = clock;
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<TimeEntryDto> Handle(
        GetTimeEntryByIdQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.TimeEntries
            .AsNoTracking()
            .Where(t => t.Id == request.Id);

        // Narrow before reading rather than checking after: a 404 for someone
        // else's entry says nothing about whether it exists, while a 403 would
        // confirm it does.
        if (TimeEntryAccess.IsRestrictedToOwnEntries(_currentUserService.Role))
        {
            var ownEmployeeId = _currentUserService.EmployeeId;
            query = query.Where(t => ownEmployeeId != null && t.EmployeeId == ownEmployeeId);
        }

        // A foreman or project manager sees hours booked to the sites they are posted to, and their own.
        if (await SiteScope.OwnProjectIdsAsync(_context, _currentUserService, _clock.Today, cancellationToken) is { } own)
        {
            var self = _currentUserService.EmployeeId;
            query = query.Where(t => own.ToList().Contains(t.ProjectId!.Value) || (self != null && t.EmployeeId == self));
        }

        return await query
            .Select(TimeEntryMapping.Projection)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(TimeEntry), request.Id);
    }
}
