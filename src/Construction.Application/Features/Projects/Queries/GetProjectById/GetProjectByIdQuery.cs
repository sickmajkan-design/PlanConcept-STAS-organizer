using AutoMapper;
using AutoMapper.QueryableExtensions;
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
    private readonly IMapper _mapper;

    public GetProjectByIdQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<ProjectDetailDto> Handle(
        GetProjectByIdQuery request,
        CancellationToken cancellationToken)
    {
        var project = await _context.Projects
            .AsNoTracking()
            .Where(p => p.Id == request.Id)
            .ProjectTo<ProjectDetailDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken);

        return project ?? throw new NotFoundException(nameof(Project), request.Id);
    }
}
