using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Tools.Models;
using Construction.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Tools.Commands.SelfCheckOutTool;

/// <summary>
/// Lets the calling employee check a tool out to themselves after scanning
/// its QR label. Unlike <c>AssignToolToEmployeeCommand</c>, the target
/// employee is always the caller — never a route/body parameter — so any
/// authenticated employee may use it, not just a Foreman or above.
/// </summary>
public record SelfCheckOutToolCommand(Guid ToolId) : IRequest<ToolDto>;

public class SelfCheckOutToolCommandHandler : IRequestHandler<SelfCheckOutToolCommand, ToolDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public SelfCheckOutToolCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<ToolDto> Handle(SelfCheckOutToolCommand request, CancellationToken cancellationToken)
    {
        var employeeId = _currentUser.EmployeeId
            ?? throw new ForbiddenAccessException("Only employees can check out tools.");

        var tool = await _context.Tools
            .Include(t => t.AssignedProject)
            .FirstOrDefaultAsync(t => t.Id == request.ToolId, cancellationToken)
            ?? throw new NotFoundException(nameof(Tool), request.ToolId);

        ToolRules.EnsureAssignable(tool);

        if (tool.AssignedEmployeeId == employeeId)
        {
            throw new ConflictException("The tool is already checked out to you.");
        }

        if (tool.AssignedEmployeeId is not null)
        {
            throw new ConflictException("The tool is already checked out to someone else.");
        }

        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.Id == employeeId, cancellationToken)
            ?? throw new NotFoundException(nameof(Employee), employeeId);

        tool.AssignedEmployeeId = employee.Id;
        tool.AssignedEmployee = employee;
        ToolRules.RecomputeStatus(tool);

        await _context.SaveChangesAsync(cancellationToken);

        return ToolMapping.ToDto(tool);
    }
}
