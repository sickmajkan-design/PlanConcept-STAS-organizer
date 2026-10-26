using Construction.Application.Common.Exceptions;
using Construction.Application.Features.Tools.Commands.AssignTool;
using Construction.Application.Features.Tools.Commands.CreateTool;
using Construction.Application.Features.Tools.Kits;
using Construction.Domain.Enums;

namespace Construction.IntegrationTests;

/// <summary>
/// A case of tools knows what is inside it, what each piece is worth, what has gone missing or broken, and what is
/// owed to or by the worker because of it.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class ToolKitTests : IntegrationTestBase
{
    public ToolKitTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    private async Task<Guid> NewKitAsync(string name = "Kofer E-1") =>
        (await InScope(scope => scope.Send(new CreateToolCommand { Name = name, IsKit = true }))).Id;

    private Task<ToolKitDto> AddAsync(Guid kit, string name, int quantity, decimal price) =>
        InScope(scope => scope.Send(new AddToolKitItemCommand { ToolId = kit, Name = name, Quantity = quantity, UnitPrice = price }));

    private Task<ToolKitDto> ActAsync(
        Guid kit,
        Guid item,
        ToolKitAction action,
        Guid? employee = null,
        decimal? amount = null,
        bool refund = true,
        ToolKitPayoutMethod? method = null) =>
        InScope(scope => scope.Send(new ActOnToolKitItemCommand
        {
            ToolId = kit,
            ItemId = item,
            Action = action,
            EmployeeId = employee,
            Amount = amount,
            Refund = refund,
            PayoutMethod = method,
        }));

    [Fact]
    public async Task The_case_adds_up_what_is_inside_it()
    {
        var kit = await NewKitAsync();
        await AddAsync(kit, "Makaze", 1, 18m);
        var result = await AddAsync(kit, "Odvijač", 2, 4m);

        Assert.Equal(26m, result.TotalValue);
        Assert.Equal(2, result.Items.Count);
        Assert.All(result.Items, i => Assert.Equal("InKit", i.State));
    }

    [Fact]
    public async Task A_missing_item_can_be_charged_to_the_worker_holding_the_case()
    {
        var (kit, worker) = await InScope(async scope => (
            (await scope.Send(new CreateToolCommand { Name = "Kofer E-2", IsKit = true })).Id,
            await TestData.SeedEmployeeAsync(scope)));
        await InScope(scope => scope.Send(new AssignToolToEmployeeCommand(kit, worker.Id)));
        var item = (await AddAsync(kit, "Kliješta", 1, 12m)).Items.Single().Id;

        await ActAsync(kit, item, ToolKitAction.ReportMissing);
        var charged = await ActAsync(kit, item, ToolKitAction.ChargeWorker);

        Assert.Equal(12m, charged.MissingValue);
        Assert.Equal(12m, charged.OpenCharges);
        var line = charged.Events.Single(e => e.Type == "ChargedToWorker");
        Assert.Equal(worker.Id, line.EmployeeId);

        var settled = await InScope(scope => scope.Send(new SettleToolKitEventCommand(kit, line.Id)));

        Assert.Equal(0m, settled.OpenCharges);
    }

    [Fact]
    public async Task A_broken_item_has_to_reach_the_office_before_it_is_written_off()
    {
        var kit = await NewKitAsync("Kofer E-3");
        var item = (await AddAsync(kit, "Tester", 1, 22m)).Items.Single().Id;

        var broken = await ActAsync(kit, item, ToolKitAction.ReportBroken);
        Assert.Equal(22m, broken.BrokenValue);

        await Assert.ThrowsAsync<ConflictException>(() => ActAsync(kit, item, ToolKitAction.WriteOff));

        await ActAsync(kit, item, ToolKitAction.ReceiveBroken);
        var written = await ActAsync(kit, item, ToolKitAction.WriteOff);

        Assert.Equal(22m, written.MissingValue);
        Assert.Equal(0m, written.OpenCharges);
    }

    [Fact]
    public async Task A_replacement_the_worker_bought_is_owed_back_and_puts_the_item_in_the_case()
    {
        var (kit, worker) = await InScope(async scope => (
            (await scope.Send(new CreateToolCommand { Name = "Kofer E-4", IsKit = true })).Id,
            await TestData.SeedEmployeeAsync(scope)));
        var item = (await AddAsync(kit, "Metar", 1, 6m)).Items.Single().Id;

        await ActAsync(kit, item, ToolKitAction.ReportMissing);

        // Nobody holds the case, so the worker has to be named.
        await Assert.ThrowsAsync<ConflictException>(() => ActAsync(kit, item, ToolKitAction.ReplacementBought, method: ToolKitPayoutMethod.Cash));

        var result = await ActAsync(kit, item, ToolKitAction.ReplacementBought, worker.Id, amount: 7.5m, method: ToolKitPayoutMethod.Cash);

        Assert.Equal("InKit", result.Items.Single().State);
        Assert.Equal(7.5m, result.OpenRefunds);
        Assert.Equal("Cash", result.Events.Single(e => e.Type == "ReplacementBought").PayoutMethod);
    }

    [Fact]
    public async Task A_refund_has_to_say_whether_it_is_paid_with_the_pay_or_in_cash()
    {
        var (kit, worker) = await InScope(async scope => (
            (await scope.Send(new CreateToolCommand { Name = "Kofer E-7", IsKit = true })).Id,
            await TestData.SeedEmployeeAsync(scope)));
        var item = (await AddAsync(kit, "Metar", 1, 6m)).Items.Single().Id;
        await ActAsync(kit, item, ToolKitAction.ReportMissing);

        await Assert.ThrowsAnyAsync<Exception>(() => ActAsync(kit, item, ToolKitAction.ReplacementBought, worker.Id));
    }

    [Fact]
    public async Task A_replacement_the_worker_caused_the_loss_of_is_not_refunded()
    {
        var (kit, worker) = await InScope(async scope => (
            (await scope.Send(new CreateToolCommand { Name = "Kofer E-8", IsKit = true })).Id,
            await TestData.SeedEmployeeAsync(scope)));
        var item = (await AddAsync(kit, "Metar", 1, 6m)).Items.Single().Id;
        await ActAsync(kit, item, ToolKitAction.ReportMissing);

        var result = await ActAsync(kit, item, ToolKitAction.ReplacementBought, worker.Id, refund: false);

        Assert.Equal("InKit", result.Items.Single().State);
        Assert.Equal(0m, result.OpenRefunds);
        var line = result.Events.Single(e => e.Type == "ReplacementNotRefunded");
        Assert.Null(line.Amount);
        Assert.Null(line.PayoutMethod);
    }

    [Fact]
    public async Task Copying_a_case_gives_each_copy_its_own_name_and_QR_code_and_keeps_what_is_inside()
    {
        var kit = await NewKitAsync("Kofer E-5");
        await AddAsync(kit, "Makaze", 1, 18m);
        await AddAsync(kit, "Odvijač", 2, 4m);

        var copies = await InScope(scope => scope.Send(new CopyToolCommand
        {
            ToolId = kit,
            Count = 3,
            NameTemplate = "Kofer E-{n}",
            StartNumber = 15,
            NumberWidth = 3,
        }));

        Assert.Equal(["Kofer E-015", "Kofer E-016", "Kofer E-017"], copies.Select(c => c.Name));
        Assert.Equal(3, copies.Select(c => c.QrCode).Distinct().Count());
        Assert.All(copies, c =>
        {
            Assert.True(c.IsKit);
            Assert.Equal(26m, c.KitTotalValue);
            Assert.Null(c.SerialNumber);
        });
    }

    [Fact]
    public async Task A_copy_can_leave_the_prices_out()
    {
        var kit = await NewKitAsync("Kofer E-6");
        await AddAsync(kit, "Makaze", 1, 18m);

        var copy = (await InScope(scope => scope.Send(new CopyToolCommand
        {
            ToolId = kit,
            NameTemplate = "Kofer E-{n}",
            StartNumber = 60,
            CopyPrices = false,
        }))).Single();

        Assert.Equal(1, copy.KitItemCount);
        Assert.Equal(0m, copy.KitTotalValue);
    }

    [Fact]
    public async Task A_plain_tool_has_no_list()
    {
        var tool = await InScope(scope => TestData.SeedToolAsync(scope));

        await Assert.ThrowsAsync<ConflictException>(() => AddAsync(tool.Id, "Makaze", 1, 18m));
    }
}
