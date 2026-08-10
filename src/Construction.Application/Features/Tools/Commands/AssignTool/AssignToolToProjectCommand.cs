using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Tools.Models;
using Construction.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Tools.Commands.AssignTool;

/// <summary>
/// Places the tool on a project (or moves it to another project). Any
/// employee assignment is kept.
/// </summary>
public record AssignToolToProjectCommand(Guid ToolId, Guid ProjectId) : IRequest<ToolDto>;

public class AssignToolToProjectCommandHandler : IRequestHandler<AssignToolToProjectCommand, ToolDto>
{
    private readonly IApplicationDbContext _context;

    public AssignToolToProjectCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ToolDto> Handle(
        AssignToolToProjectCommand request,
        CancellationToken cancellationToken)
    {
        var tool = await _context.Tools
            .Include(t => t.AssignedEmployee)
            .FirstOrDefaultAsync(t => t.Id == request.ToolId, cancellationToken)
            ?? throw new NotFoundException(nameof(Tool), request.ToolId);

        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == request.ProjectId, cancellationToken)
            ?? throw new NotFoundException(nameof(Project), request.ProjectId);

        ToolRules.EnsureAssignable(tool);

        if (tool.AssignedProjectId == request.ProjectId)
        {
            throw new ConflictException("The tool is already assigned to this project.");
        }

        tool.AssignedProjectId = project.Id;
        tool.AssignedProject = project;
        ToolRules.RecomputeStatus(tool);

        await _context.SaveChangesAsync(cancellationToken);

        return ToolMapping.ToDto(tool);
    }
}
