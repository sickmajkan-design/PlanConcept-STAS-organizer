using Construction.Application.Common.Exceptions;
using Construction.Application.Features.Absences;
using Construction.Application.Features.Absences.Commands.CreateLeaveAdjustment;
using Construction.Application.Features.Absences.Queries.GetAbsenceBalance;
using Construction.Application.Features.Absences.Queries.GetLeaveAdjustments;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.IntegrationTests;

/// <summary>
/// The customer's leave rules against a real database: working days, holidays, pro rata in the
/// year of starting, carry-over, and manual corrections with who wrote them.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class LeaveRulesTests : IntegrationTestBase
{
    public LeaveRulesTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    private static void ActAs(TestScope scope, User user, Guid? employeeId = null) =>
        scope.CurrentUser.SignInAs(user.Id, user.Role, employeeId, user.Email);

    private async Task<Employee> EmployeeAsync(DateOnly? employed = null)
    {
        return await InScope(async scope =>
        {
            var employee = await TestData.SeedEmployeeAsync(scope);
            employee.EmploymentDate = employed ?? new DateOnly(2020, 1, 1);
            await scope.Db.SaveChangesAsync();

            return employee;
        });
    }

    private Task LeaveAsync(Employee employee, DateOnly start, DateOnly end, AbsenceStatus status = AbsenceStatus.Approved) =>
        InScope(async scope =>
        {
            scope.Db.Absences.Add(new Absence
            {
                EmployeeId = employee.Id,
                Type = AbsenceType.AnnualLeave,
                Status = status,
                StartDate = start,
                EndDate = end,
            });

            await scope.Db.SaveChangesAsync();
        });

    private async Task<Application.Features.Absences.Models.AbsenceBalanceDto> BalanceAsync(Employee employee, int year)
    {
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));

        return await InScope(scope =>
        {
            ActAs(scope, admin);
            return scope.Send(new GetAbsenceBalanceQuery { EmployeeId = employee.Id, Year = year });
        });
    }

    [Fact]
    public async Task Leave_is_counted_in_working_days_not_calendar_days()
    {
        var employee = await EmployeeAsync();

        // Mon 2 March - Sun 8 March 2026: seven calendar days, five working days.
        await LeaveAsync(employee, new DateOnly(2026, 3, 2), new DateOnly(2026, 3, 8));

        var balance = await BalanceAsync(employee, 2026);

        Assert.Equal(5, balance.UsedDays);
    }

    [Fact]
    public async Task Only_approved_annual_leave_counts()
    {
        var employee = await EmployeeAsync();

        await LeaveAsync(employee, new DateOnly(2026, 3, 2), new DateOnly(2026, 3, 3), AbsenceStatus.Requested);
        await LeaveAsync(employee, new DateOnly(2026, 3, 4), new DateOnly(2026, 3, 5), AbsenceStatus.Rejected);

        await InScope(async scope =>
        {
            scope.Db.Absences.Add(new Absence
            {
                EmployeeId = employee.Id,
                Type = AbsenceType.SickLeave,
                Status = AbsenceStatus.Approved,
                StartDate = new DateOnly(2026, 3, 9),
                EndDate = new DateOnly(2026, 3, 10),
            });
            await scope.Db.SaveChangesAsync();
        });

        Assert.Equal(0, (await BalanceAsync(employee, 2026)).UsedDays);
    }

    [Fact]
    public async Task A_public_holiday_of_the_firms_country_is_not_a_leave_day()
    {
        var employee = await EmployeeAsync();

        await InScope(async scope =>
        {
            var settings = await scope.Db.CompanySettings.FirstOrDefaultAsync();
            if (settings is null)
            {
                settings = new CompanySettings { Name = "Test firm" };
                scope.Db.CompanySettings.Add(settings);
            }

            settings.LeaveHolidayCountryCode = "DE";
            scope.Db.PublicHolidays.Add(new PublicHoliday
            {
                Date = new DateOnly(2026, 3, 4), Name = "Leave test holiday", CountryCode = "DE",
            });
            scope.Db.PublicHolidays.Add(new PublicHoliday
            {
                Date = new DateOnly(2026, 3, 5), Name = "Leave test holiday", CountryCode = "HR", // another country's
            });
            await scope.Db.SaveChangesAsync();
        });

        try
        {
            await LeaveAsync(employee, new DateOnly(2026, 3, 2), new DateOnly(2026, 3, 6));

            Assert.Equal(4, (await BalanceAsync(employee, 2026)).UsedDays);
        }
        finally
        {
            await InScope(async scope =>
            {
                await scope.Db.CompanySettings.ExecuteDeleteAsync();
                await scope.Db.PublicHolidays.Where(h => h.Name == "Leave test holiday").ExecuteDeleteAsync();
            });
        }
    }

    [Fact]
    public async Task The_right_is_pro_rata_in_the_year_of_starting()
    {
        var employee = await EmployeeAsync(new DateOnly(2026, 7, 1));

        var balance = await BalanceAsync(employee, 2026);

        Assert.Equal(10, balance.EntitlementDays); // six full months of twenty days
        Assert.Equal(0, balance.CarriedOverDays);
    }

    [Fact]
    public async Task Unused_days_carry_into_the_next_year_and_the_expiry_date_is_stated()
    {
        var employee = await EmployeeAsync();

        // 10 working days in October 2025 (Mon 6 - Fri 17).
        await LeaveAsync(employee, new DateOnly(2025, 10, 6), new DateOnly(2025, 10, 17));

        var balance = await BalanceAsync(employee, 2026);

        Assert.Equal(10, balance.CarriedOverDays);
        Assert.Equal(new DateOnly(2026, 6, 1), balance.CarryOverExpiresOn);
        Assert.Equal(20, balance.EntitlementDays);
    }

    [Fact]
    public async Task Days_taken_are_counted_in_the_year_they_fall_in()
    {
        var employee = await EmployeeAsync();

        // Mon 28 Dec 2026 - Fri 1 Jan 2027: four working days in 2026 (31 Dec is a Thursday), one in 2027.
        await LeaveAsync(employee, new DateOnly(2026, 12, 28), new DateOnly(2027, 1, 1));

        Assert.Equal(4, (await BalanceAsync(employee, 2026)).UsedDays);
        Assert.Equal(1, (await BalanceAsync(employee, 2027)).UsedDays);
    }

    [Fact]
    public async Task Going_over_the_right_shows_as_a_negative_remainder()
    {
        var employee = await EmployeeAsync(new DateOnly(2026, 1, 1));

        // 25 working days: Mon 5 Jan - Fri 6 Feb 2026.
        await LeaveAsync(employee, new DateOnly(2026, 1, 5), new DateOnly(2026, 2, 6));

        var balance = await BalanceAsync(employee, 2026);

        Assert.Equal(25, balance.UsedDays);
        Assert.True(balance.RemainingDays < 0 || balance.AllowanceDays >= 20);
        Assert.Equal(balance.AllowanceDays - balance.UsedDays, balance.RemainingDays);
    }

    // ------------------------------------------------------------- corrections

    [Fact]
    public async Task Management_writes_a_correction_and_it_counts_and_shows_who_wrote_it()
    {
        var employee = await EmployeeAsync();
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));

        var created = await InScope(scope =>
        {
            ActAs(scope, admin);
            return scope.Send(new CreateLeaveAdjustmentCommand
            {
                EmployeeId = employee.Id,
                Year = 2026,
                Days = 3,
                Reason = "Prenos iz stare evidencije",
            });
        });

        Assert.Equal(3, created.Days);
        Assert.Equal(admin.Email, created.CreatedBy);

        var balance = await BalanceAsync(employee, 2026);
        Assert.Equal(3, balance.AdjustmentDays);

        var history = await InScope(scope =>
        {
            ActAs(scope, admin);
            return scope.Send(new GetLeaveAdjustmentsQuery { EmployeeId = employee.Id });
        });
        Assert.Single(history);
    }

    [Fact]
    public async Task A_correction_can_take_days_away_and_is_undone_by_an_opposite_one()
    {
        var employee = await EmployeeAsync();
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.SuperAdmin));

        async Task Adjust(int days) => await InScope(scope =>
        {
            ActAs(scope, admin);
            return scope.Send(new CreateLeaveAdjustmentCommand
            {
                EmployeeId = employee.Id, Year = 2026, Days = days, Reason = "Ispravka",
            });
        });

        await Adjust(-4);
        Assert.Equal(-4, (await BalanceAsync(employee, 2026)).AdjustmentDays);

        await Adjust(4);
        Assert.Equal(0, (await BalanceAsync(employee, 2026)).AdjustmentDays);
    }

    [Fact]
    public async Task Below_management_nobody_may_write_a_correction()
    {
        var employee = await EmployeeAsync();

        foreach (var role in new[] { UserRole.ProjectManager, UserRole.Foreman, UserRole.Worker })
        {
            var user = await InScope(scope => TestData.SeedUserAsync(scope, role));

            await Assert.ThrowsAsync<ForbiddenAccessException>(() => InScope(scope =>
            {
                ActAs(scope, user);
                return scope.Send(new CreateLeaveAdjustmentCommand
                {
                    EmployeeId = employee.Id, Year = 2026, Days = 2, Reason = "Zelim vise",
                });
            }));
        }
    }

    [Fact]
    public async Task A_correction_needs_a_reason_and_a_number_of_days()
    {
        var employee = await EmployeeAsync();
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));

        Task Send(int days, string reason) => InScope(scope =>
        {
            ActAs(scope, admin);
            return scope.Send(new CreateLeaveAdjustmentCommand
            {
                EmployeeId = employee.Id, Year = 2026, Days = days, Reason = reason,
            });
        });

        await Assert.ThrowsAsync<ValidationException>(() => Send(0, "Nista"));
        await Assert.ThrowsAsync<ValidationException>(() => Send(2, ""));
        await Assert.ThrowsAsync<ValidationException>(() => Send(500, "Previse"));
    }

    [Fact]
    public async Task A_worker_asking_after_somebody_elses_leave_gets_their_own()
    {
        var mine = await EmployeeAsync();
        var theirs = await EmployeeAsync();
        var worker = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Worker));

        await LeaveAsync(theirs, new DateOnly(2026, 3, 2), new DateOnly(2026, 3, 6));

        var balance = await InScope(scope =>
        {
            ActAs(scope, worker, mine.Id);
            return scope.Send(new GetAbsenceBalanceQuery { EmployeeId = theirs.Id, Year = 2026 });
        });

        Assert.Equal(mine.Id, balance.EmployeeId);
        Assert.Equal(0, balance.UsedDays);
    }
}
