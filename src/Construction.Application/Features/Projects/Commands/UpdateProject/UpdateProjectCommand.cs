using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Projects.Models;
using Construction.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Projects.Commands.UpdateProject;

public record UpdateProjectCommand : ProjectCommandBase, IRequest<ProjectDto>
{
    /// <summary>Set by the API layer from the route, never from the request body.</summary>
    public Guid Id { get; init; }
}

public class UpdateProjectCommandValidator : ProjectCommandBaseValidator<UpdateProjectCommand>;

public class UpdateProjectCommandHandler : IRequestHandler<UpdateProjectCommand, ProjectDto>
{
    private readonly IApplicationDbContext _context;

    public UpdateProjectCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ProjectDto> Handle(
        UpdateProjectCommand request,
        CancellationToken cancellationToken)
    {
        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Project), request.Id);

        project.Name = request.Name.Trim();
        project.Description = request.Description?.Trim();
        project.Client = request.Client?.Trim();
        project.Address = request.Address?.Trim();
        project.Latitude = request.Latitude;
        project.Longitude = request.Longitude;
        project.StartDate = request.StartDate;
        project.EndDate = request.EndDate;
        project.Status = request.Status;

        await _context.SaveChangesAsync(cancellationToken);

        return ProjectMapping.ToDto(project);
    }
}
