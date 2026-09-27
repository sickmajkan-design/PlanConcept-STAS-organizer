using Construction.Application.Common;
using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Tools.Models;
using Construction.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Tools.Queries.GetToolById;

public record GetToolByIdQuery(Guid Id) : IRequest<ToolDto>;

public class GetToolByIdQueryHandler : IRequestHandler<GetToolByIdQuery, ToolDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetToolByIdQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ToolDto> Handle(GetToolByIdQuery request, CancellationToken cancellationToken)
    {
        var tool = await _context.Tools
            .AsNoTracking()
            .Where(t => t.Id == request.Id)
            .Select(ToolMapping.Projection)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(Tool), request.Id);

        var today = DateOnly.FromDateTime(_dateTimeProvider.UtcNow);
        var ownProjects = await SiteScope.OwnProjectIdsAsync(_context, _currentUserService, today, cancellationToken);

        // Not found rather than forbidden, as for a project: a foreman or project manager is
        // not told that a tool outside their site exists.
        if (ownProjects is not null
            && (tool.AssignedProjectId is not { } projectId || !ownProjects.Contains(projectId))
            && tool.AssignedEmployeeId != _currentUserService.EmployeeId)
        {
            throw new NotFoundException(nameof(Tool), request.Id);
        }

        return tool;
    }
}
