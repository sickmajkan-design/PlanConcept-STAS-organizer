using Construction.Application.Common.Exceptions;
using Construction.Application.Features.Materials.Commands.AdjustMaterialQuantity;
using Microsoft.EntityFrameworkCore;

namespace Construction.IntegrationTests;

/// <summary>
/// Stock movements are applied as one conditional UPDATE that only fires while
/// the result stays non-negative. The concurrency test below is the reason
/// these run against PostgreSQL — an in-memory provider does not support
/// ExecuteUpdate at all, and would not serialise the writers either way.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class MaterialStockTests : IntegrationTestBase
{
    public MaterialStockTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    private Task<decimal> QuantityOf(Guid id) =>
        InScope(scope => scope.Db.Materials
            .Where(m => m.Id == id)
            .Select(m => m.Quantity)
            .SingleAsync());

    private Task AdjustAsync(Guid id, decimal change, string? reason = null) =>
        InScope(scope => scope.Send(new AdjustMaterialQuantityCommand
        {
            Id = id,
            Change = change,
            Reason = reason
        }));

    [Fact]
    public async Task Receiving_stock_adds_to_the_quantity()
    {
        var material = await InScope(scope => TestData.SeedMaterialAsync(scope, quantity: 100m));

        var result = await InScope(scope => scope.Send(new AdjustMaterialQuantityCommand
        {
            Id = material.Id,
            Change = 25m,
            Reason = "Delivery received"
        }));

        Assert.Equal(125m, result.Quantity);
        Assert.Equal(125m, await QuantityOf(material.Id));
    }

    [Fact]
    public async Task Consuming_stock_subtracts_from_the_quantity()
    {
        var material = await InScope(scope => TestData.SeedMaterialAsync(scope, quantity: 100m));

        await AdjustAsync(material.Id, -40m, "Used on site");

        Assert.Equal(60m, await QuantityOf(material.Id));
    }

    [Fact]
    public async Task Stock_may_be_taken_down_to_exactly_zero()
    {
        var material = await InScope(scope => TestData.SeedMaterialAsync(scope, quantity: 30m));

        await AdjustAsync(material.Id, -30m);

        Assert.Equal(0m, await QuantityOf(material.Id));
    }

    [Fact]
    public async Task An_adjustment_that_would_go_negative_is_refused_and_changes_nothing()
    {
        var material = await InScope(scope => TestData.SeedMaterialAsync(scope, quantity: 10m));

        await Assert.ThrowsAsync<ConflictException>(() => AdjustAsync(material.Id, -11m));

        Assert.Equal(10m, await QuantityOf(material.Id));
    }

    [Fact]
    public async Task Adjusting_a_material_that_does_not_exist_reports_not_found()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => AdjustAsync(Guid.NewGuid(), 5m));
    }

    [Fact]
    public async Task Fractional_quantities_survive_a_round_trip()
    {
        // The column is numeric(18,3); decimals must not be silently rounded.
        var material = await InScope(scope => TestData.SeedMaterialAsync(scope, quantity: 12.5m));

        await AdjustAsync(material.Id, 0.125m);

        Assert.Equal(12.625m, await QuantityOf(material.Id));
    }

    [Fact]
    public async Task Adjusting_bumps_the_last_updated_stamp()
    {
        var material = await InScope(scope => TestData.SeedMaterialAsync(scope, quantity: 10m));
        var before = await InScope(scope => scope.Db.Materials
            .Where(m => m.Id == material.Id)
            .Select(m => m.LastUpdated)
            .SingleAsync());

        await Task.Delay(10);
        await AdjustAsync(material.Id, 1m);

        var after = await InScope(scope => scope.Db.Materials
            .Where(m => m.Id == material.Id)
            .Select(m => m.LastUpdated)
            .SingleAsync());

        Assert.True(after > before, $"Expected LastUpdated to advance, was {before} then {after}.");
    }

    [Fact]
    public async Task Concurrent_withdrawals_can_never_oversell_the_stock()
    {
        // Ten crews each try to take 10 from a stock of 50 at the same time.
        // Exactly five may succeed; the rest must be refused, and the stock
        // must land on zero rather than going negative.
        var material = await InScope(scope => TestData.SeedMaterialAsync(scope, quantity: 50m));

        var attempts = Enumerable.Range(0, 10).Select(async _ =>
        {
            try
            {
                await AdjustAsync(material.Id, -10m);
                return true;
            }
            catch (ConflictException)
            {
                return false;
            }
        });

        var results = await Task.WhenAll(attempts);

        Assert.Equal(5, results.Count(succeeded => succeeded));
        Assert.Equal(0m, await QuantityOf(material.Id));
    }

    [Fact]
    public async Task Concurrent_deliveries_all_land()
    {
        // Additions have no guard to trip, so none may be lost to a race.
        var material = await InScope(scope => TestData.SeedMaterialAsync(scope, quantity: 0m));

        await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => AdjustAsync(material.Id, 3m)));

        Assert.Equal(30m, await QuantityOf(material.Id));
    }
}
