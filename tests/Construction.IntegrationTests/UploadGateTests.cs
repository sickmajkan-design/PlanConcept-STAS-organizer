using Construction.Application.Common.Exceptions;
using Construction.Application.Features.WeeklySiteReports.Commands.CreateWeeklySiteReport;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.IntegrationTests;

/// <summary>
/// A weekly report's pictures are opened by the office like any document, so they go through the same two checks as
/// every other upload: the content has to be what the name says, and the antivirus has to pass it.
/// </summary>
/// <remarks>
/// Before this, the report was the one upload that skipped both: a program named .pdf, a virus named .txt and any file at
/// all while the antivirus was down were stored and handed to the office.
/// </remarks>
[Collection(DatabaseCollection.Name)]
public class UploadGateTests : IntegrationTestBase
{
    public UploadGateTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    private async Task<(Project Project, Employee Employee, User Office)> SeedAsync()
    {
        var project = await InScope(scope => TestData.SeedProjectAsync(scope, $"Gate {Guid.NewGuid():N}"[..14]));
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var office = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));

        return (project, employee, office);
    }

    private Task<Guid> SendAsync(
        (Project Project, Employee Employee, User Office) world, string fileName, byte[] bytes, Action<TestScope>? before = null) =>
        InScope(async scope =>
        {
            scope.CurrentUser.SignInAs(world.Office.Id, world.Office.Role, null, world.Office.Email);
            before?.Invoke(scope);

            var created = await scope.Send(new CreateWeeklySiteReportCommand
            {
                ProjectId = world.Project.Id,
                IsoYear = 2026,
                IsoWeek = 10,
                Type = WeeklyReportType.SignedHours,
                SubmittedByEmployeeId = world.Employee.Id,
                Files = [new ReportFileInput(fileName, bytes.Length, new MemoryStream(bytes))],
                Lines = [],
            });

            return created.Id;
        });

    private Task<int> PagesAsync(Guid projectId) =>
        InScope(scope => scope.Db.WeeklySiteReportPages.CountAsync(p => p.Report.ProjectId == projectId));

    [Fact]
    public async Task A_picture_that_is_what_its_name_says_is_stored()
    {
        var world = await SeedAsync();

        await SendAsync(world, "satnica.jpg", TestFiles.WithSignature("satnica.jpg", string.Empty));

        Assert.Equal(1, await PagesAsync(world.Project.Id));
    }

    [Theory]
    [InlineData("junk.pdf", "MZ this is a program, not a pdf")]
    [InlineData("page.png", "<html><script>alert(document.domain)</script></html>")]
    public async Task A_file_whose_content_is_not_its_name_is_refused_and_nothing_is_stored(string fileName, string content)
    {
        var world = await SeedAsync();

        await Assert.ThrowsAsync<ConflictException>(() =>
            SendAsync(world, fileName, System.Text.Encoding.UTF8.GetBytes(content)));

        Assert.Equal(0, await PagesAsync(world.Project.Id));
    }

    [Fact]
    public async Task A_file_the_virus_scan_recognises_is_refused()
    {
        var world = await SeedAsync();

        await Assert.ThrowsAsync<ConflictException>(() =>
            SendAsync(world, "eicar.txt", System.Text.Encoding.UTF8.GetBytes(FakeVirusScanner.Eicar)));

        Assert.Equal(0, await PagesAsync(world.Project.Id));
    }

    [Fact]
    public async Task While_the_virus_scan_is_down_nothing_is_accepted()
    {
        var world = await SeedAsync();

        try
        {
            await Assert.ThrowsAsync<ServiceUnavailableException>(() =>
                SendAsync(world, "satnica.jpg", TestFiles.WithSignature("satnica.jpg", string.Empty), scope => scope.VirusScanner.Down = true));

            Assert.Equal(0, await PagesAsync(world.Project.Id));
        }
        finally
        {
            // One scanner is shared by every test of the collection.
            await InScope(scope =>
            {
                scope.VirusScanner.Down = false;
                return Task.CompletedTask;
            });
        }
    }
}
