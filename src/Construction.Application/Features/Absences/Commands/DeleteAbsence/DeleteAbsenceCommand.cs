using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Absences.Commands.DeleteAbsence;

/// <summary>Withdraws or removes an absence.</summary>
/// <remarks>
/// A person may withdraw their own request while it is still unanswered — that
/// is cancelling, and it leaves the row. Removing something already granted is
/// a supervisor's act, because somebody has planned around it.
/// </remarks>
public record DeleteAbsenceCommand(Guid Id) : IRequest;

public class DeleteAbsenceCommandHandler : IRequestHandler<DeleteAbsenceCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public DeleteAbsenceCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task Handle(DeleteAbsenceCommand request, CancellationToken cancellationToken)
    {
        var absence = await _context.Absences
            .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Absence), request.Id);

        var isOwn = _currentUserService.EmployeeId is { } employeeId
            && employeeId == absence.EmployeeId;

        var canReview = AbsenceRules.CanReview(_currentUserService.Role);

        if (!canReview && !isOwn)
        {
            throw new ForbiddenAccessException("You may not remove this absence.");
        }

        if (isOwn && !canReview)
        {
            if (absence.Status != AbsenceStatus.Requested)
            {
                throw new ConflictException(
                    "This has already been answered. Ask a supervisor to change it.");
            }

            // Withdrawn rather than removed: the row records that it was asked
            // for and taken back, which is what a supervisor who half-planned
            // around it needs to see.
            absence.Status = AbsenceStatus.Cancelled;
            await _context.SaveChangesAsync(cancellationToken);
            return;
        }

        _context.Absences.Remove(absence);

        await _context.SaveChangesAsync(cancellationToken);
    }
}
