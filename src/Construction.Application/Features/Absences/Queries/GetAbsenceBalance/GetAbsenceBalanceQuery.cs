using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Absences.Models;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Absences.Queries.GetAbsenceBalance;

/// <summary>
/// How many annual leave days an employee has left in a calendar year.
/// </summary>
/// <remarks>
/// Meant to sit alongside a review decision: granting leave without knowing
/// what is left of the allowance is a guess, not a decision.
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

        var year = request.Year ?? _dateTimeProvider.UtcNow.Year;
        var yearStart = new DateOnly(year, 1, 1);
        var yearEnd = new DateOnly(year, 12, 31);

        var allowance = await _context.Employees
            .AsNoTracking()
            .Where(e => e.Id == employeeId)
            .Select(e => (int?)e.AnnualLeaveDaysAllowance)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(Employee), employeeId);

        // Overlap with the year, clipped at both ends, so a run of leave that
        // crosses New Year's only counts the days that actually fall in it.
        var overlapping = await _context.Absences
            .AsNoTracking()
            .Where(a => a.EmployeeId == employeeId)
            .Where(a => a.Type == AbsenceType.AnnualLeave)
            .Where(a => a.Status == AbsenceStatus.Approved)
            .Where(a => a.StartDate <= yearEnd && a.EndDate >= yearStart)
            .Select(a => new { a.StartDate, a.EndDate })
            .ToListAsync(cancellationToken);

        var usedDays = overlapping.Sum(a =>
        {
            var clippedStart = a.StartDate > yearStart ? a.StartDate : yearStart;
            var clippedEnd = a.EndDate < yearEnd ? a.EndDate : yearEnd;
            return clippedEnd.DayNumber - clippedStart.DayNumber + 1;
        });

        return new AbsenceBalanceDto
        {
            EmployeeId = employeeId,
            Year = year,
            AllowanceDays = allowance,
            UsedDays = usedDays,
        };
    }
}
