using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.SignedTimesheets.Commands.GetOrCreateSignedTimesheet;

/// <summary>
/// Resolves a (project, calendar week) pair to a row id, creating the row the first time
/// that pair is used. The panel calls this right before uploading a scan: an attachment
/// needs an owner id, and a signed timesheet's owner id is not known until somebody
/// actually files one for that week.
/// </summary>
public record GetOrCreateSignedTimesheetCommand : IRequest<Guid>
{
    public Guid ProjectId { get; init; }

    /// <summary>The ISO week's own year — see <see cref="Ledgers.Models.LedgerTemplates.MonthWeeks"/>.</summary>
    public int Year { get; init; }

    public int IsoWeek { get; init; }
}

public class GetOrCreateSignedTimesheetCommandValidator
    : AbstractValidator<GetOrCreateSignedTimesheetCommand>
{
    public GetOrCreateSignedTimesheetCommandValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.Year).InclusiveBetween(2000, 2100);
        RuleFor(x => x.IsoWeek).InclusiveBetween(1, 53);
    }
}

public class GetOrCreateSignedTimesheetCommandHandler
    : IRequestHandler<GetOrCreateSignedTimesheetCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public GetOrCreateSignedTimesheetCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(
        GetOrCreateSignedTimesheetCommand request,
        CancellationToken cancellationToken)
    {
        var existingId = await _context.SignedTimesheets
            .Where(s => s.ProjectId == request.ProjectId
                && s.Year == request.Year
                && s.IsoWeek == request.IsoWeek)
            .Select(s => s.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingId != Guid.Empty)
        {
            return existingId;
        }

        if (!await _context.Projects.AnyAsync(p => p.Id == request.ProjectId, cancellationToken))
        {
            throw new NotFoundException(nameof(Project), request.ProjectId);
        }

        var signedTimesheet = new SignedTimesheet
        {
            ProjectId = request.ProjectId,
            Year = request.Year,
            IsoWeek = request.IsoWeek,
        };

        _context.SignedTimesheets.Add(signedTimesheet);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Lost a race with another request creating the same week's row — the unique
            // index is the real guard, this is just avoiding a needless 500 for it.
            var raced = await _context.SignedTimesheets
                .Where(s => s.ProjectId == request.ProjectId
                    && s.Year == request.Year
                    && s.IsoWeek == request.IsoWeek)
                .Select(s => s.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (raced != Guid.Empty)
            {
                return raced;
            }

            throw;
        }

        return signedTimesheet.Id;
    }
}
