using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Materials.Commands.DeleteMaterial;

/// <summary>Soft-deletes a material (the row remains for inventory history).</summary>
public record DeleteMaterialCommand(Guid Id) : IRequest;

public class DeleteMaterialCommandHandler : IRequestHandler<DeleteMaterialCommand>
{
    private readonly IApplicationDbContext _context;

    public DeleteMaterialCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DeleteMaterialCommand request, CancellationToken cancellationToken)
    {
        var material = await _context.Materials
            .FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Material), request.Id);

        _context.Materials.Remove(material);

        await _context.SaveChangesAsync(cancellationToken);
    }
}
