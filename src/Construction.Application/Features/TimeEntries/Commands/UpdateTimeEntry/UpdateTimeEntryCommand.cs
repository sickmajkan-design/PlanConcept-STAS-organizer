using AutoMapper;
using AutoMapper.QueryableExtensions;
using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.TimeEntries.Models;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.TimeEntries.Commands.UpdateTimeEntry;

/// <summary>Corrects a recorded shift — including closing one left running.</summary>
public record UpdateTimeEntryCommand : TimeEntryCommandBase, IRequest<TimeEntryDto>
{
    public Guid Id { get; init; }
}

public class UpdateTimeEntryCommandValidator
    : TimeEntryCommandBaseValidator<UpdateTimeEntryCommand>
{
    public UpdateTimeEntryCommandValidator(IDateTimeProvider dateTimeProvider)
        : base(dateTimeProvider)
    {
    }
}

public class UpdateTimeEntryCommandHandler
    : IRequestHandler<UpdateTimeEntryCommand, TimeEntryDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public UpdateTimeEntryCommandHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<TimeEntryDto> Handle(
        UpdateTimeEntryCommand request,
        CancellationToken cancellationToken)
    {
        var entry = await _context.TimeEntries
            .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(TimeEntry), request.Id);

        TimeEntryRules.EnsureEditable(entry);

        if (entry.EmployeeId != request.EmployeeId)
        {
            var employeeExists = await _context.Employees
                .AnyAsync(e => e.Id == request.EmployeeId, cancellationToken);

            if (!employeeExists)
            {
                throw new NotFoundException(nameof(Employee), request.EmployeeId);
            }
        }

        if (request.ProjectId is { } projectId && projectId != entry.ProjectId)
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
            _context, request.EmployeeId, startedAt, endedAt, entry.Id, cancellationToken);

        entry.EmployeeId = request.EmployeeId;
        entry.ProjectId = request.ProjectId;
        entry.StartedAt = startedAt;
        entry.EndedAt = endedAt;
        entry.BreakMinutes = request.BreakMinutes;
        entry.WorkType = request.WorkType;
        entry.Note = request.Note?.Trim();

        // A correction answers whatever the reviewer sent it back for, so the
        // entry returns to the queue and the old rejection note goes with it.
        // Leaving the note behind would show a reviewer a complaint that no
        // longer describes the row in front of them.
        entry.Status = endedAt is null ? TimeEntryStatus.InProgress : TimeEntryStatus.Submitted;
        entry.ReviewedByUserId = null;
        entry.ReviewedAt = null;
        entry.ReviewNote = null;

        await _context.SaveChangesAsync(cancellationToken);

        return await _context.TimeEntries
            .AsNoTracking()
            .Where(t => t.Id == entry.Id)
            .ProjectTo<TimeEntryDto>(_mapper.ConfigurationProvider)
            .FirstAsync(cancellationToken);
    }
}
