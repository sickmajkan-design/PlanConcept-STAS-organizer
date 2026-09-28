using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.VehicleTolls.Commands.DeleteVehicleToll;

/// <summary>
/// Removes a toll obligation entirely. Hard-deleted — see
/// <see cref="VehicleToll"/>'s remarks for why. The payment history in
/// <see cref="VehicleTollPayment"/> is cascade-deleted with it; unlike an
/// <see cref="Attachment"/>'s retention rule, a toll payment carries no
/// legal record-keeping requirement of its own once the toll it belongs to
/// is gone.
/// </summary>
public record DeleteVehicleTollCommand(Guid VehicleTollId) : IRequest;

public class DeleteVehicleTollCommandHandler : IRequestHandler<DeleteVehicleTollCommand>
{
    private readonly IApplicationDbContext _context;

    public DeleteVehicleTollCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DeleteVehicleTollCommand request, CancellationToken cancellationToken)
    {
        var toll = await _context.VehicleTolls
            .FirstOrDefaultAsync(t => t.Id == request.VehicleTollId, cancellationToken)
            ?? throw new NotFoundException(nameof(VehicleToll), request.VehicleTollId);

        _context.VehicleTolls.Remove(toll);

        await _context.SaveChangesAsync(cancellationToken);
    }
}
