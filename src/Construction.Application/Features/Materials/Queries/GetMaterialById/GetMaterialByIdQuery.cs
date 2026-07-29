using AutoMapper;
using AutoMapper.QueryableExtensions;
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
    private readonly IMapper _mapper;

    public GetMaterialByIdQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<MaterialDto> Handle(
        GetMaterialByIdQuery request,
        CancellationToken cancellationToken)
    {
        var material = await _context.Materials
            .AsNoTracking()
            .Where(m => m.Id == request.Id)
            .ProjectTo<MaterialDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken);

        return material ?? throw new NotFoundException(nameof(Material), request.Id);
    }
}
