using System.Text;
using Construction.Application.Common.Exceptions;
using Construction.Application.Features.Attachments;
using Construction.Application.Features.Attachments.Commands.UploadAttachment;
using Construction.Application.Features.Ledgers.Commands;
using Construction.Application.Features.Ledgers.Commands.CreateLedger;
using Construction.Application.Features.Ledgers.Commands.SetLedgerCell;
using Construction.Application.Features.Ledgers.Models;
using Construction.Application.Features.Ledgers.Queries.GetLedgerChecks;
using Construction.Application.Features.SignedTimesheets.Commands.GetOrCreateSignedTimesheet;
using Construction.Application.Features.SignedTimesheets.Queries.GetSignedTimesheetWeeks;
using Construction.Domain.Entities;
using Construction.Domain.Enums;

namespace Construction.IntegrationTests;

/// <summary>
/// The (project, calendar week) rows a client-signed timesheet scan is filed against —
/// A3 in the customer's answers. A row exists only so an attachment has somewhere to
/// hang; the interesting behaviour is that it is created lazily and reused, that only a
/// SuperAdmin may touch it, and that the ledger's checks notice when hours are typed for
/// a week nobody has filed a scan for.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class SignedTimesheetTests : IntegrationTestBase
{
    public SignedTimesheetTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    private static void ActAs(TestScope scope, User user) =>
        scope.CurrentUser.SignInAs(user.Id, user.Role, null, user.Email);

    // ---- get-or-create -----------------------------------------------------

    [Fact]
    public async Task The_same_project_and_week_always_resolves_to_the_same_row()
    {
        var project = await InScope(scope => TestData.SeedProjectAsync(scope));
        var superAdmin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.SuperAdmin));

        var first = await InScope(scope =>
        {
            ActAs(scope, superAdmin);
            return scope.Send(new GetOrCreateSignedTimesheetCommand
            {
                ProjectId = project.Id,
                Year = 2026,
                IsoWeek = 36,
            });
        });

        var second = await InScope(scope =>
        {
            ActAs(scope, superAdmin);
            return scope.Send(new GetOrCreateSignedTimesheetCommand
            {
                ProjectId = project.Id,
                Year = 2026,
                IsoWeek = 36,
            });
        });

        Assert.Equal(first, second);
    }

    [Fact]
    public async Task Different_weeks_get_different_rows()
    {
        var project = await InScope(scope => TestData.SeedProjectAsync(scope));
        var superAdmin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.SuperAdmin));

        var week36 = await InScope(scope =>
        {
            ActAs(scope, superAdmin);
            return scope.Send(new GetOrCreateSignedTimesheetCommand
            {
                ProjectId = project.Id,
                Year = 2026,
                IsoWeek = 36,
            });
        });

        var week37 = await InScope(scope =>
        {
            ActAs(scope, superAdmin);
            return scope.Send(new GetOrCreateSignedTimesheetCommand
            {
                ProjectId = project.Id,
                Year = 2026,
                IsoWeek = 37,
            });
        });

        Assert.NotEqual(week36, week37);
    }

    [Fact]
    public async Task Resolving_against_a_project_that_does_not_exist_is_a_404()
    {
        var superAdmin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.SuperAdmin));

        await Assert.ThrowsAsync<NotFoundException>(() => InScope(scope =>
        {
            ActAs(scope, superAdmin);
            return scope.Send(new GetOrCreateSignedTimesheetCommand
            {
                ProjectId = Guid.NewGuid(),
                Year = 2026,
                IsoWeek = 36,
            });
        }));
    }

    // ---- who may touch the scan --------------------------------------------

    [Fact]
    public async Task An_administrator_cannot_upload_a_signed_timesheet()
    {
        // Same door as the ledger it backs: Admin sees the rest of the cost
        // records, but not this one.
        var project = await InScope(scope => TestData.SeedProjectAsync(scope));
        var superAdmin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.SuperAdmin));
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));

        var rowId = await InScope(scope =>
        {
            ActAs(scope, superAdmin);
            return scope.Send(new GetOrCreateSignedTimesheetCommand
            {
                ProjectId = project.Id,
                Year = 2026,
                IsoWeek = 36,
            });
        });

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => InScope(scope =>
        {
            ActAs(scope, admin);
            var bytes = new MemoryStream(Encoding.UTF8.GetBytes("satnica"));

            return scope.Send(new UploadAttachmentCommand
            {
                OwnerType = AttachmentOwnerType.SignedTimesheet,
                OwnerId = rowId,
                Category = AttachmentCategory.Other,
                FileName = "satnica.pdf",
                SizeBytes = bytes.Length,
                Content = bytes,
            });
        }));
    }

    [Fact]
    public async Task A_super_admin_can_upload_and_read_a_signed_timesheet()
    {
        var project = await InScope(scope => TestData.SeedProjectAsync(scope));
        var superAdmin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.SuperAdmin));

        var rowId = await InScope(scope =>
        {
            ActAs(scope, superAdmin);
            return scope.Send(new GetOrCreateSignedTimesheetCommand
            {
                ProjectId = project.Id,
                Year = 2026,
                IsoWeek = 36,
            });
        });

        var uploaded = await InScope(scope =>
        {
            ActAs(scope, superAdmin);
            var bytes = new MemoryStream(Encoding.UTF8.GetBytes("satnica"));

            return scope.Send(new UploadAttachmentCommand
            {
                OwnerType = AttachmentOwnerType.SignedTimesheet,
                OwnerId = rowId,
                Category = AttachmentCategory.Other,
                FileName = "satnica.pdf",
                SizeBytes = bytes.Length,
                Content = bytes,
            });
        });

        Assert.Equal(rowId, uploaded.OwnerId);
    }

    // ---- which weeks have a scan --------------------------------------------

    [Fact]
    public async Task A_week_with_no_upload_is_reported_as_not_having_one()
    {
        var project = await InScope(scope => TestData.SeedProjectAsync(scope));
        var superAdmin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.SuperAdmin));

        var weeks = await InScope(scope =>
        {
            ActAs(scope, superAdmin);
            return scope.Send(new GetSignedTimesheetWeeksQuery
            {
                ProjectId = project.Id,
                Year = 2026,
                Month = 9,
            });
        });

        Assert.NotEmpty(weeks);
        Assert.All(weeks, w => Assert.False(w.HasAttachment));
        Assert.All(weeks, w => Assert.Null(w.SignedTimesheetId));
    }

    [Fact]
    public async Task Uploading_a_scan_marks_that_weeks_row_and_no_other()
    {
        var project = await InScope(scope => TestData.SeedProjectAsync(scope));
        var superAdmin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.SuperAdmin));

        var weeksBefore = await InScope(scope =>
        {
            ActAs(scope, superAdmin);
            return scope.Send(new GetSignedTimesheetWeeksQuery
            {
                ProjectId = project.Id,
                Year = 2026,
                Month = 9,
            });
        });

        var target = weeksBefore[0];

        var rowId = await InScope(scope =>
        {
            ActAs(scope, superAdmin);
            return scope.Send(new GetOrCreateSignedTimesheetCommand
            {
                ProjectId = project.Id,
                Year = target.IsoYear,
                IsoWeek = target.IsoWeek,
            });
        });

        await InScope(scope =>
        {
            ActAs(scope, superAdmin);
            var bytes = new MemoryStream(Encoding.UTF8.GetBytes("satnica"));

            return scope.Send(new UploadAttachmentCommand
            {
                OwnerType = AttachmentOwnerType.SignedTimesheet,
                OwnerId = rowId,
                Category = AttachmentCategory.Other,
                FileName = "satnica.pdf",
                SizeBytes = bytes.Length,
                Content = bytes,
            });
        });

        var weeksAfter = await InScope(scope =>
        {
            ActAs(scope, superAdmin);
            return scope.Send(new GetSignedTimesheetWeeksQuery
            {
                ProjectId = project.Id,
                Year = 2026,
                Month = 9,
            });
        });

        var flagged = weeksAfter.Single(w => w.IsoYear == target.IsoYear && w.IsoWeek == target.IsoWeek);
        Assert.True(flagged.HasAttachment);

        Assert.All(
            weeksAfter.Where(w => !(w.IsoYear == target.IsoYear && w.IsoWeek == target.IsoWeek)),
            w => Assert.False(w.HasAttachment));
    }

    // ---- the ledger check ----------------------------------------------------

    private static string Unique() => Guid.NewGuid().ToString("N")[..8];

    [Fact]
    public async Task Hours_typed_for_a_week_with_no_signed_scan_are_flagged()
    {
        var project = await InScope(scope => TestData.SeedProjectAsync(scope, $"Gradiliste {Unique()}"));
        var superAdmin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.SuperAdmin));

        var ledger = await InScope(scope =>
        {
            ActAs(scope, superAdmin);
            return scope.Send(new CreateLedgerCommand
            {
                Name = $"Obračun {Unique()}",
                Year = 2026,
                Month = 9,
                Template = LedgerTemplates.Payroll,
            });
        });

        var section = await InScope(scope =>
        {
            ActAs(scope, superAdmin);
            return scope.Send(new AddLedgerSectionCommand
            {
                LedgerId = ledger.Id,
                Name = project.Name,
                ProjectId = project.Id,
            });
        });

        var row = await InScope(scope =>
        {
            ActAs(scope, superAdmin);
            return scope.Send(new AddLedgerRowCommand
            {
                SectionId = section.Id,
                Label = "Radnik",
            });
        });

        var weekColumnId = ledger.Columns.Single(c => c.SystemKey == LedgerTemplates.Keys.Week(1)).Id;

        await InScope(scope =>
        {
            ActAs(scope, superAdmin);
            return scope.Send(new SetLedgerCellCommand
            {
                RowId = row.Id,
                ColumnId = weekColumnId,
                Value = "40",
            });
        });

        var checks = await InScope(scope =>
        {
            ActAs(scope, superAdmin);
            return scope.Send(new GetLedgerChecksQuery { LedgerId = ledger.Id });
        });

        Assert.Contains(checks, c =>
            c.Kind == LedgerCheckKinds.MissingSignedTimesheet && c.SectionId == section.Id);
    }

    [Fact]
    public async Task Filing_the_scan_clears_the_flag_for_that_week()
    {
        var project = await InScope(scope => TestData.SeedProjectAsync(scope, $"Gradiliste {Unique()}"));
        var superAdmin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.SuperAdmin));

        var ledger = await InScope(scope =>
        {
            ActAs(scope, superAdmin);
            return scope.Send(new CreateLedgerCommand
            {
                Name = $"Obračun {Unique()}",
                Year = 2026,
                Month = 9,
                Template = LedgerTemplates.Payroll,
            });
        });

        var section = await InScope(scope =>
        {
            ActAs(scope, superAdmin);
            return scope.Send(new AddLedgerSectionCommand
            {
                LedgerId = ledger.Id,
                Name = project.Name,
                ProjectId = project.Id,
            });
        });

        var row = await InScope(scope =>
        {
            ActAs(scope, superAdmin);
            return scope.Send(new AddLedgerRowCommand
            {
                SectionId = section.Id,
                Label = "Radnik",
            });
        });

        var weekColumnId = ledger.Columns.Single(c => c.SystemKey == LedgerTemplates.Keys.Week(1)).Id;

        await InScope(scope =>
        {
            ActAs(scope, superAdmin);
            return scope.Send(new SetLedgerCellCommand
            {
                RowId = row.Id,
                ColumnId = weekColumnId,
                Value = "40",
            });
        });

        var week = LedgerTemplates.MonthWeeks(2026, 9)[0];

        var timesheetId = await InScope(scope =>
        {
            ActAs(scope, superAdmin);
            return scope.Send(new GetOrCreateSignedTimesheetCommand
            {
                ProjectId = project.Id,
                Year = week.IsoYear,
                IsoWeek = week.IsoWeek,
            });
        });

        await InScope(scope =>
        {
            ActAs(scope, superAdmin);
            var bytes = new MemoryStream(Encoding.UTF8.GetBytes("satnica"));

            return scope.Send(new UploadAttachmentCommand
            {
                OwnerType = AttachmentOwnerType.SignedTimesheet,
                OwnerId = timesheetId,
                Category = AttachmentCategory.Other,
                FileName = "satnica.pdf",
                SizeBytes = bytes.Length,
                Content = bytes,
            });
        });

        var checks = await InScope(scope =>
        {
            ActAs(scope, superAdmin);
            return scope.Send(new GetLedgerChecksQuery { LedgerId = ledger.Id });
        });

        Assert.DoesNotContain(checks, c => c.Kind == LedgerCheckKinds.MissingSignedTimesheet);
    }
}
