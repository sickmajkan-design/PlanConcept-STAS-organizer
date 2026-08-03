using AutoMapper;
using AutoMapper.QueryableExtensions;
using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.WorkItems.Models;
using Construction.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.WorkItems.Queries.GetWorkItemById;

public record GetWorkItemByIdQuery(Guid Id) : IRequest<WorkItemDto>;

public class GetWorkItemByIdQueryHandler : IRequestHandler<GetWorkItemByIdQuery, WorkItemDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;

    public GetWorkItemByIdQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IMapper mapper)
    {
        _context = context;
        _currentUserService = currentUserService;
        _mapper = mapper;
    }

    public async Task<WorkItemDto> Handle(
        GetWorkItemByIdQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.WorkItems
            .AsNoTracking()
            .Where(w => w.Id == request.Id);

        // Narrowed before reading rather than checked after: a 404 for someone
        // else's item says nothing about whether it exists.
        if (WorkItemRules.IsRestrictedToOwnItems(_currentUserService.Role))
        {
            var ownEmployeeId = _currentUserService.EmployeeId;

            query = query.Where(w =>
                ownEmployeeId != null && w.AssignedEmployeeId == ownEmployeeId);
        }

        return await query
            .ProjectTo<WorkItemDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(WorkItem), request.Id);
    }
}
