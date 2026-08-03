using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.TimeEntries.Commands.DeleteTimeEntry;

/// <summary>Soft-deletes a time entry.</summary>
/// <remarks>
/// Approved entries are refused rather than deleted. Once hours have been
/// signed off they are what someone is paid against, and a row that quietly
/// disappears from a timesheet is the one thing a worker cannot argue with.
/// Rejecting it first leaves a reviewer and a reason on the record.
/// </remarks>
public record DeleteTimeEntryCommand(Guid Id) : IRequest;

public class DeleteTimeEntryCommandHandler : IRequestHandler<DeleteTimeEntryCommand>
{
    private readonly IApplicationDbContext _context;

    public DeleteTimeEntryCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DeleteTimeEntryCommand request, CancellationToken cancellationToken)
    {
        var entry = await _context.TimeEntries
            .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(TimeEntry), request.Id);

        TimeEntryRules.EnsureEditable(entry);

        _context.TimeEntries.Remove(entry);

        await _context.SaveChangesAsync(cancellationToken);
    }
}
