using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Employees.Commands.AssignEmployeeToProject;

public record AssignEmployeeToProjectCommand(Guid EmployeeId, Guid ProjectId) : IRequest;

public class AssignEmployeeToProjectCommandHandler : IRequestHandler<AssignEmployeeToProjectCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AssignEmployeeToProjectCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task Handle(AssignEmployeeToProjectCommand request, CancellationToken cancellationToken)
    {
        var employeeExists = await _context.Employees
            .AnyAsync(e => e.Id == request.EmployeeId, cancellationToken);

        if (!employeeExists)
        {
            throw new NotFoundException(nameof(Employee), request.EmployeeId);
        }

        var projectExists = await _context.Projects
            .AnyAsync(p => p.Id == request.ProjectId, cancellationToken);

        if (!projectExists)
        {
            throw new NotFoundException(nameof(Project), request.ProjectId);
        }

        var alreadyAssigned = await _context.EmployeeProjects
            .AnyAsync(ep => ep.EmployeeId == request.EmployeeId && ep.ProjectId == request.ProjectId,
                cancellationToken);

        if (alreadyAssigned)
        {
            throw new ConflictException("The employee is already assigned to this project.");
        }

        _context.EmployeeProjects.Add(new EmployeeProject
        {
            EmployeeId = request.EmployeeId,
            ProjectId = request.ProjectId,
            AssignedAt = _dateTimeProvider.UtcNow,
            AssignedByUserId = _currentUserService.UserId
        });

        await _context.SaveChangesAsync(cancellationToken);
    }
}
