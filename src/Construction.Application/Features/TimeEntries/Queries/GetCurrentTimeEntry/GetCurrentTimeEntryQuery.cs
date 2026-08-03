using AutoMapper;
using AutoMapper.QueryableExtensions;
using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.TimeEntries.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.TimeEntries.Queries.GetCurrentTimeEntry;

/// <summary>
/// The signed-in employee's running shift, or null when they are not on one.
/// </summary>
/// <remarks>
/// Null is the ordinary answer, not an error: the app asks this on every
/// launch to decide whether to show "clock in" or "clock out", and being off
/// shift is the normal state for most of the day.
/// </remarks>
public record GetCurrentTimeEntryQuery : IRequest<TimeEntryDto?>;

public class GetCurrentTimeEntryQueryHandler
    : IRequestHandler<GetCurrentTimeEntryQuery, TimeEntryDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;

    public GetCurrentTimeEntryQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IMapper mapper)
    {
        _context = context;
        _currentUserService = currentUserService;
        _mapper = mapper;
    }

    public async Task<TimeEntryDto?> Handle(
        GetCurrentTimeEntryQuery request,
        CancellationToken cancellationToken)
    {
        var employeeId = _currentUserService.EmployeeId
            ?? throw new ForbiddenAccessException(
                "Only accounts linked to an employee can record work time.");

        return await _context.TimeEntries
            .AsNoTracking()
            .Where(t => t.EmployeeId == employeeId && t.EndedAt == null)
            .ProjectTo<TimeEntryDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
