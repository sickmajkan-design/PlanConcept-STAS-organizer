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

    public GetToolByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ToolDto> Handle(GetToolByIdQuery request, CancellationToken cancellationToken)
    {
        var tool = await _context.Tools
            .AsNoTracking()
            .Where(t => t.Id == request.Id)
            .Select(ToolMapping.Projection)
            .FirstOrDefaultAsync(cancellationToken);

        return tool ?? throw new NotFoundException(nameof(Tool), request.Id);
    }
}
