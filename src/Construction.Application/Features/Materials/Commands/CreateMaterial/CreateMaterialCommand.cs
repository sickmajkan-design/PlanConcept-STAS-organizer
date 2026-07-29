using AutoMapper;
using AutoMapper.QueryableExtensions;
using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Materials.Models;
using Construction.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Materials.Commands.CreateMaterial;

public record CreateMaterialCommand : MaterialCommandBase, IRequest<MaterialDto>;

public class CreateMaterialCommandValidator : MaterialCommandBaseValidator<CreateMaterialCommand>;

public class CreateMaterialCommandHandler : IRequestHandler<CreateMaterialCommand, MaterialDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IMapper _mapper;

    public CreateMaterialCommandHandler(
        IApplicationDbContext context,
        IDateTimeProvider dateTimeProvider,
        IMapper mapper)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
        _mapper = mapper;
    }

    public async Task<MaterialDto> Handle(
        CreateMaterialCommand request,
        CancellationToken cancellationToken)
    {
        if (request.ProjectId is { } projectId)
        {
            var projectExists = await _context.Projects
                .AnyAsync(p => p.Id == projectId, cancellationToken);

            if (!projectExists)
            {
                throw new NotFoundException(nameof(Project), projectId);
            }
        }

        var material = new Material
        {
            Name = request.Name.Trim(),
            Unit = request.Unit.Trim(),
            Quantity = request.Quantity,
            Warehouse = request.Warehouse?.Trim(),
            ProjectId = request.ProjectId,
            LastUpdated = _dateTimeProvider.UtcNow
        };

        _context.Materials.Add(material);

        await _context.SaveChangesAsync(cancellationToken);

        // Reload through a projection so the project name is populated.
        return await _context.Materials
            .AsNoTracking()
            .Where(m => m.Id == material.Id)
            .ProjectTo<MaterialDto>(_mapper.ConfigurationProvider)
            .FirstAsync(cancellationToken);
    }
}
