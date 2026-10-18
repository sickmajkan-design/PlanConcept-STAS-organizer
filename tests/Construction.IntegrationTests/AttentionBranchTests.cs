using Construction.Application.Features.Attachments.Queries.GetExpiringDocuments;
using Construction.Application.Features.Attention;
using Construction.Domain.Entities;
using Construction.Domain.Enums;

namespace Construction.IntegrationTests;

/// <summary>
/// "What needs attention" and the expiring documents follow the business unit chosen in the header, like every
/// list they link to: a unit's view counts only what belongs to it, and the whole company's counts everything.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class AttentionBranchTests : IntegrationTestBase
{
    public AttentionBranchTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    private sealed record World(Branch Mine, Branch Other, Employee MyPerson, Employee OtherPerson, Project MySite, Project OtherSite);

    private static string Unique() => Guid.NewGuid().ToString("N")[..8];

    private Task<World> SeedAsync() => InScope(async scope =>
    {
        var mine = new Branch { Name = $"Mine {Unique()}", Color = "#3457D5" };
        var other = new Branch { Name = $"Other {Unique()}", Color = "#D55734" };
        scope.Db.Branches.AddRange(mine, other);

        var myPerson = await TestData.SeedEmployeeAsync(scope);
        var otherPerson = await TestData.SeedEmployeeAsync(scope);
        var mySite = await TestData.SeedProjectAsync(scope);
        var otherSite = await TestData.SeedProjectAsync(scope);
        await scope.Db.SaveChangesAsync();

        mySite.BranchId = mine.Id;
        otherSite.BranchId = other.Id;
        scope.Db.EmployeeBranches.Add(new EmployeeBranch { EmployeeId = myPerson.Id, BranchId = mine.Id, StartDate = new DateOnly(2026, 1, 1) });
        scope.Db.EmployeeBranches.Add(new EmployeeBranch { EmployeeId = otherPerson.Id, BranchId = other.Id, StartDate = new DateOnly(2026, 1, 1) });
        await scope.Db.SaveChangesAsync();

        return new World(mine, other, myPerson, otherPerson, mySite, otherSite);
    });

    private Task<AttentionDto> AttentionAsync(Guid? branchId) =>
        InScope(async scope =>
        {
            var admin = await TestData.SeedUserAsync(scope, UserRole.SuperAdmin);
            scope.CurrentUser.SignInAs(admin.Id, admin.Role, null, admin.Email);

            return await scope.Send(new GetAttentionQuery { BranchId = branchId });
        });

    private static int Count(AttentionDto dto, string key) => dto.Groups.SingleOrDefault(g => g.Key == key)?.Count ?? 0;

    [Fact]
    public async Task Leave_requests_orders_and_postings_are_counted_for_the_unit_they_belong_to()
    {
        var world = await SeedAsync();
        var requester = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));
        var wholeBefore = await AttentionAsync(null);

        await InScope(async scope =>
        {
            foreach (var person in new[] { world.MyPerson, world.OtherPerson })
            {
                scope.Db.Absences.Add(new Absence
                {
                    EmployeeId = person.Id,
                    Type = AbsenceType.AnnualLeave,
                    Status = AbsenceStatus.Requested,
                    StartDate = new DateOnly(2031, 5, 5),
                    EndDate = new DateOnly(2031, 5, 9),
                });
            }

            foreach (var site in new[] { world.MySite, world.OtherSite })
            {
                scope.Db.ArticleOrders.Add(new ArticleOrder { RequestedByUserId = requester.Id, ProjectId = site.Id, Status = ArticleOrderStatus.Requested });
            }

            // One posting per person, on their own unit's site and still to be confirmed.
            scope.Db.EmployeeProjects.Add(new EmployeeProject { EmployeeId = world.MyPerson.Id, ProjectId = world.MySite.Id, StartDate = new DateOnly(2026, 1, 1) });
            scope.Db.EmployeeProjects.Add(new EmployeeProject { EmployeeId = world.OtherPerson.Id, ProjectId = world.OtherSite.Id, StartDate = new DateOnly(2026, 1, 1) });
            await scope.Db.SaveChangesAsync();
        });

        var mine = await AttentionAsync(world.Mine.Id);
        var other = await AttentionAsync(world.Other.Id);
        var whole = await AttentionAsync(null);

        foreach (var key in new[] { "absenceRequests", "articleOrders", "unconfirmedPostings" })
        {
            Assert.Equal(1, Count(mine, key));
            Assert.Equal(1, Count(other, key));
            // The whole company counts both.
            Assert.Equal(Count(wholeBefore, key) + 2, Count(whole, key));
        }
    }

    [Fact]
    public async Task A_vehicles_dates_and_a_persons_certificates_are_counted_for_their_unit()
    {
        var world = await SeedAsync();
        var soon = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(5);

        await InScope(async scope =>
        {
            var vehicle = await TestData.SeedVehicleAsync(scope);
            vehicle.BranchId = world.Other.Id;
            vehicle.RegistrationValidUntil = soon;

            scope.Db.EmployeeCertificates.Add(new EmployeeCertificate { EmployeeId = world.MyPerson.Id, Name = "Scaffolding", ValidUntil = soon });
            await scope.Db.SaveChangesAsync();
        });

        var mine = await AttentionAsync(world.Mine.Id);
        var other = await AttentionAsync(world.Other.Id);

        Assert.Equal(0, Count(mine, "vehicleDates"));
        Assert.Equal(1, Count(other, "vehicleDates"));
        Assert.Equal(1, Count(mine, "certificatesExpiring"));
        Assert.Equal(0, Count(other, "certificatesExpiring"));
    }

    [Fact]
    public async Task Expiring_documents_are_those_of_the_units_people_and_sites()
    {
        var world = await SeedAsync();
        var uploader = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));
        var soon = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(5);

        Attachment Doc(string name) => new()
        {
            FileName = name,
            ContentType = "application/pdf",
            SizeBytes = 10,
            StorageKey = Guid.NewGuid().ToString("N"),
            ExpiresAt = soon,
            UploadedByUserId = uploader.Id,
        };

        var mineByPerson = Doc("mine-person.pdf");
        mineByPerson.EmployeeId = world.MyPerson.Id;
        var mineBySite = Doc("mine-site.pdf");
        mineBySite.ProjectId = world.MySite.Id;
        var other = Doc("other-person.pdf");
        other.EmployeeId = world.OtherPerson.Id;
        // A person who belongs to no unit: their document is in no unit's list.
        var nobody = Doc("nobody.pdf");

        await InScope(async scope =>
        {
            nobody.EmployeeId = (await TestData.SeedEmployeeAsync(scope)).Id;
            scope.Db.Attachments.AddRange(mineByPerson, mineBySite, other, nobody);
            await scope.Db.SaveChangesAsync();
        });

        Task<IReadOnlyList<string>> NamesAsync(Guid? branchId) => InScope(async scope =>
        {
            var admin = await TestData.SeedUserAsync(scope, UserRole.SuperAdmin);
            scope.CurrentUser.SignInAs(admin.Id, admin.Role, null, admin.Email);
            var documents = await scope.Send(new GetExpiringDocumentsQuery { WithinDays = 30, BranchId = branchId });

            return (IReadOnlyList<string>)documents.Select(d => d.FileName).ToList();
        });

        var inMine = await NamesAsync(world.Mine.Id);
        var inOther = await NamesAsync(world.Other.Id);
        var everything = await NamesAsync(null);

        Assert.Contains("mine-person.pdf", inMine);
        Assert.Contains("mine-site.pdf", inMine);
        Assert.DoesNotContain("other-person.pdf", inMine);
        Assert.DoesNotContain("nobody.pdf", inMine);

        Assert.Contains("other-person.pdf", inOther);
        Assert.DoesNotContain("mine-person.pdf", inOther);

        // Without a unit chosen, everything, including what belongs to no unit.
        Assert.Contains("nobody.pdf", everything);
        Assert.Contains("mine-person.pdf", everything);
        Assert.Contains("other-person.pdf", everything);
    }
}
