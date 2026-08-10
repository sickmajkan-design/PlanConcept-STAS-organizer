using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.TimeEntries.Models;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.TimeEntries.Commands.ReviewTimeEntry;

/// <summary>
/// Signs a shift off, or sends it back with a reason.
/// </summary>
/// <remarks>
/// One command for both outcomes because they share every rule that matters —
/// who may do it, what state the entry has to be in, and that the decision is
/// recorded against a person. Splitting them would have duplicated all three.
/// </remarks>
public record ReviewTimeEntryCommand : IRequest<TimeEntryDto>
{
    public Guid Id { get; init; }

    public bool Approve { get; init; }

    /// <summary>Required when sending an entry back, so the worker knows what to fix.</summary>
    public string? Note { get; init; }
}

public class ReviewTimeEntryCommandValidator : AbstractValidator<ReviewTimeEntryCommand>
{
    public ReviewTimeEntryCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.Note)
            .NotEmpty().WithMessage("A reason is required when sending an entry back.")
            .MaximumLength(1000)
            .When(x => !x.Approve);

        RuleFor(x => x.Note).MaximumLength(1000);
    }
}

public class ReviewTimeEntryCommandHandler
    : IRequestHandler<ReviewTimeEntryCommand, TimeEntryDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ReviewTimeEntryCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<TimeEntryDto> Handle(
        ReviewTimeEntryCommand request,
        CancellationToken cancellationToken)
    {
        var reviewerId = _currentUserService.UserId
            ?? throw new ForbiddenAccessException("No signed-in user to record the review against.");

        var entry = await _context.TimeEntries
            .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(TimeEntry), request.Id);

        // Nobody signs off their own hours, whatever their role. This is the
        // one control that makes the approval mean anything: without it, a
        // supervisor's own timesheet approves itself.
        if (_currentUserService.EmployeeId is { } reviewerEmployeeId
            && reviewerEmployeeId == entry.EmployeeId)
        {
            throw new ForbiddenAccessException("You cannot review your own hours.");
        }

        if (entry.EndedAt is null)
        {
            throw new ConflictException("This shift is still running and cannot be reviewed yet.");
        }

        if (request.Approve && entry.Status == TimeEntryStatus.Approved)
        {
            throw new ConflictException("This entry is already approved.");
        }

        entry.Status = request.Approve ? TimeEntryStatus.Approved : TimeEntryStatus.Rejected;
        entry.ReviewedByUserId = reviewerId;
        entry.ReviewedAt = _dateTimeProvider.UtcNow;
        // An approval note would sit on the row looking like an objection.
        entry.ReviewNote = request.Approve ? null : request.Note!.Trim();

        await _context.SaveChangesAsync(cancellationToken);

        return await _context.TimeEntries
            .AsNoTracking()
            .Where(t => t.Id == entry.Id)
            .Select(TimeEntryMapping.Projection)
            .FirstAsync(cancellationToken);
    }
}
