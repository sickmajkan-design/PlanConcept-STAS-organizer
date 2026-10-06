using Construction.Application.Common.Exceptions;
using Construction.Application.Features.Absences;
using Construction.Application.Features.Absences.Commands.RequestAbsence;
using Construction.Application.Features.Absences.Commands.ReviewAbsence;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.IntegrationTests;

/// <summary>
/// A person who is housed and goes on leave stops being housed for those days: automatically for
/// annual leave, by the office's choice for anything else. Housing is not charged for days nobody is
/// there, so what happens to the stay is what the accommodation costs report.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class AbsenceHousingTests : IntegrationTestBase
{
    public AbsenceHousingTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    /// <summary>Leave from day 10 to day 14 ahead, inclusive: five days.</summary>
    private static readonly DateOnly LeaveStart = Today.AddDays(10);
    private static readonly DateOnly LeaveEnd = Today.AddDays(14);

    private async Task<(User Admin, Employee Employee, Accommodation Place, AccommodationStay Stay)> SeedHousedAsync(
        DateOnly stayStart, DateOnly? stayEnd = null)
    {
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var place = await InScope(scope => TestData.SeedAccommodationAsync(scope));

        var stay = await InScope(async scope =>
        {
            var s = new AccommodationStay
            {
                AccommodationId = place.Id,
                EmployeeId = employee.Id,
                StartDate = stayStart,
                EndDate = stayEnd
            };

            scope.Db.AccommodationStays.Add(s);
            await scope.Db.SaveChangesAsync();
            return s;
        });

        return (admin, employee, place, stay);
    }

    private async Task<Guid> RequestedLeaveAsync(Employee employee, AbsenceType type)
    {
        var requester = await InScope(scope =>
            TestData.SeedUserAsync(scope, UserRole.Worker, employeeId: employee.Id));

        return await InScope(async scope =>
        {
            scope.CurrentUser.SignInAs(requester.Id, requester.Role, employee.Id, requester.Email);

            var absence = await scope.Send(new RequestAbsenceCommand
            {
                Type = type,
                StartDate = LeaveStart,
                EndDate = LeaveEnd
            });

            return absence.Id;
        });
    }

    private Task ApproveAsync(User admin, Guid absenceId, bool? release = null, bool? returnAfter = null) =>
        InScope(scope =>
        {
            scope.CurrentUser.SignInAs(admin.Id, admin.Role, null, admin.Email);

            return scope.Send(new ReviewAbsenceCommand
            {
                Id = absenceId,
                Approve = true,
                ReleaseAccommodation = release,
                ReturnToAccommodation = returnAfter
            });
        });

    private Task<List<AccommodationStay>> StaysOfAsync(Guid employeeId) =>
        InScope(scope => scope.Db.AccommodationStays
            .AsNoTracking()
            .Where(s => s.EmployeeId == employeeId)
            .OrderBy(s => s.StartDate)
            .ToListAsync());

    [Fact]
    public async Task Annual_leave_takes_a_housed_person_off_housing_and_books_them_back_after()
    {
        var (admin, employee, place, _) = await SeedHousedAsync(Today.AddDays(-30));
        var leave = await RequestedLeaveAsync(employee, AbsenceType.AnnualLeave);

        await ApproveAsync(admin, leave);

        var stays = await StaysOfAsync(employee.Id);

        Assert.Equal(2, stays.Count);
        Assert.Equal(LeaveStart.AddDays(-1), stays[0].EndDate);
        Assert.Equal(LeaveEnd.AddDays(1), stays[1].StartDate);
        Assert.Null(stays[1].EndDate);
        Assert.Equal(place.Id, stays[1].AccommodationId);
        Assert.Contains("leave", stays[0].Note, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Sick_leave_leaves_housing_alone_unless_the_office_says_otherwise()
    {
        var (admin, employee, _, _) = await SeedHousedAsync(Today.AddDays(-30));
        var leave = await RequestedLeaveAsync(employee, AbsenceType.SickLeave);

        await ApproveAsync(admin, leave);

        var stay = Assert.Single(await StaysOfAsync(employee.Id));
        Assert.Null(stay.EndDate);
    }

    [Fact]
    public async Task Sick_leave_can_take_them_off_housing_when_the_office_chooses()
    {
        var (admin, employee, _, _) = await SeedHousedAsync(Today.AddDays(-30));
        var leave = await RequestedLeaveAsync(employee, AbsenceType.SickLeave);

        await ApproveAsync(admin, leave, release: true);

        var stays = await StaysOfAsync(employee.Id);
        Assert.Equal(LeaveStart.AddDays(-1), stays[0].EndDate);
    }

    [Fact]
    public async Task Annual_leave_can_be_approved_without_touching_housing()
    {
        var (admin, employee, _, _) = await SeedHousedAsync(Today.AddDays(-30));
        var leave = await RequestedLeaveAsync(employee, AbsenceType.AnnualLeave);

        await ApproveAsync(admin, leave, release: false);

        Assert.Null(Assert.Single(await StaysOfAsync(employee.Id)).EndDate);
    }

    [Fact]
    public async Task The_return_can_be_declined()
    {
        var (admin, employee, _, _) = await SeedHousedAsync(Today.AddDays(-30));
        var leave = await RequestedLeaveAsync(employee, AbsenceType.AnnualLeave);

        await ApproveAsync(admin, leave, returnAfter: false);

        var stay = Assert.Single(await StaysOfAsync(employee.Id));
        Assert.Equal(LeaveStart.AddDays(-1), stay.EndDate);
    }

    [Fact]
    public async Task A_stay_that_was_ending_during_the_leave_gets_no_return()
    {
        var (admin, employee, _, _) = await SeedHousedAsync(Today.AddDays(-30), stayEnd: LeaveStart.AddDays(2));
        var leave = await RequestedLeaveAsync(employee, AbsenceType.AnnualLeave);

        await ApproveAsync(admin, leave);

        var stay = Assert.Single(await StaysOfAsync(employee.Id));
        Assert.Equal(LeaveStart.AddDays(-1), stay.EndDate);
    }

    [Fact]
    public async Task A_return_keeps_the_original_end_date()
    {
        var originalEnd = LeaveEnd.AddDays(20);
        var (admin, employee, _, _) = await SeedHousedAsync(Today.AddDays(-30), stayEnd: originalEnd);
        var leave = await RequestedLeaveAsync(employee, AbsenceType.AnnualLeave);

        await ApproveAsync(admin, leave);

        var stays = await StaysOfAsync(employee.Id);
        Assert.Equal(2, stays.Count);
        Assert.Equal(originalEnd, stays[1].EndDate);
    }

    [Fact]
    public async Task A_stay_that_starts_during_the_leave_is_not_touched()
    {
        var (admin, employee, _, _) = await SeedHousedAsync(LeaveStart.AddDays(1));
        var leave = await RequestedLeaveAsync(employee, AbsenceType.AnnualLeave);

        await ApproveAsync(admin, leave);

        var stay = Assert.Single(await StaysOfAsync(employee.Id));
        Assert.Equal(LeaveStart.AddDays(1), stay.StartDate);
        Assert.Null(stay.EndDate);
    }

    [Fact]
    public async Task A_person_who_is_not_housed_is_unaffected()
    {
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var leave = await RequestedLeaveAsync(employee, AbsenceType.AnnualLeave);

        await ApproveAsync(admin, leave);

        Assert.Empty(await StaysOfAsync(employee.Id));
    }

    [Fact]
    public async Task Leave_recorded_already_granted_takes_the_person_off_housing_too()
    {
        var (admin, employee, _, _) = await SeedHousedAsync(Today.AddDays(-30));

        await InScope(scope =>
        {
            scope.CurrentUser.SignInAs(admin.Id, admin.Role, null, admin.Email);

            return scope.Send(new RequestAbsenceCommand
            {
                EmployeeId = employee.Id,
                Type = AbsenceType.AnnualLeave,
                StartDate = LeaveStart,
                EndDate = LeaveEnd,
                Approve = true
            });
        });

        var stays = await StaysOfAsync(employee.Id);
        Assert.Equal(2, stays.Count);
        Assert.Equal(LeaveStart.AddDays(-1), stays[0].EndDate);
    }

    [Fact]
    public async Task The_office_is_told_where_the_person_lives_before_deciding()
    {
        var (admin, employee, place, stay) = await SeedHousedAsync(Today.AddDays(-30));

        var impact = await InScope(scope =>
        {
            scope.CurrentUser.SignInAs(admin.Id, admin.Role, null, admin.Email);

            return scope.Send(new GetAbsenceHousingImpactQuery(employee.Id, LeaveStart, LeaveEnd));
        });

        Assert.True(impact.HasStay);
        Assert.Equal(stay.Id, impact.StayId);
        Assert.Equal(place.Id, impact.AccommodationId);
    }

    [Fact]
    public async Task A_person_with_no_stay_has_no_impact()
    {
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));

        var impact = await InScope(scope =>
        {
            scope.CurrentUser.SignInAs(admin.Id, admin.Role, null, admin.Email);

            return scope.Send(new GetAbsenceHousingImpactQuery(employee.Id, LeaveStart, LeaveEnd));
        });

        Assert.False(impact.HasStay);
    }

    [Fact]
    public async Task Only_the_people_who_decide_leave_may_see_where_staff_live()
    {
        var foreman = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Foreman));
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => InScope(scope =>
        {
            scope.CurrentUser.SignInAs(foreman.Id, foreman.Role, null, foreman.Email);

            return scope.Send(new GetAbsenceHousingImpactQuery(employee.Id, LeaveStart, LeaveEnd));
        }));
    }
}
