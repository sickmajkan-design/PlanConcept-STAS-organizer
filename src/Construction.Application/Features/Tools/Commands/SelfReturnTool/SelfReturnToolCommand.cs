using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Tools.Models;
using Construction.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Tools.Commands.SelfReturnTool;

/// <summary>
/// Lets the calling employee return a tool that is currently checked out to
/// them. Any tool checked out to someone else stays theirs to return.
/// </summary>
public record SelfReturnToolCommand(Guid ToolId) : IRequest<ToolDto>;

public class SelfReturnToolCommandHandler : IRequestHandler<SelfReturnToolCommand, ToolDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public SelfReturnToolCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<ToolDto> Handle(SelfReturnToolCommand request, CancellationToken cancellationToken)
    {
        var employeeId = _currentUser.EmployeeId
            ?? throw new ForbiddenAccessException("Only employees can return tools.");

        var tool = await _context.Tools
            .Include(t => t.AssignedProject)
            .FirstOrDefaultAsync(t => t.Id == request.ToolId, cancellationToken)
            ?? throw new NotFoundException(nameof(Tool), request.ToolId);

        if (tool.AssignedEmployeeId != employeeId)
        {
            throw new ConflictException("This tool is not checked out to you.");
        }

        tool.AssignedEmployeeId = null;
        tool.AssignedEmployee = null;
        ToolRules.RecomputeStatus(tool);

        await _context.SaveChangesAsync(cancellationToken);

        return ToolMapping.ToDto(tool);
    }
}
