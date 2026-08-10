using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Materials.Models;
using Construction.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Materials.Queries.GetMaterialById;

public record GetMaterialByIdQuery(Guid Id) : IRequest<MaterialDto>;

public class GetMaterialByIdQueryHandler : IRequestHandler<GetMaterialByIdQuery, MaterialDto>
{
    private readonly IApplicationDbContext _context;

    public GetMaterialByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<MaterialDto> Handle(
        GetMaterialByIdQuery request,
        CancellationToken cancellationToken)
    {
        var material = await _context.Materials
            .AsNoTracking()
            .Where(m => m.Id == request.Id)
            .Select(MaterialMapping.Projection)
            .FirstOrDefaultAsync(cancellationToken);

        return material ?? throw new NotFoundException(nameof(Material), request.Id);
    }
}
