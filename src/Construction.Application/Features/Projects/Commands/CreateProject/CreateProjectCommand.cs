using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Projects.Models;
using Construction.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

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
        var customerId = request.CustomerId;
        var countryCode = request.CountryCode?.Trim().ToUpperInvariant();

        if (request.ParentProjectId is { } parentProjectId)
        {
            var parent = await _context.Projects
                .FirstOrDefaultAsync(p => p.Id == parentProjectId, cancellationToken)
                ?? throw new NotFoundException(nameof(Project), parentProjectId);

            if (parent.ParentProjectId is not null)
            {
                throw new ConflictException(
                    "A sub-project's parent must itself be a Main project.");
            }

            // Always the parent's, regardless of what was submitted — a
            // sub-project belongs to whichever customer and country its Main
            // project does: it is the same site, just a narrower scope of it.
            customerId = parent.CustomerId;
            countryCode = parent.CountryCode;
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

        var project = new Project
        {
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            CustomerId = customerId,
            ParentProjectId = request.ParentProjectId,
            Address = request.Address?.Trim(),
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            CountryCode = countryCode,
            ShiftStartTime = request.ShiftStartTime,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Status = request.Status,
            ContractValue = request.ContractValue
        };

        _context.Projects.Add(project);

        await _context.SaveChangesAsync(cancellationToken);

        return await _context.Projects
            .AsNoTracking()
            .Where(p => p.Id == project.Id)
            .Select(ProjectMapping.Projection)
            .FirstAsync(cancellationToken);
    }
}
