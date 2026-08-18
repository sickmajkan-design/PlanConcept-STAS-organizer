using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Costs;
using Construction.Application.Features.Employees.Models;
using Construction.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Employees.Queries.GetEmployeeById;

public record GetEmployeeByIdQuery(Guid Id) : IRequest<EmployeeDetailDto>;

public class GetEmployeeByIdQueryHandler : IRequestHandler<GetEmployeeByIdQuery, EmployeeDetailDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetEmployeeByIdQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<EmployeeDetailDto> Handle(
        GetEmployeeByIdQuery request,
        CancellationToken cancellationToken)
    {
        var employee = await _context.Employees
            .AsNoTracking()
            .Where(e => e.Id == request.Id)
            .Select(EmployeeDetailMapping.Projection)
            .FirstOrDefaultAsync(cancellationToken);

        if (employee is null)
        {
            throw new NotFoundException(nameof(Employee), request.Id);
        }

        // Pay is withheld from a role the API does not show it to, the same
        // as everywhere else money appears — the hours a posting ran still
        // show, just not what it cost.
        var includesPay = CostRules.CanSeeLabourCost(_currentUserService.Role);
        var today = DateOnly.FromDateTime(_dateTimeProvider.UtcNow);

        var entriesByProject = (await _context.FinanceEntries
            .AsNoTracking()
            .Where(f => f.EmployeeId == request.Id && f.ProjectId != null)
            .Select(f => new
            {
                ProjectId = f.ProjectId!.Value,
                Entry = new AssignmentPaySummary.Entry(f.OccurredOn, f.Kind, f.Amount, f.HoursWorked)
            })
            .ToListAsync(cancellationToken))
            .ToLookup(x => x.ProjectId, x => x.Entry);

        void ApplyPay(EmployeeProjectAssignmentDto assignment)
        {
            var totals = AssignmentPaySummary.For(
                entriesByProject[assignment.ProjectId],
                assignment.StartDate,
                assignment.EndDate ?? today);

            assignment.WorkedHours = totals.Hours;
            assignment.WorkedDays = totals.Days;
            assignment.TotalPay = includesPay ? totals.Amount : null;
        }

        foreach (var assignment in employee.Projects.Concat(employee.PastProjects))
        {
            ApplyPay(assignment);
        }

        return employee;
    }
}
