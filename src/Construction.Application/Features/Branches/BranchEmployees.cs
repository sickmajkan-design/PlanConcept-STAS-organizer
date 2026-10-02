using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Branches;

/// <summary>One stretch of employment of a person in a business unit.</summary>
public class EmployeeBranchPeriodDto
{
    public Guid Id { get; init; }

    public Guid BranchId { get; init; }

    public string BranchName { get; init; } = null!;

    public string BranchColor { get; init; } = null!;

    public DateOnly StartDate { get; init; }

    /// <summary>Null while the employment in the unit is ongoing.</summary>
    public DateOnly? EndDate { get; init; }
}

/// <summary>
/// How an employee moves between business units, so the dated history can never overlap or leave
/// a hole by accident. Every path that changes it — one person, many people, a new hire — goes
/// through <see cref="MoveAsync"/>.
/// </summary>
public static class EmployeeBranchRules
{
    /// <summary>
    /// Puts <paramref name="employeeId"/> in <paramref name="branchId"/> from <paramref name="from"/>
    /// (or in no unit when it is null), closing the stretch they were in the day before.
    /// </summary>
    /// <returns>False when nothing changed: they were already there.</returns>
    /// <remarks>
    /// Refused rather than guessed when the date does not fit the history: moving someone "from
    /// January" when they were already moved in February would rewrite which unit paid for the
    /// hours in between. Fixing history is done by removing the wrong period first.
    /// </remarks>
    public static async Task<bool> MoveAsync(
        IApplicationDbContext context,
        Guid employeeId,
        Guid? branchId,
        DateOnly from,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        if (from > today)
        {
            throw new ConflictException("The date cannot be in the future.");
        }

        var periods = await context.EmployeeBranches
            .Where(p => p.EmployeeId == employeeId)
            .ToListAsync(cancellationToken);

        var current = periods.FirstOrDefault(p => p.EndDate is null);

        if (current?.BranchId == branchId)
        {
            return false;
        }

        // Anything that ended on or after the date overlaps the stretch about to open.
        if (periods.Any(p => p != current && (p.EndDate ?? DateOnly.MaxValue) >= from))
        {
            throw new ConflictException(
                "There is already employment in a unit on or after that date. Remove that period first, or choose a later date.");
        }

        if (current is not null)
        {
            if (from < current.StartDate)
            {
                throw new ConflictException(
                    $"They have been in their current unit since {current.StartDate:dd.MM.yyyy.}; choose a later date.");
            }

            if (from == current.StartDate)
            {
                // Same day it began: that was a correction, not a move, so there is no day to close it on.
                if (branchId is { } corrected)
                {
                    current.BranchId = corrected;
                }
                else
                {
                    context.EmployeeBranches.Remove(current);
                }

                return true;
            }

            current.EndDate = from.AddDays(-1);
        }

        if (branchId is { } id)
        {
            context.EmployeeBranches.Add(new EmployeeBranch
            {
                EmployeeId = employeeId,
                BranchId = id,
                StartDate = from,
            });
        }

        return true;
    }
}

/// <summary>Moves an employee into a business unit (or out of every unit) from a date.</summary>
public record SetEmployeeBranchCommand : IRequest<IReadOnlyList<EmployeeBranchPeriodDto>>
{
    public Guid EmployeeId { get; init; }

    /// <summary>Null means they are in no unit from the date.</summary>
    public Guid? BranchId { get; init; }

    /// <summary>Defaults to today.</summary>
    public DateOnly? From { get; init; }
}

public class SetEmployeeBranchCommandValidator : AbstractValidator<SetEmployeeBranchCommand>
{
    public SetEmployeeBranchCommandValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
    }
}

public class SetEmployeeBranchCommandHandler
    : IRequestHandler<SetEmployeeBranchCommand, IReadOnlyList<EmployeeBranchPeriodDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly INotificationService _notifications;

    public SetEmployeeBranchCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        INotificationService notifications)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
        _notifications = notifications;
    }

    public async Task<IReadOnlyList<EmployeeBranchPeriodDto>> Handle(
        SetEmployeeBranchCommand request,
        CancellationToken cancellationToken)
    {
        BranchRules.EnsureManagement(_currentUserService);

        if (!await _context.Employees.AnyAsync(e => e.Id == request.EmployeeId, cancellationToken))
        {
            throw new NotFoundException(nameof(Employee), request.EmployeeId);
        }

        var branch = await BranchLookup.LoadAsync(_context, request.BranchId, cancellationToken);

        var today = DateOnly.FromDateTime(_dateTimeProvider.UtcNow);
        var from = request.From ?? today;

        var changed = await EmployeeBranchRules.MoveAsync(
            _context, request.EmployeeId, request.BranchId, from, today, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        if (changed)
        {
            await EmployeeBranchNotifier.NotifyAsync(
                _context, _notifications, [(request.EmployeeId, from)], branch?.Name, cancellationToken);
        }

        return await EmployeeBranchHistory.LoadAsync(_context, request.EmployeeId, cancellationToken);
    }
}

public static class EmployeeBranchHistory
{
    /// <summary>The employee's units, most recent first.</summary>
    public static async Task<IReadOnlyList<EmployeeBranchPeriodDto>> LoadAsync(
        IApplicationDbContext context,
        Guid employeeId,
        CancellationToken cancellationToken) =>
        await context.EmployeeBranches
            .AsNoTracking()
            .Where(p => p.EmployeeId == employeeId)
            .OrderByDescending(p => p.StartDate)
            .Select(p => new EmployeeBranchPeriodDto
            {
                Id = p.Id,
                BranchId = p.BranchId,
                BranchName = p.Branch.Name,
                BranchColor = p.Branch.Color,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
            })
            .ToListAsync(cancellationToken);
}

/// <summary>Removes one wrong period from an employee's history.</summary>
public record RemoveEmployeeBranchPeriodCommand(Guid EmployeeId, Guid PeriodId)
    : IRequest<IReadOnlyList<EmployeeBranchPeriodDto>>;

public class RemoveEmployeeBranchPeriodCommandHandler
    : IRequestHandler<RemoveEmployeeBranchPeriodCommand, IReadOnlyList<EmployeeBranchPeriodDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public RemoveEmployeeBranchPeriodCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<EmployeeBranchPeriodDto>> Handle(
        RemoveEmployeeBranchPeriodCommand request,
        CancellationToken cancellationToken)
    {
        BranchRules.EnsureManagement(_currentUserService);

        var period = await _context.EmployeeBranches
            .FirstOrDefaultAsync(p => p.Id == request.PeriodId && p.EmployeeId == request.EmployeeId, cancellationToken)
            ?? throw new NotFoundException(nameof(EmployeeBranch), request.PeriodId);

        _context.EmployeeBranches.Remove(period);
        await _context.SaveChangesAsync(cancellationToken);

        return await EmployeeBranchHistory.LoadAsync(_context, request.EmployeeId, cancellationToken);
    }
}

/// <summary>Moves several employees into one business unit from a date, leaving everyone else as they are.</summary>
public record AssignEmployeesToBranchCommand : IRequest<BranchDto>
{
    public Guid Id { get; init; }

    public IReadOnlyList<Guid> EmployeeIds { get; init; } = [];

    /// <summary>Defaults to today.</summary>
    public DateOnly? From { get; init; }

    /// <summary>
    /// For an employee who has never been in any unit, start from their employment date instead of
    /// <see cref="From"/>, so everything they worked before this was set up belongs to the unit
    /// too. Those who already have a history use <see cref="From"/>.
    /// </summary>
    public bool BackdateNewcomers { get; init; }
}

public class AssignEmployeesToBranchCommandValidator : AbstractValidator<AssignEmployeesToBranchCommand>
{
    public AssignEmployeesToBranchCommandValidator()
    {
        RuleFor(x => x.EmployeeIds).NotEmpty().WithMessage("Choose at least one employee.");
        RuleFor(x => x.EmployeeIds.Count).LessThanOrEqualTo(2000);
    }
}

public class AssignEmployeesToBranchCommandHandler : IRequestHandler<AssignEmployeesToBranchCommand, BranchDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly INotificationService _notifications;

    public AssignEmployeesToBranchCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        INotificationService notifications)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
        _notifications = notifications;
    }

    public async Task<BranchDto> Handle(AssignEmployeesToBranchCommand request, CancellationToken cancellationToken)
    {
        BranchRules.EnsureManagement(_currentUserService);

        var branch = await _context.Branches.FirstOrDefaultAsync(b => b.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Branch), request.Id);

        var ids = request.EmployeeIds.Distinct().ToList();

        var existing = await _context.Employees
            .Where(e => ids.Contains(e.Id))
            .Select(e => new { e.Id, e.EmploymentDate })
            .ToListAsync(cancellationToken);

        var missing = ids.Except(existing.Select(e => e.Id)).FirstOrDefault();

        if (missing != default)
        {
            throw new NotFoundException(nameof(Employee), missing);
        }

        var today = DateOnly.FromDateTime(_dateTimeProvider.UtcNow);
        var from = request.From ?? today;

        // All or nothing: one person whose history does not fit the date fails the whole request,
        // so a half-applied move cannot leave some hours in the old unit and some in the new.
        var withHistory = request.BackdateNewcomers
            ? (await _context.EmployeeBranches
                .Where(p => ids.Contains(p.EmployeeId))
                .Select(p => p.EmployeeId)
                .Distinct()
                .ToListAsync(cancellationToken)).ToHashSet()
            : [];

        var moved = new List<(Guid EmployeeId, DateOnly From)>();

        foreach (var employee in existing)
        {
            var start = request.BackdateNewcomers && !withHistory.Contains(employee.Id)
                ? (employee.EmploymentDate < today ? employee.EmploymentDate : today)
                : from;

            if (await EmployeeBranchRules.MoveAsync(_context, employee.Id, branch.Id, start, today, cancellationToken))
            {
                moved.Add((employee.Id, start));
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Only people who actually moved are told, and only after everything was saved.
        await EmployeeBranchNotifier.NotifyAsync(_context, _notifications, moved, branch.Name, cancellationToken);

        return await BranchView.ForCallerAsync(_context, _currentUserService, branch, cancellationToken);
    }
}
