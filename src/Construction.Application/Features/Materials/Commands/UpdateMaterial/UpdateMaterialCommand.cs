using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Materials.Models;
using Construction.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Materials.Commands.UpdateMaterial;

public record UpdateMaterialCommand : MaterialCommandBase, IRequest<MaterialDto>
{
    /// <summary>Set by the API layer from the route, never from the request body.</summary>
    public Guid Id { get; init; }
}

public class UpdateMaterialCommandValidator : MaterialCommandBaseValidator<UpdateMaterialCommand>;

public class UpdateMaterialCommandHandler : IRequestHandler<UpdateMaterialCommand, MaterialDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly INotificationService _notifications;

    public UpdateMaterialCommandHandler(
        IApplicationDbContext context,
        IDateTimeProvider dateTimeProvider,
        INotificationService notifications)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
        _notifications = notifications;
    }

    public async Task<MaterialDto> Handle(
        UpdateMaterialCommand request,
        CancellationToken cancellationToken)
    {
        var material = await _context.Materials
            .Include(m => m.Project)
            .FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Material), request.Id);

        if (request.ProjectId is { } projectId && projectId != material.ProjectId)
        {
            var projectExists = await _context.Projects
                .AnyAsync(p => p.Id == projectId, cancellationToken);

            if (!projectExists)
            {
                throw new NotFoundException(nameof(Project), projectId);
            }
        }

        var previousQuantity = material.Quantity;

        if (material.Quantity != request.Quantity)
        {
            material.LastUpdated = _dateTimeProvider.UtcNow;
        }

        material.Name = request.Name.Trim();
        material.Unit = request.Unit.Trim();
        material.Quantity = request.Quantity;
        material.Warehouse = request.Warehouse?.Trim();
        material.UnitPrice = request.UnitPrice;
        material.MinimumQuantity = request.MinimumQuantity;
        material.ProjectId = request.ProjectId;

        await _context.SaveChangesAsync(cancellationToken);

        await Construction.Application.Features.Materials.LowStockNotifier.NotifyIfCrossedAsync(
            _context, _notifications, material.Id, request.Quantity - previousQuantity, cancellationToken);

        // Reload through a projection so a changed ProjectId comes back with its name.
        return await _context.Materials
            .AsNoTracking()
            .Where(m => m.Id == material.Id)
            .Select(MaterialMapping.Projection)
            .FirstAsync(cancellationToken);
    }
}
