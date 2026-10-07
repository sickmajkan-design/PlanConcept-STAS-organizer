using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Postings;

/// <summary>A worker confirms they have seen where they are posted.</summary>
/// <remarks>
/// Only for one's own posting: the schedule shows who has confirmed, and a confirmation somebody else
/// could give would be worth nothing. Confirming twice changes nothing.
/// </remarks>
public record AcknowledgePostingCommand(Guid PostingId) : IRequest;

public class AcknowledgePostingCommandHandler : IRequestHandler<AcknowledgePostingCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;

    public AcknowledgePostingCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IDateTimeProvider clock)
    {
        _context = context;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task Handle(AcknowledgePostingCommand request, CancellationToken cancellationToken)
    {
        var posting = await _context.EmployeeProjects
            .FirstOrDefaultAsync(ep => ep.Id == request.PostingId, cancellationToken)
            ?? throw new NotFoundException(nameof(EmployeeProject), request.PostingId);

        // Somebody else's posting is answered as not found, the way the other own-record routes do.
        if (_currentUser.EmployeeId is not { } employeeId || posting.EmployeeId != employeeId)
        {
            throw new NotFoundException(nameof(EmployeeProject), request.PostingId);
        }

        if (posting.AcknowledgedAt is null)
        {
            posting.AcknowledgedAt = _clock.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
