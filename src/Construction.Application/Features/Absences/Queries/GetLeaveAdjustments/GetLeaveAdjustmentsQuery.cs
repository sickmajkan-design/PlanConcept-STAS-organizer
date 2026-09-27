using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Absences.Models;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Absences.Queries.GetLeaveAdjustments;

/// <summary>
/// The history of manual corrections of somebody's annual leave, newest first. A worker
/// only ever gets their own.
/// </summary>
public record GetLeaveAdjustmentsQuery : IRequest<IReadOnlyList<LeaveAdjustmentDto>>
{
    public Guid EmployeeId { get; init; }

    public int? Year { get; init; }
}

public class GetLeaveAdjustmentsQueryValidator : AbstractValidator<GetLeaveAdjustmentsQuery>
{
    public GetLeaveAdjustmentsQueryValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
    }
}

public class GetLeaveAdjustmentsQueryHandler
    : IRequestHandler<GetLeaveAdjustmentsQuery, IReadOnlyList<LeaveAdjustmentDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetLeaveAdjustmentsQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<LeaveAdjustmentDto>> Handle(
        GetLeaveAdjustmentsQuery request,
        CancellationToken cancellationToken)
    {
        var employeeId = AbsenceRules.IsRestrictedToOwnAbsences(_currentUserService.Role)
            ? _currentUserService.EmployeeId ?? Guid.Empty
            : request.EmployeeId;

        var query = _context.LeaveAdjustments.AsNoTracking().Where(a => a.EmployeeId == employeeId);

        if (request.Year is { } year)
        {
            query = query.Where(a => a.Year == year);
        }

        return await query
            .OrderByDescending(a => a.CreatedAt)
            .Select(LeaveAdjustmentMapping.Projection)
            .ToListAsync(cancellationToken);
    }
}
