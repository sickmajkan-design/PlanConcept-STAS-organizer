using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Projects.Models;
using Construction.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Projects.Queries.GetProjectById;

public record GetProjectByIdQuery(Guid Id) : IRequest<ProjectDetailDto>;

public class GetProjectByIdQueryHandler : IRequestHandler<GetProjectByIdQuery, ProjectDetailDto>
{
    private readonly IApplicationDbContext _context;

    public GetProjectByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ProjectDetailDto> Handle(
        GetProjectByIdQuery request,
        CancellationToken cancellationToken)
    {
        var project = await _context.Projects
            .AsNoTracking()
            .Where(p => p.Id == request.Id)
            .Select(ProjectDetailMapping.Projection)
            .FirstOrDefaultAsync(cancellationToken);

        return project ?? throw new NotFoundException(nameof(Project), request.Id);
    }
}
