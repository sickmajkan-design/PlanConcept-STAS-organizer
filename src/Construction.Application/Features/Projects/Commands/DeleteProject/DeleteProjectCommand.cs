using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Projects.Commands.DeleteProject;

/// <summary>
/// Soft-deletes a project. Tools operationally assigned to the project are
/// released (assignment cleared, status back to Available when applicable).
/// Employee assignments and materials keep their rows for audit/history and
/// disappear from queries via the global soft-delete filters.
/// </summary>
public record DeleteProjectCommand(Guid Id) : IRequest;

public class DeleteProjectCommandHandler : IRequestHandler<DeleteProjectCommand>
{
    private readonly IApplicationDbContext _context;

    public DeleteProjectCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DeleteProjectCommand request, CancellationToken cancellationToken)
    {
        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Project), request.Id);

        var assignedTools = await _context.Tools
            .Where(t => t.AssignedProjectId == request.Id)
            .ToListAsync(cancellationToken);

        foreach (var tool in assignedTools)
        {
            tool.AssignedProjectId = null;

            if (tool.Status == ToolStatus.Assigned && tool.AssignedEmployeeId is null)
            {
                tool.Status = ToolStatus.Available;
            }
        }

        _context.Projects.Remove(project);

        await _context.SaveChangesAsync(cancellationToken);
    }
}
