using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Tools.Models;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Tools.Commands.AssignTool;

/// <summary>
/// Hands the tool to an employee (or moves it to another employee). Any
/// project assignment is kept — a tool can sit on a project and be held by
/// an employee at the same time.
/// </summary>
public record AssignToolToEmployeeCommand(Guid ToolId, Guid EmployeeId) : IRequest<ToolDto>;

public class AssignToolToEmployeeCommandHandler : IRequestHandler<AssignToolToEmployeeCommand, ToolDto>
{
    private readonly IApplicationDbContext _context;
    private readonly INotificationService _notificationService;

    public AssignToolToEmployeeCommandHandler(
        IApplicationDbContext context,
        INotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    public async Task<ToolDto> Handle(
        AssignToolToEmployeeCommand request,
        CancellationToken cancellationToken)
    {
        var tool = await _context.Tools
            .Include(t => t.AssignedProject)
            .FirstOrDefaultAsync(t => t.Id == request.ToolId, cancellationToken)
            ?? throw new NotFoundException(nameof(Tool), request.ToolId);

        var employee = await _context.Employees
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.Id == request.EmployeeId, cancellationToken)
            ?? throw new NotFoundException(nameof(Employee), request.EmployeeId);

        ToolRules.EnsureAssignable(tool);

        if (tool.AssignedEmployeeId == request.EmployeeId)
        {
            throw new ConflictException("The tool is already assigned to this employee.");
        }

        tool.AssignedEmployeeId = employee.Id;
        tool.AssignedEmployee = employee;
        ToolRules.RecomputeStatus(tool);

        await _context.SaveChangesAsync(cancellationToken);

        if (employee.User is { IsActive: true } user)
        {
            await _notificationService.NotifyUserAsync(
                user.Id,
                NotificationType.ToolAssigned,
                "Tool assigned",
                $"Tool '{tool.Name}' has been assigned to you.",
                new Dictionary<string, string> { ["toolId"] = tool.Id.ToString() },
                cancellationToken);
        }

        return ToolMapping.ToDto(tool);
    }
}
