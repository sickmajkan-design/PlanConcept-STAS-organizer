using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.TimeEntries.Models;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.TimeEntries.Commands.CreateTimeEntry;

/// <summary>Records a shift on someone's behalf — the flat-phone case.</summary>
public record CreateTimeEntryCommand : TimeEntryCommandBase, IRequest<TimeEntryDto>;

public class CreateTimeEntryCommandValidator
    : TimeEntryCommandBaseValidator<CreateTimeEntryCommand>
{
    public CreateTimeEntryCommandValidator(IDateTimeProvider dateTimeProvider)
        : base(dateTimeProvider)
    {
    }
}

public class CreateTimeEntryCommandHandler
    : IRequestHandler<CreateTimeEntryCommand, TimeEntryDto>
{
    private readonly IApplicationDbContext _context;

    public CreateTimeEntryCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<TimeEntryDto> Handle(
        CreateTimeEntryCommand request,
        CancellationToken cancellationToken)
    {
        var employeeExists = await _context.Employees
            .AnyAsync(e => e.Id == request.EmployeeId, cancellationToken);

        if (!employeeExists)
        {
            throw new NotFoundException(nameof(Employee), request.EmployeeId);
        }

        if (request.ProjectId is { } projectId)
        {
            var projectExists = await _context.Projects
                .AnyAsync(p => p.Id == projectId, cancellationToken);

            if (!projectExists)
            {
                throw new NotFoundException(nameof(Project), projectId);
            }
        }

        var startedAt = TimeEntryRules.AsUtc(request.StartedAt);
        var endedAt = request.EndedAt is null
            ? (DateTime?)null
            : TimeEntryRules.AsUtc(request.EndedAt.Value);

        await TimeEntryRules.EnsureNoOverlapAsync(
            _context, request.EmployeeId, startedAt, endedAt, null, cancellationToken);

        var entry = new TimeEntry
        {
            EmployeeId = request.EmployeeId,
            ProjectId = request.ProjectId,
            StartedAt = startedAt,
            EndedAt = endedAt,
            BreakMinutes = request.BreakMinutes,
            WorkType = request.WorkType,
            // A shift entered by hand with both ends known is already
            // complete, so it joins the review queue rather than pretending
            // to still be running.
            Status = endedAt is null ? TimeEntryStatus.InProgress : TimeEntryStatus.Submitted,
            Note = request.Note?.Trim()
        };

        _context.TimeEntries.Add(entry);

        await _context.SaveChangesAsync(cancellationToken);

        return await _context.TimeEntries
            .AsNoTracking()
            .Where(t => t.Id == entry.Id)
            .Select(TimeEntryMapping.Projection)
            .FirstAsync(cancellationToken);
    }
}
