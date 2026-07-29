using AutoMapper;
using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Tools.Models;
using Construction.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Tools.Commands.UnassignTool;

public enum ToolAssignmentTarget
{
    Employee = 1,
    Project = 2
}

/// <summary>
/// Releases one side of the tool's assignment (employee or project). The
/// status returns to Available once neither side holds the tool.
/// </summary>
public record UnassignToolCommand(Guid ToolId, ToolAssignmentTarget Target) : IRequest<ToolDto>;

public class UnassignToolCommandHandler : IRequestHandler<UnassignToolCommand, ToolDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public UnassignToolCommandHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<ToolDto> Handle(UnassignToolCommand request, CancellationToken cancellationToken)
    {
        var tool = await _context.Tools
            .Include(t => t.AssignedEmployee)
            .Include(t => t.AssignedProject)
            .FirstOrDefaultAsync(t => t.Id == request.ToolId, cancellationToken)
            ?? throw new NotFoundException(nameof(Tool), request.ToolId);

        switch (request.Target)
        {
            case ToolAssignmentTarget.Employee when tool.AssignedEmployeeId is null:
                throw new ConflictException("The tool is not assigned to any employee.");

            case ToolAssignmentTarget.Employee:
                tool.AssignedEmployeeId = null;
                tool.AssignedEmployee = null;
                break;

            case ToolAssignmentTarget.Project when tool.AssignedProjectId is null:
                throw new ConflictException("The tool is not assigned to any project.");

            case ToolAssignmentTarget.Project:
                tool.AssignedProjectId = null;
                tool.AssignedProject = null;
                break;

            default:
                throw new ConflictException("Unknown assignment target.");
        }

        ToolRules.RecomputeStatus(tool);

        await _context.SaveChangesAsync(cancellationToken);

        return _mapper.Map<ToolDto>(tool);
    }
}
