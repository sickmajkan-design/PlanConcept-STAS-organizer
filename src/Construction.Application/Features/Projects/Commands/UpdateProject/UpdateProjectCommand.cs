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

        var customerId = request.CustomerId;

        if (request.ParentProjectId is { } parentProjectId)
        {
            if (parentProjectId == project.Id)
            {
                throw new ConflictException("A project cannot be its own parent.");
            }

            var parent = await _context.Projects
                .FirstOrDefaultAsync(p => p.Id == parentProjectId, cancellationToken)
                ?? throw new NotFoundException(nameof(Project), parentProjectId);

            if (parent.ParentProjectId is not null)
            {
                throw new ConflictException(
                    "A sub-project's parent must itself be a Main project.");
            }

            // A sub-project cannot itself carry sub-projects — hierarchy stops
            // at two levels, so this project's own children would be
            // stranded three deep if it became a sub-project now.
            var hasOwnSubProjects = await _context.Projects
                .AnyAsync(p => p.ParentProjectId == project.Id, cancellationToken);

            if (hasOwnSubProjects)
            {
                throw new ConflictException(
                    "This project has its own sub-projects and cannot become a sub-project itself. Reparent or remove them first.");
            }

            // Always the parent's, regardless of what was submitted — a
            // sub-project belongs to whichever customer its Main project does.
            customerId = parent.CustomerId;
        }
        else if (request.CustomerId is { } requestedCustomerId)
        {
            var customerExists = await _context.Customers
                .AnyAsync(c => c.Id == requestedCustomerId, cancellationToken);

            if (!customerExists)
            {
                throw new NotFoundException(nameof(Customer), requestedCustomerId);
            }
        }

        project.Name = request.Name.Trim();
        project.Description = request.Description?.Trim();
        project.CustomerId = customerId;
        project.ParentProjectId = request.ParentProjectId;
        project.Address = request.Address?.Trim();
        project.Latitude = request.Latitude;
        project.Longitude = request.Longitude;
        project.ShiftStartTime = request.ShiftStartTime;
        project.StartDate = request.StartDate;
        project.EndDate = request.EndDate;
        project.Status = request.Status;
        project.ContractValue = request.ContractValue;

        await _context.SaveChangesAsync(cancellationToken);

        return await _context.Projects
            .AsNoTracking()
            .Where(p => p.Id == project.Id)
            .Select(ProjectMapping.Projection)
            .FirstAsync(cancellationToken);
    }
}
