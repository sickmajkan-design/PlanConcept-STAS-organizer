using Construction.Application.Common.Exceptions;
using System.Text;
using Construction.Application.Features.Branches;
using Construction.Domain.Enums;

namespace Construction.IntegrationTests;

/// <summary>
/// The logo of a business unit: who may set it, what is accepted, the optional dark one, and the one
/// main unit whose logo stands for all units.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class BranchLogoTests : IntegrationTestBase
{
    public BranchLogoTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    private static string UniqueName(string prefix) => $"{prefix} {Guid.NewGuid():N}"[..28];

    private static async Task SignInAsync(TestScope scope, UserRole role)
    {
        var user = await TestData.SeedUserAsync(scope, role);
        scope.CurrentUser.SignInAs(user.Id, role, null, user.Email);
    }

    private async Task<BranchDto> CreateBranchAsync()
    {
        return await InScope(async scope =>
        {
            await SignInAsync(scope, UserRole.SuperAdmin);

            return await scope.Send(new CreateBranchCommand { Name = UniqueName("Logo"), Color = "#3457D5" });
        });
    }

    private static UploadBranchLogoCommand Upload(Guid id, string fileName = "logo.png", bool dark = false, long size = 4) => new()
    {
        Id = id,
        Variant = dark ? BranchLogoVariant.Dark : BranchLogoVariant.Light,
        FileName = fileName,
        SizeBytes = size,
        Content = new MemoryStream([1, 2, 3, 4]),
    };

    [Fact]
    public async Task Admin_sets_a_logo_and_everyone_can_read_that_it_exists()
    {
        var branch = await CreateBranchAsync();
        Assert.False(branch.HasLogo);

        var updated = await InScope(async scope =>
        {
            await SignInAsync(scope, UserRole.Admin);

            return await scope.Send(Upload(branch.Id));
        });

        Assert.True(updated.HasLogo);
        Assert.False(updated.HasDarkLogo);
        Assert.NotNull(updated.LogoVersion);

        var listed = await InScope(async scope =>
        {
            await SignInAsync(scope, UserRole.Foreman);

            return (await scope.Send(new GetBranchesQuery())).Single(b => b.Id == branch.Id);
        });

        Assert.True(listed.HasLogo);
    }

    [Fact]
    public async Task A_foreman_may_not_change_a_logo()
    {
        var branch = await CreateBranchAsync();

        await InScope(async scope =>
        {
            await SignInAsync(scope, UserRole.Foreman);

            await Assert.ThrowsAsync<ForbiddenAccessException>(() => scope.Send(Upload(branch.Id)));
        });
    }

    [Fact]
    public async Task Only_png_jpg_webp_and_svg_of_at_most_two_megabytes_are_taken()
    {
        var branch = await CreateBranchAsync();

        await InScope(async scope =>
        {
            await SignInAsync(scope, UserRole.SuperAdmin);

            await Assert.ThrowsAsync<ValidationException>(() => scope.Send(Upload(branch.Id, "logo.gif")));
            await Assert.ThrowsAsync<ValidationException>(() => scope.Send(Upload(branch.Id, "logo.pdf")));
            await Assert.ThrowsAsync<ValidationException>(() => scope.Send(Upload(branch.Id, size: 3 * 1024 * 1024)));
        });
    }

    [Fact]
    public async Task The_dark_logo_is_optional_and_removing_the_light_one_removes_both()
    {
        var branch = await CreateBranchAsync();

        await InScope(async scope =>
        {
            await SignInAsync(scope, UserRole.SuperAdmin);

            await scope.Send(Upload(branch.Id));
            var both = await scope.Send(Upload(branch.Id, "dark.webp", dark: true));
            Assert.True(both.HasLogo);
            Assert.True(both.HasDarkLogo);

            var darkGone = await scope.Send(new DeleteBranchLogoCommand(branch.Id, BranchLogoVariant.Dark));
            Assert.True(darkGone.HasLogo);
            Assert.False(darkGone.HasDarkLogo);

            await scope.Send(Upload(branch.Id, "dark.webp", dark: true));
            var none = await scope.Send(new DeleteBranchLogoCommand(branch.Id, BranchLogoVariant.Light));
            Assert.False(none.HasLogo);
            Assert.False(none.HasDarkLogo);
        });
    }

    [Fact]
    public async Task A_dark_request_falls_back_to_the_light_logo()
    {
        var branch = await CreateBranchAsync();

        await InScope(async scope =>
        {
            await SignInAsync(scope, UserRole.SuperAdmin);
            await scope.Send(Upload(branch.Id));

            var dark = await scope.Send(new GetBranchLogoQuery(branch.Id, BranchLogoVariant.Dark));

            Assert.NotNull(dark);
            Assert.Equal("image/png", dark.ContentType);
            Assert.Null(await scope.Send(new GetBranchLogoQuery(Guid.NewGuid(), BranchLogoVariant.Light)));
        });
    }

    [Fact]
    public async Task Only_one_unit_is_main_and_the_new_one_replaces_the_old()
    {
        var first = await CreateBranchAsync();
        var second = await CreateBranchAsync();

        await InScope(async scope =>
        {
            await SignInAsync(scope, UserRole.Admin);

            await scope.Send(new SetMainBranchCommand(first.Id));
            var list = await scope.Send(new SetMainBranchCommand(second.Id));

            Assert.False(list.Single(b => b.Id == first.Id).IsMain);
            Assert.True(list.Single(b => b.Id == second.Id).IsMain);
            Assert.Single(list, b => b.IsMain);
        });
    }

    [Fact]
    public async Task Switching_the_main_unit_off_makes_it_not_main_and_a_foreman_cannot_choose_one()
    {
        var branch = await CreateBranchAsync();

        await InScope(async scope =>
        {
            await SignInAsync(scope, UserRole.SuperAdmin);
            await scope.Send(new SetMainBranchCommand(branch.Id));

            var off = await scope.Send(new UpdateBranchCommand
            {
                Id = branch.Id,
                Name = branch.Name,
                Color = branch.Color,
                IsActive = false,
            });
            Assert.False(off.IsMain);

            await Assert.ThrowsAsync<ConflictException>(() => scope.Send(new SetMainBranchCommand(branch.Id)));
        });

        await InScope(async scope =>
        {
            await SignInAsync(scope, UserRole.Foreman);

            await Assert.ThrowsAsync<ForbiddenAccessException>(() => scope.Send(new SetMainBranchCommand(branch.Id)));
        });
    }

    private const string Svg =
        "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 10 10\"><script>alert(1)</script><rect width=\"8\" height=\"8\" onclick=\"x()\"/></svg>";

    private static UploadBranchLogoCommand UploadSvg(Guid id, bool withRaster)
    {
        var bytes = Encoding.UTF8.GetBytes(Svg);

        return new UploadBranchLogoCommand
        {
            Id = id,
            Variant = BranchLogoVariant.Light,
            FileName = "logo.svg",
            SizeBytes = bytes.Length,
            Content = new MemoryStream(bytes),
            RasterContent = withRaster ? new MemoryStream([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3]) : null,
        };
    }

    [Fact]
    public async Task An_svg_logo_is_cleaned_and_its_png_copy_is_what_exports_and_the_phone_draw()
    {
        var branch = await CreateBranchAsync();

        await InScope(async scope =>
        {
            await SignInAsync(scope, UserRole.Admin);

            var updated = await scope.Send(UploadSvg(branch.Id, withRaster: true));
            Assert.True(updated.HasLogo);

            var served = await scope.Send(new GetBranchLogoQuery(branch.Id, BranchLogoVariant.Light));
            Assert.NotNull(served);
            Assert.Equal("image/svg+xml", served.ContentType);
            using var reader = new StreamReader(served.Content);
            var text = await reader.ReadToEndAsync();
            Assert.DoesNotContain("script", text);
            Assert.DoesNotContain("onclick", text);
            Assert.Contains("<rect", text);

            var picture = await scope.Send(new GetBranchLogoQuery(branch.Id, BranchLogoVariant.Light, Raster: true));
            Assert.NotNull(picture);
            Assert.Equal("image/png", picture.ContentType);
        });
    }

    [Fact]
    public async Task An_svg_without_a_png_copy_has_no_picture_for_exports_and_a_broken_one_is_refused()
    {
        var branch = await CreateBranchAsync();

        await InScope(async scope =>
        {
            await SignInAsync(scope, UserRole.Admin);
            await scope.Send(UploadSvg(branch.Id, withRaster: false));

            Assert.Null(await scope.Send(new GetBranchLogoQuery(branch.Id, BranchLogoVariant.Light, Raster: true)));

            var broken = UploadSvg(branch.Id, withRaster: false) with { Content = new MemoryStream(Encoding.UTF8.GetBytes("not an svg")) };
            await Assert.ThrowsAsync<ConflictException>(() => scope.Send(broken));
        });
    }

    [Fact]
    public async Task The_data_of_a_unit_is_readable_by_everyone_with_a_panel_account()
    {
        var branch = await InScope(async scope =>
        {
            await SignInAsync(scope, UserRole.SuperAdmin);

            return await scope.Send(new CreateBranchCommand
            {
                Name = UniqueName("Open"),
                Color = "#3457D5",
                Address = "Zmaja od Bosne 12",
                City = "Sarajevo",
                TaxId = "4200000000000",
            });
        });

        var seen = await InScope(async scope =>
        {
            await SignInAsync(scope, UserRole.Foreman);

            return (await scope.Send(new GetBranchesQuery())).Single(b => b.Id == branch.Id);
        });

        Assert.Equal("Zmaja od Bosne 12", seen.Address);
        Assert.Equal("Sarajevo", seen.City);
        Assert.Equal("4200000000000", seen.TaxId);
    }
}
