using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Tools.Commands.DeleteTool;

/// <summary>
/// Soft-deletes a tool, clearing its assignments first so it no longer
/// appears as held by an employee or a project.
/// </summary>
public record DeleteToolCommand(Guid Id) : IRequest;

public class DeleteToolCommandHandler : IRequestHandler<DeleteToolCommand>
{
    private readonly IApplicationDbContext _context;

    public DeleteToolCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DeleteToolCommand request, CancellationToken cancellationToken)
    {
        var tool = await _context.Tools
            .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Tool), request.Id);

        tool.AssignedEmployeeId = null;
        tool.AssignedProjectId = null;

        _context.Tools.Remove(tool);

        await _context.SaveChangesAsync(cancellationToken);
    }
}
