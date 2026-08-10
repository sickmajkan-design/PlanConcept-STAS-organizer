using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Tools.Models;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Tools.Commands.UpdateTool;

public record UpdateToolCommand : ToolCommandBase, IRequest<ToolDto>
{
    /// <summary>Set by the API layer from the route, never from the request body.</summary>
    public Guid Id { get; init; }
}

public class UpdateToolCommandValidator : ToolCommandBaseValidator<UpdateToolCommand>;

public class UpdateToolCommandHandler : IRequestHandler<UpdateToolCommand, ToolDto>
{
    private readonly IApplicationDbContext _context;

    public UpdateToolCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ToolDto> Handle(UpdateToolCommand request, CancellationToken cancellationToken)
    {
        var tool = await _context.Tools
            .Include(t => t.AssignedEmployee)
            .Include(t => t.AssignedProject)
            .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Tool), request.Id);

        var serialNumber = string.IsNullOrWhiteSpace(request.SerialNumber)
            ? null
            : request.SerialNumber.Trim();
        var qrCode = string.IsNullOrWhiteSpace(request.QrCode) ? null : request.QrCode.Trim();

        await ToolRules.EnsureUniqueAsync(_context, serialNumber, qrCode, request.Id, cancellationToken);

        var hasAssignment = tool.AssignedEmployeeId is not null || tool.AssignedProjectId is not null;

        if (hasAssignment && request.Status != ToolStatus.Assigned)
        {
            throw new ConflictException(
                "The tool is currently assigned; unassign it before changing its status.");
        }

        tool.Name = request.Name.Trim();
        tool.Category = request.Category?.Trim();
        tool.SerialNumber = serialNumber;
        tool.QrCode = qrCode;
        tool.Status = request.Status;

        await _context.SaveChangesAsync(cancellationToken);

        return ToolMapping.ToDto(tool);
    }
}
