using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Absences.Models;
using Construction.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Absences.Queries.GetAbsenceBalance;

/// <summary>
/// How many annual leave days an employee has left in a calendar year, in working days.
/// </summary>
/// <remarks>
/// Meant to sit alongside a review decision: granting leave without knowing
/// what is left of the allowance is a guess, not a decision. The rules are the customer's:
/// working days, pro rata in the year of starting, unused days carried over and lost on
/// 1 June (see <see cref="LeaveCalculator"/>).
/// </remarks>
public record GetAbsenceBalanceQuery : IRequest<AbsenceBalanceDto>
{
    public Guid EmployeeId { get; init; }

    public int? Year { get; init; }
}

public class GetAbsenceBalanceQueryValidator : AbstractValidator<GetAbsenceBalanceQuery>
{
    public GetAbsenceBalanceQueryValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
    }
}

public class GetAbsenceBalanceQueryHandler : IRequestHandler<GetAbsenceBalanceQuery, AbsenceBalanceDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetAbsenceBalanceQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<AbsenceBalanceDto> Handle(
        GetAbsenceBalanceQuery request,
        CancellationToken cancellationToken)
    {
        // A worker asking after someone else's balance gets their own instead,
        // the same narrowing GetAbsencesQuery applies to the list.
        var employeeId = AbsenceRules.IsRestrictedToOwnAbsences(_currentUserService.Role)
            ? _currentUserService.EmployeeId ?? Guid.Empty
            : request.EmployeeId;

        var today = DateOnly.FromDateTime(_dateTimeProvider.UtcNow);
        var year = request.Year ?? today.Year;

        var employee = await _context.Employees
            .AsNoTracking()
            .Where(e => e.Id == employeeId)
            .Select(e => new { e.AnnualLeaveDaysAllowance, e.EmploymentDate })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(Employee), employeeId);

        var firstYear = Math.Max(employee.EmploymentDate.Year, year - LeaveCalculator.MaxCarryYears);
        firstYear = Math.Min(firstYear, year);

        var taken = (await LeaveData.ApprovedLeaveDaysAsync(
                _context,
                [employeeId],
                new DateOnly(firstYear, 1, 1),
                new DateOnly(year, 12, 31),
                cancellationToken))[employeeId];

        var adjustments = await _context.LeaveAdjustments
            .AsNoTracking()
            .Where(a => a.EmployeeId == employeeId && a.Year >= firstYear && a.Year <= year)
            .GroupBy(a => a.Year)
            .Select(g => new { Year = g.Key, Days = g.Sum(a => a.Days) })
            .ToDictionaryAsync(g => g.Year, g => g.Days, cancellationToken);

        var standing = LeaveCalculator.Standing(
            employee.AnnualLeaveDaysAllowance,
            employee.EmploymentDate,
            year,
            adjustments,
            taken);

        // "As of" is today inside the current year; for a year gone by everything has expired,
        // and for a year to come nothing has.
        var asOf = year < today.Year ? new DateOnly(year, 12, 31)
            : year > today.Year ? new DateOnly(year, 1, 1)
            : today;

        var remaining = LeaveCalculator.Remaining(standing, asOf);

        return new AbsenceBalanceDto
        {
            EmployeeId = employeeId,
            Year = year,
            AllowanceDays = remaining + standing.UsedDays,
            UsedDays = standing.UsedDays,
            EntitlementDays = standing.Entitlement,
            AdjustmentDays = standing.Adjustments,
            CarriedOverDays = standing.CarryIn,
            CarriedOverUsedDays = standing.CarryInUsed,
            CarriedOverExpiredDays = LeaveCalculator.CarryExpired(standing, asOf),
            CarryOverExpiresOn = LeaveCalculator.CarryOverExpiry(year),
            CarryingIntoNextYearDays = standing.CarryOut,
        };
    }
}
