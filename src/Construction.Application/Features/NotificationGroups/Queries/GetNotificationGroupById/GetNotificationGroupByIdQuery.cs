using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.NotificationGroups.Models;
using Construction.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.NotificationGroups.Queries.GetNotificationGroupById;

public record GetNotificationGroupByIdQuery(Guid Id) : IRequest<NotificationGroupDetailDto>;

public class GetNotificationGroupByIdQueryHandler
    : IRequestHandler<GetNotificationGroupByIdQuery, NotificationGroupDetailDto>
{
    private readonly IApplicationDbContext _context;

    public GetNotificationGroupByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<NotificationGroupDetailDto> Handle(
        GetNotificationGroupByIdQuery request,
        CancellationToken cancellationToken)
    {
        return await _context.NotificationGroups
            .AsNoTracking()
            .Where(g => g.Id == request.Id)
            .Select(NotificationGroupMapping.DetailProjection)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(NotificationGroup), request.Id);
    }
}
