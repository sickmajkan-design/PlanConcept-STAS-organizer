using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Costs;
using Construction.Application.Features.Projects.Models;
using Construction.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Projects.Queries.GetProjectById;

public record GetProjectByIdQuery(Guid Id) : IRequest<ProjectDetailDto>;

public class GetProjectByIdQueryHandler : IRequestHandler<GetProjectByIdQuery, ProjectDetailDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetProjectByIdQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ProjectDetailDto> Handle(
        GetProjectByIdQuery request,
        CancellationToken cancellationToken)
    {
        var project = await _context.Projects
            .AsNoTracking()
            .Where(p => p.Id == request.Id)
            .Select(ProjectDetailMapping.Projection)
            .FirstOrDefaultAsync(cancellationToken);

        if (project is null)
        {
            throw new NotFoundException(nameof(Project), request.Id);
        }

        // Pay is withheld from a role the API does not show it to — a
        // foreman still sees how long each posting ran, just not its cost.
        var includesPay = CostRules.CanSeeLabourCost(_currentUserService.Role);
        var today = DateOnly.FromDateTime(_dateTimeProvider.UtcNow);

        var entriesByEmployee = (await _context.FinanceEntries
            .AsNoTracking()
            .Where(f => f.ProjectId == request.Id)
            .Select(f => new
            {
                f.EmployeeId,
                Entry = new AssignmentPaySummary.Entry(f.OccurredOn, f.Kind, f.Amount, f.HoursWorked)
            })
            .ToListAsync(cancellationToken))
            .ToLookup(x => x.EmployeeId, x => x.Entry);

        void ApplyPay(ProjectEmployeeDto member)
        {
            var totals = AssignmentPaySummary.For(
                entriesByEmployee[member.EmployeeId],
                member.StartDate,
                member.EndDate ?? today);

            member.WorkedHours = totals.Hours;
            member.WorkedDays = totals.Days;
            member.TotalPay = includesPay ? totals.Amount : null;
        }

        foreach (var member in project.Employees.Concat(project.PastEmployees))
        {
            ApplyPay(member);
        }

        return project;
    }
}
