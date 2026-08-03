using AutoMapper;
using AutoMapper.QueryableExtensions;
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
    private readonly IMapper _mapper;

    public GetTimeEntryByIdQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IMapper mapper)
    {
        _context = context;
        _currentUserService = currentUserService;
        _mapper = mapper;
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

        return await query
            .ProjectTo<TimeEntryDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(TimeEntry), request.Id);
    }
}
