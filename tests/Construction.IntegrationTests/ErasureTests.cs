using Construction.Application.Common.Exceptions;
using Construction.Application.Features.Employees.Commands.DeleteEmployee;
using Construction.Application.Features.Locations.Commands.ReportLocations;
using Construction.Application.Features.Privacy.Commands.ErasePersonalData;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.IntegrationTests;

/// <summary>
/// Erasing a person without erasing the employment record.
/// </summary>
/// <remarks>
/// <para>
/// The command is irreversible, so what it <em>keeps</em> needs testing at
/// least as much as what it removes. An erasure that took the hours with the
/// GPS would trade a privacy failure for a payroll one, and nobody would find
/// out until somebody queried a wage.
/// </para>
/// <para>
/// These run against PostgreSQL because most of the work is
/// <c>ExecuteDelete</c> and <c>ExecuteUpdate</c> over rows the ordinary query
/// filters hide — an erasure request usually arrives after the employee has
/// already been soft-deleted, which is exactly when a filtered query reports
/// them as not found.
/// </para>
/// </remarks>
[Collection(DatabaseCollection.Name)]
public class ErasureTests : IntegrationTestBase
{
    public ErasureTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    private static readonly DateTime Noon = new(2026, 8, 5, 12, 0, 0, DateTimeKind.Utc);

    private const string Reason = "Data subject erasure request, ref DSR-2026-014";

    /// <summary>An employee with an account, a track, a shift and an absence.</summary>
    private async Task<(Employee Employee, User User)> SeedPersonAsync()
    {
        var (employee, user) = await InScope(async scope =>
        {
            var employee = await TestData.SeedEmployeeAsync(scope);
            var user = await TestData.SeedUserAsync(scope, UserRole.Worker, employee.Id);

            employee.Phone = "+381 60 000 0000";
            employee.Email = "radnik@example.test";
            employee.Address = "Ulica 1, Beograd";
            employee.DateOfBirth = new DateOnly(1988, 4, 2);
            await scope.Db.SaveChangesAsync();

            return (employee, user);
        });

        // A GPS track.
        await InScope(scope =>
        {
            scope.CurrentUser.SignInAs(user.Id, user.Role, employee.Id, user.Email);
            return scope.Send(new ReportLocationsCommand
            {
                Pings =
                [
                    new LocationPing { Latitude = 44.8, Longitude = 20.4, Timestamp = Noon },
                    new LocationPing { Latitude = 44.9, Longitude = 20.5, Timestamp = Noon.AddMinutes(5) }
                ]
            });
        });

        // A completed shift with clock-in and clock-out coordinates, and a
        // sick-leave absence carrying a free-text reason.
        await InScope(async scope =>
        {
            scope.Db.TimeEntries.Add(new TimeEntry
            {
                EmployeeId = employee.Id,
                StartedAt = Noon,
                EndedAt = Noon.AddHours(8),
                Status = TimeEntryStatus.Approved,
                StartLatitude = 44.81,
                StartLongitude = 20.41,
                EndLatitude = 44.82,
                EndLongitude = 20.42
            });

            scope.Db.Absences.Add(new Absence
            {
                EmployeeId = employee.Id,
                Type = AbsenceType.SickLeave,
                Status = AbsenceStatus.Approved,
                StartDate = new DateOnly(2026, 7, 1),
                EndDate = new DateOnly(2026, 7, 5),
                Reason = "Operacija kolena, nalaz priložen",
                ReviewNote = "Odobreno na osnovu doznake"
            });

            scope.Db.Notifications.Add(new Notification
            {
                UserId = user.Id,
                Type = NotificationType.VehicleAssigned,
                Title = "Vozilo dodeljeno",
                Body = "Kombi ZG-1234-AB"
            });

            scope.Db.DeviceTokens.Add(new DeviceToken
            {
                UserId = user.Id,
                Token = $"fcm-{Guid.NewGuid():N}",
                Platform = DevicePlatform.Android
            });

            await scope.Db.SaveChangesAsync();
        });

        return (employee, user);
    }

    private Task<ErasureResult> EraseAsync(Guid employeeId) =>
        InScope(scope => scope.Send(new ErasePersonalDataCommand
        {
            EmployeeId = employeeId,
            Reason = Reason
        }));

    private Task<Employee> ReloadAsync(Guid id) =>
        InScope(scope => scope.Db.Employees
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleAsync(e => e.Id == id));

    // ---- what goes -------------------------------------------------------

    [Fact]
    public async Task The_whole_GPS_track_is_removed()
    {
        // Hard-deleted, not soft-deleted. A soft-deleted GPS track is still a
        // GPS track — it is one query filter away from being read again, and
        // it is still in the backup.
        var (employee, _) = await SeedPersonAsync();

        var result = await EraseAsync(employee.Id);

        Assert.Equal(2, result.LocationRecords);

        var left = await InScope(scope => scope.Db.LocationRecords
            .IgnoreQueryFilters()
            .CountAsync(l => l.EmployeeId == employee.Id));

        Assert.Equal(0, left);
    }

    [Fact]
    public async Task Clock_in_coordinates_go_but_the_shift_stays()
    {
        // The case that makes this command worth writing rather than reusing
        // delete. The hours are payroll and must survive; the two coordinates
        // attached to them are location data and must not.
        var (employee, _) = await SeedPersonAsync();

        var result = await EraseAsync(employee.Id);

        Assert.Equal(1, result.TimeEntryCoordinates);

        var entry = await InScope(scope => scope.Db.TimeEntries
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleAsync(t => t.EmployeeId == employee.Id));

        Assert.Null(entry.StartLatitude);
        Assert.Null(entry.StartLongitude);
        Assert.Null(entry.EndLatitude);
        Assert.Null(entry.EndLongitude);

        // Still a payable shift.
        Assert.Equal(Noon, entry.StartedAt);
        Assert.Equal(Noon.AddHours(8), entry.EndedAt);
        Assert.Equal(TimeEntryStatus.Approved, entry.Status);
    }

    [Fact]
    public async Task The_reason_somebody_was_off_sick_is_cleared()
    {
        // A free-text reason on a sick-leave record is health data in all but
        // name. The dates and the leave type stay because payroll needs them;
        // the sentence describing the illness does not.
        var (employee, _) = await SeedPersonAsync();

        var result = await EraseAsync(employee.Id);

        Assert.Equal(1, result.AbsenceReasons);

        var absence = await InScope(scope => scope.Db.Absences
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleAsync(a => a.EmployeeId == employee.Id));

        Assert.Null(absence.Reason);
        Assert.Null(absence.ReviewNote);

        Assert.Equal(AbsenceType.SickLeave, absence.Type);
        Assert.Equal(new DateOnly(2026, 7, 1), absence.StartDate);
        Assert.Equal(AbsenceStatus.Approved, absence.Status);
    }

    [Fact]
    public async Task Contact_details_and_date_of_birth_are_redacted()
    {
        var (employee, _) = await SeedPersonAsync();

        await EraseAsync(employee.Id);

        var erased = await ReloadAsync(employee.Id);

        Assert.Null(erased.Phone);
        Assert.Null(erased.Email);
        Assert.Null(erased.Address);
        Assert.Null(erased.DateOfBirth);
        Assert.Equal(ErasePersonalDataCommandHandler.RedactedName, erased.FirstName);
    }

    [Fact]
    public async Task The_account_is_anonymised_and_cannot_be_signed_into()
    {
        var (employee, user) = await SeedPersonAsync();

        var result = await EraseAsync(employee.Id);

        Assert.True(result.AccountAnonymised);

        var account = await InScope(scope => scope.Db.Users
            .AsNoTracking()
            .SingleAsync(u => u.Id == user.Id));

        Assert.DoesNotContain("@construction.test", account.Email);
        Assert.EndsWith("@invalid", account.Email);
        Assert.False(account.IsActive);
        Assert.Null(account.EmployeeId);
    }

    [Fact]
    public async Task Sessions_device_tokens_and_notifications_are_removed()
    {
        // Sessions and reset tokens carry IP addresses as well as
        // credentials; a device token is a live route to somebody's phone.
        var (employee, user) = await SeedPersonAsync();

        var result = await EraseAsync(employee.Id);

        Assert.Equal(1, result.Notifications);
        Assert.Equal(1, result.DeviceTokens);

        var remaining = await InScope(async scope => new
        {
            Notifications = await scope.Db.Notifications.CountAsync(n => n.UserId == user.Id),
            Devices = await scope.Db.DeviceTokens.CountAsync(d => d.UserId == user.Id),
            Refresh = await scope.Db.RefreshTokens.CountAsync(t => t.UserId == user.Id),
            Resets = await scope.Db.PasswordResetTokens.CountAsync(t => t.UserId == user.Id)
        });

        Assert.Equal(0, remaining.Notifications);
        Assert.Equal(0, remaining.Devices);
        Assert.Equal(0, remaining.Refresh);
        Assert.Equal(0, remaining.Resets);
    }

    // ---- what stays ------------------------------------------------------

    [Fact]
    public async Task The_employment_record_survives()
    {
        // An employer is obliged to hold this for years after somebody leaves.
        // An erasure that removed it would trade a privacy failure for a
        // bookkeeping one.
        var (employee, _) = await SeedPersonAsync();

        await EraseAsync(employee.Id);

        var erased = await ReloadAsync(employee.Id);

        Assert.Equal(employee.EmployeeNumber, erased.EmployeeNumber);
        Assert.Equal(employee.EmploymentDate, erased.EmploymentDate);
        Assert.Equal(employee.Position, erased.Position);
    }

    [Fact]
    public async Task The_timesheet_still_says_whose_shift_it_was()
    {
        // The row keeps its employee id, and the employee row keeps its
        // number. Payroll can still answer "how many hours against this
        // number" — which is the question it actually asks — without the name.
        var (employee, _) = await SeedPersonAsync();

        await EraseAsync(employee.Id);

        var erased = await ReloadAsync(employee.Id);

        Assert.Equal(employee.EmployeeNumber, erased.LastName);

        var hours = await InScope(scope => scope.Db.TimeEntries
            .IgnoreQueryFilters()
            .CountAsync(t => t.EmployeeId == employee.Id));

        Assert.Equal(1, hours);
    }

    [Fact]
    public async Task The_audit_trail_is_left_intact()
    {
        // The decision that needs a lawyer rather than an engineer, so it is
        // pinned here: scrubbing entries would destroy the trail's integrity
        // for everybody else, and the position taken is that it is retained to
        // demonstrate compliance. If that is overruled, this test is what
        // fails and points at the choice.
        var (employee, _) = await SeedPersonAsync();

        var before = await InScope(scope => scope.Db.AuditEntries
            .CountAsync(a => a.EntityId == employee.Id));

        Assert.True(before > 0);

        await EraseAsync(employee.Id);

        var after = await InScope(scope => scope.Db.AuditEntries
            .CountAsync(a => a.EntityId == employee.Id));

        Assert.True(after >= before);
    }

    [Fact]
    public async Task The_erasure_itself_is_recorded()
    {
        // An erasure nobody can see happened is indistinguishable from
        // somebody quietly removing an inconvenient record.
        var (employee, _) = await SeedPersonAsync();

        await EraseAsync(employee.Id);

        var entries = await InScope(scope => scope.Db.AuditEntries
            .AsNoTracking()
            .Where(a => a.EntityName == "Employee" && a.EntityId == employee.Id)
            .ToListAsync());

        Assert.Contains(entries, e => e.Action == AuditAction.Deleted);
    }

    // ---- the edges -------------------------------------------------------

    [Fact]
    public async Task Somebody_who_already_left_can_still_be_erased()
    {
        // The normal case, and the one a filtered query gets wrong: erasure
        // requests arrive after the employee is gone from the directory.
        var (employee, _) = await SeedPersonAsync();

        await InScope(scope => scope.Send(new DeleteEmployeeCommand(employee.Id)));

        var result = await EraseAsync(employee.Id);

        Assert.Equal(2, result.LocationRecords);
    }

    [Fact]
    public async Task An_employee_with_no_account_is_erased_without_complaint()
    {
        // Plenty of site workers never sign in. The account half simply has
        // nothing to do.
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));

        var result = await EraseAsync(employee.Id);

        Assert.False(result.AccountAnonymised);
        Assert.Equal(0, result.DeviceTokens);
    }

    [Fact]
    public async Task Erasing_twice_is_harmless()
    {
        // A request that was retried, or one carried out by two people who
        // each thought the other had not. The second pass finds nothing left
        // and says so rather than failing.
        var (employee, _) = await SeedPersonAsync();

        await EraseAsync(employee.Id);
        var second = await EraseAsync(employee.Id);

        Assert.Equal(0, second.LocationRecords);
        Assert.Equal(0, second.TimeEntryCoordinates);
        Assert.Equal(0, second.AbsenceReasons);
    }

    [Fact]
    public async Task Attachments_are_reported_rather_than_silently_left()
    {
        // Their bytes live in object storage or on disk. Deleting the database
        // row would orphan the file rather than erase it, so the count is
        // returned and the second step is named in the documentation.
        var (employee, _) = await SeedPersonAsync();

        await InScope(async scope =>
        {
            scope.Db.Attachments.Add(new Attachment
            {
                EmployeeId = employee.Id,
                FileName = "ugovor.pdf",
                ContentType = "application/pdf",
                SizeBytes = 1024,
                StorageKey = $"employees/{employee.Id}/ugovor.pdf",
                Category = AttachmentCategory.Contract
            });

            await scope.Db.SaveChangesAsync();
        });

        var result = await EraseAsync(employee.Id);

        Assert.Equal(1, result.AttachmentsFlagged);
    }

    [Fact]
    public async Task Erasing_somebody_who_is_not_there_reports_not_found()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => EraseAsync(Guid.NewGuid()));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("cleanup")]
    public async Task An_erasure_without_a_usable_reason_is_refused(string reason)
    {
        // The reason lands in the audit trail. "cleanup" is not something
        // somebody reading it two years later could act on.
        var (employee, _) = await SeedPersonAsync();

        await Assert.ThrowsAsync<ValidationException>(() => InScope(scope =>
            scope.Send(new ErasePersonalDataCommand
            {
                EmployeeId = employee.Id,
                Reason = reason
            })));
    }

    [Fact]
    public async Task Erasing_one_person_leaves_everybody_else_alone()
    {
        // The failure that would be worst and quietest: a predicate that
        // matched more than one employee.
        var (mine, _) = await SeedPersonAsync();
        var (theirs, theirUser) = await SeedPersonAsync();

        await EraseAsync(mine.Id);

        var untouched = await ReloadAsync(theirs.Id);

        Assert.NotEqual(ErasePersonalDataCommandHandler.RedactedName, untouched.FirstName);
        Assert.Equal("radnik@example.test", untouched.Email);

        var theirTrack = await InScope(scope => scope.Db.LocationRecords
            .IgnoreQueryFilters()
            .CountAsync(l => l.EmployeeId == theirs.Id));

        Assert.Equal(2, theirTrack);

        var theirDevices = await InScope(scope => scope.Db.DeviceTokens
            .CountAsync(d => d.UserId == theirUser.Id));

        Assert.Equal(1, theirDevices);
    }
}
