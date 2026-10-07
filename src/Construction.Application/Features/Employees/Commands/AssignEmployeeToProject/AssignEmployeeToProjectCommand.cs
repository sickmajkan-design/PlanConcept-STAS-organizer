using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.CustomerCompanies;
using Construction.Application.Features.Employees;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Employees.Commands.AssignEmployeeToProject;

/// <summary>Posts an employee to a project for a stretch of time.</summary>
/// <remarks>
/// The dates are optional so existing callers keep working: omitting them
/// means "from today, open-ended", which is exactly what the assignment meant
/// before it had dates.
/// </remarks>
public record AssignEmployeeToProjectCommand(Guid EmployeeId, Guid ProjectId) : IRequest
{
    /// <summary>Defaults to today.</summary>
    public DateOnly? StartDate { get; init; }

    /// <summary>Null leaves the posting open-ended.</summary>
    public DateOnly? EndDate { get; init; }

    /// <summary>
    /// Which of the project's client's companies this posting is worked for. Null means "the
    /// client itself" — the only option for a client with no companies of its own. Naming one
    /// here is management's call, same door as the companies themselves: see
    /// <see cref="CustomerCompanyRules.EnsureManagement"/>. Changing an existing posting's company
    /// later is <c>SetEmployeeProjectCompanyCommand</c>, not another call to this one.
    /// </summary>
    public Guid? CustomerCompanyId { get; init; }
}

public class AssignEmployeeToProjectCommandValidator
    : AbstractValidator<AssignEmployeeToProjectCommand>
{
    public AssignEmployeeToProjectCommandValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.ProjectId).NotEmpty();

        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate!.Value)
            .WithMessage("The posting cannot end before it starts.")
            .When(x => x.StartDate is not null && x.EndDate is not null);
    }
}

public class AssignEmployeeToProjectCommandHandler : IRequestHandler<AssignEmployeeToProjectCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly INotificationService _notificationService;

    public AssignEmployeeToProjectCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        INotificationService notificationService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
        _notificationService = notificationService;
    }

    public async Task Handle(AssignEmployeeToProjectCommand request, CancellationToken cancellationToken)
    {
        var employee = await _context.Employees
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.Id == request.EmployeeId, cancellationToken)
            ?? throw new NotFoundException(nameof(Employee), request.EmployeeId);

        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == request.ProjectId, cancellationToken)
            ?? throw new NotFoundException(nameof(Project), request.ProjectId);

        if (request.CustomerCompanyId is not null)
        {
            CustomerCompanyRules.EnsureManagement(_currentUserService);
            await EnsureCompanyBelongsToProjectAsync(request.CustomerCompanyId.Value, project, cancellationToken);
        }

        var startDate = request.StartDate
            ?? DateOnly.FromDateTime(_dateTimeProvider.UtcNow);
        var endDate = request.EndDate;

        // At most one posting can ever overlap — this same check is what
        // stops a second one existing — so finding it, rather than just
        // asking whether it exists, costs nothing extra.
        var overlapping = await _context.EmployeeProjects
            .FirstOrDefaultAsync(
                ep => ep.EmployeeId == request.EmployeeId
                    && ep.ProjectId == request.ProjectId
                    && ep.StartDate <= (endDate ?? DateOnly.MaxValue)
                    && (ep.EndDate == null || ep.EndDate >= startDate),
                cancellationToken);

        if (overlapping is not null)
        {
            // A day is the smallest unit a posting has, so "removed this
            // morning, needs to go back on this afternoon" and "assigning
            // them again by mistake" look identical at this resolution —
            // both are a request that overlaps a posting already there. The
            // office almost never means "refuse this," they mean "keep them
            // on," so this extends the existing posting to cover the request
            // instead of rejecting it. A closed posting reopens; an
            // open-ended one already covers whatever was asked.
            if (startDate < overlapping.StartDate)
            {
                overlapping.StartDate = startDate;
                overlapping.AcknowledgedAt = null;
            }

            if (overlapping.EndDate != endDate)
            {
                overlapping.AcknowledgedAt = null;
            }

            overlapping.EndDate = endDate;

            await EmployeeEquipmentSync.FollowEmployeeAsync(
                _context, request.EmployeeId, request.ProjectId, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            return;
        }

        _context.EmployeeProjects.Add(new EmployeeProject
        {
            EmployeeId = request.EmployeeId,
            ProjectId = request.ProjectId,
            StartDate = startDate,
            EndDate = endDate,
            AssignedAt = _dateTimeProvider.UtcNow,
            AssignedByUserId = _currentUserService.UserId,
            CustomerCompanyId = request.CustomerCompanyId
        });

        await EmployeeEquipmentSync.FollowEmployeeAsync(
            _context, request.EmployeeId, request.ProjectId, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        await NotifyAsync(employee, project, cancellationToken);
    }

    /// <summary>
    /// A company can only be named on a posting to a site of its own client — offering another
    /// client's company would let a posting claim to be worked for a firm it has nothing to do
    /// with, and the invoicing this exists for depends on that not happening.
    /// </summary>
    private async Task EnsureCompanyBelongsToProjectAsync(
        Guid customerCompanyId,
        Project project,
        CancellationToken cancellationToken)
    {
        var companyCustomerId = await _context.CustomerCompanies
            .Where(c => c.Id == customerCompanyId)
            .Select(c => (Guid?)c.CustomerId)
            .FirstOrDefaultAsync(cancellationToken);

        if (companyCustomerId is null)
        {
            throw new NotFoundException(nameof(CustomerCompany), customerCompanyId);
        }

        if (project.CustomerId != companyCustomerId)
        {
            throw new Construction.Application.Common.Exceptions.ValidationException(
            [
                new ValidationFailure(
                    nameof(AssignEmployeeToProjectCommand.CustomerCompanyId),
                    "That company does not belong to this project's client.")
            ]);
        }
    }

    private Task NotifyAsync(Employee employee, Project project, CancellationToken cancellationToken) =>
        ProjectAssignmentNotifier.NotifyAsync(_context, _notificationService, employee, project, cancellationToken);
}
