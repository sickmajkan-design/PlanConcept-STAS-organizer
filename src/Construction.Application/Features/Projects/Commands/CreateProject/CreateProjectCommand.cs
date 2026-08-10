using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Projects.Models;
using Construction.Domain.Entities;
using MediatR;

namespace Construction.Application.Features.Projects.Commands.CreateProject;

public record CreateProjectCommand : ProjectCommandBase, IRequest<ProjectDto>;

public class CreateProjectCommandValidator : ProjectCommandBaseValidator<CreateProjectCommand>;

public class CreateProjectCommandHandler : IRequestHandler<CreateProjectCommand, ProjectDto>
{
    private readonly IApplicationDbContext _context;

    public CreateProjectCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ProjectDto> Handle(
        CreateProjectCommand request,
        CancellationToken cancellationToken)
    {
        var project = new Project
        {
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            Client = request.Client?.Trim(),
            Address = request.Address?.Trim(),
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Status = request.Status
        };

        _context.Projects.Add(project);

        await _context.SaveChangesAsync(cancellationToken);

        return ProjectMapping.ToDto(project);
    }
}
