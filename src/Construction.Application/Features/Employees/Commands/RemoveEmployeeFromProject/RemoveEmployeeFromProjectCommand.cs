using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Employees.Commands.RemoveEmployeeFromProject;

public record RemoveEmployeeFromProjectCommand(Guid EmployeeId, Guid ProjectId) : IRequest;

public class RemoveEmployeeFromProjectCommandHandler : IRequestHandler<RemoveEmployeeFromProjectCommand>
{
    private readonly IApplicationDbContext _context;

    public RemoveEmployeeFromProjectCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(
        RemoveEmployeeFromProjectCommand request,
        CancellationToken cancellationToken)
    {
        var assignment = await _context.EmployeeProjects
            .FirstOrDefaultAsync(
                ep => ep.EmployeeId == request.EmployeeId && ep.ProjectId == request.ProjectId,
                cancellationToken)
            ?? throw new NotFoundException(
                $"Employee '{request.EmployeeId}' is not assigned to project '{request.ProjectId}'.");

        _context.EmployeeProjects.Remove(assignment);

        await _context.SaveChangesAsync(cancellationToken);
    }
}
