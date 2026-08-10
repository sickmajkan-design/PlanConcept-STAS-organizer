using Construction.Domain.Entities;
using Construction.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Construction.IntegrationTests;

/// <summary>
/// <c>location_records</c> is partitioned by month, and stays that way.
/// </summary>
/// <remarks>
/// <para>
/// Partitioning buys one thing: retention becomes a catalogue change instead
/// of deleting a million rows a month. It charges one thing for it, and the
/// charge is the dangerous part — a partitioned table refuses a row it has no
/// partition for, so a maintenance job nobody noticed had stopped would turn
/// into rejected GPS pings on the busiest write path in the system.
/// </para>
/// <para>
/// So most of what is asserted here is about that refusal never happening: a
/// DEFAULT partition catches anything unforeseen, partitions are created ahead
/// of time, and a ping for a month nobody planned for is still stored and
/// still readable.
/// </para>
/// </remarks>
[Collection(DatabaseCollection.Name)]
public class LocationPartitionTests : IntegrationTestBase
{
    public LocationPartitionTests(DatabaseFixture fixture)
        : base(fixture)
    {
    }

    private static DateTime Utc(int year, int month, int day) =>
        new(year, month, day, 12, 0, 0, DateTimeKind.Utc);

    /// <summary>Which partition PostgreSQL actually put a row in.</summary>
    private static async Task<string?> PartitionHoldingAsync(
        ApplicationDbContext context,
        long id)
    {
        await using var command = context.Database.GetDbConnection().CreateCommand();

        command.CommandText = """
            SELECT c.relname
            FROM location_records lr
            JOIN pg_class c ON c.oid = lr.tableoid
            WHERE lr."Id" = @id
            """;

        var parameter = command.CreateParameter();
        parameter.ParameterName = "id";
        parameter.Value = id;
        command.Parameters.Add(parameter);

        await context.Database.OpenConnectionAsync();

        try
        {
            return await command.ExecuteScalarAsync() as string;
        }
        finally
        {
            await context.Database.CloseConnectionAsync();
        }
    }

    [Fact]
    public async Task The_table_is_partitioned_by_month_and_a_ping_lands_in_its_own_month()
    {
        var moment = Utc(2027, 3, 14);

        await InScope(async scope =>
        {
            var employee = await TestData.SeedEmployeeAsync(scope);

            await scope.LocationPartitions.EnsureAsync(
                moment, monthsAhead: 0, CancellationToken.None);

            var record = new LocationRecord
            {
                EmployeeId = employee.Id,
                Latitude = 43.85,
                Longitude = 18.41,
                Timestamp = moment,
                ReceivedAt = moment,
            };

            scope.Db.LocationRecords.Add(record);
            await scope.Db.SaveChangesAsync();

            Assert.Equal(
                "location_records_2027_03",
                await PartitionHoldingAsync(scope.Db, record.Id));
        });
    }

    /// <summary>
    /// The assertion the whole design hangs on: an unforeseen month is stored,
    /// not refused.
    /// </summary>
    /// <remarks>
    /// Without the DEFAULT partition this insert fails with "no partition of
    /// relation found for row", and a worker's day of movement is lost because
    /// a maintenance task did not run. It is worth a test of its own because
    /// nothing else in the suite would notice: every other test works in
    /// months that exist.
    /// </remarks>
    [Fact]
    public async Task A_ping_for_a_month_with_no_partition_is_still_stored()
    {
        // Far enough out that no sweep would ever have created it.
        var faraway = Utc(2099, 11, 5);

        await InScope(async scope =>
        {
            var employee = await TestData.SeedEmployeeAsync(scope);

            var record = new LocationRecord
            {
                EmployeeId = employee.Id,
                Latitude = 43.85,
                Longitude = 18.41,
                Timestamp = faraway,
                ReceivedAt = faraway,
            };

            scope.Db.LocationRecords.Add(record);
            await scope.Db.SaveChangesAsync();

            Assert.Equal(
                LocationPartitions.DefaultPartition,
                await PartitionHoldingAsync(scope.Db, record.Id));

            // And it reads back through the ordinary query, like any other ping.
            var readBack = await scope.Db.LocationRecords
                .IgnoreQueryFilters()
                .SingleAsync(r => r.Id == record.Id);

            Assert.Equal(faraway, readBack.Timestamp);
        });
    }

    [Fact]
    public async Task Partitions_are_created_for_the_months_ahead()
    {
        var now = Utc(2031, 1, 20);

        await InScope(scope =>
            scope.LocationPartitions.EnsureAsync(now, monthsAhead: 3, CancellationToken.None));

        // Last month through three ahead, so a phone that was offline over a
        // month boundary and a deployment left alone for a quarter both have
        // somewhere to put a row.
        foreach (var expected in new[]
                 {
                     "location_records_2030_12",
                     "location_records_2031_01",
                     "location_records_2031_02",
                     "location_records_2031_03",
                     "location_records_2031_04",
                 })
        {
            Assert.True(
                await RelationExistsAsync(expected),
                $"{expected} should have been created ahead of time.");
        }
    }

    [Fact]
    public async Task Running_it_twice_creates_nothing_the_second_time()
    {
        var now = Utc(2032, 6, 10);

        await InScope(scope =>
            scope.LocationPartitions.EnsureAsync(now, monthsAhead: 2, CancellationToken.None));

        // Every start-up and every sweep runs this, and two instances may run
        // it at the same moment.
        var second = await InScope(scope =>
            scope.LocationPartitions.EnsureAsync(now, monthsAhead: 2, CancellationToken.None));

        Assert.Equal(0, second);
    }

    [Fact]
    public async Task A_partition_entirely_older_than_the_cutoff_is_dropped()
    {
        var old = Utc(2021, 2, 10);

        await InScope(async scope =>
        {
            var employee = await TestData.SeedEmployeeAsync(scope);

            await scope.LocationPartitions.EnsureAsync(
                old, monthsAhead: 0, CancellationToken.None);

            scope.Db.LocationRecords.Add(new LocationRecord
            {
                EmployeeId = employee.Id,
                Latitude = 43.85,
                Longitude = 18.41,
                Timestamp = old,
                ReceivedAt = old,
            });

            await scope.Db.SaveChangesAsync();
        });

        Assert.True(await RelationExistsAsync("location_records_2021_02"));

        var dropped = await InScope(scope =>
            scope.LocationPartitions.DropExpiredAsync(Utc(2021, 4, 1), CancellationToken.None));

        Assert.Contains("location_records_2021_02", dropped);
        Assert.False(await RelationExistsAsync("location_records_2021_02"));
    }

    /// <summary>
    /// A partition that straddles the cutoff keeps everything, including the
    /// part that is past it.
    /// </summary>
    /// <remarks>
    /// Dropping it would delete rows inside the retention window — data the
    /// deployment has promised to keep — to save a row-level delete. The
    /// straddling month is the purge's job, not the drop's.
    /// </remarks>
    [Fact]
    public async Task A_partition_straddling_the_cutoff_is_left_alone()
    {
        var month = Utc(2022, 7, 15);

        await InScope(scope =>
            scope.LocationPartitions.EnsureAsync(month, monthsAhead: 0, CancellationToken.None));

        var dropped = await InScope(scope =>
            scope.LocationPartitions.DropExpiredAsync(Utc(2022, 7, 20), CancellationToken.None));

        Assert.DoesNotContain("location_records_2022_07", dropped);
        Assert.True(await RelationExistsAsync("location_records_2022_07"));
    }

    /// <summary>
    /// The DEFAULT partition is never dropped, whatever the cutoff.
    /// </summary>
    /// <remarks>
    /// It has no upper bound, so "everything in it is older than the cutoff"
    /// can never be established — and it is the table's safety net. Dropping
    /// it would turn every unforeseen month from a stored row into a rejected
    /// one.
    /// </remarks>
    [Fact]
    public async Task The_default_partition_survives_every_cutoff()
    {
        var dropped = await InScope(scope =>
            scope.LocationPartitions.DropExpiredAsync(Utc(2999, 1, 1), CancellationToken.None));

        Assert.DoesNotContain(LocationPartitions.DefaultPartition, dropped);
        Assert.True(await RelationExistsAsync(LocationPartitions.DefaultPartition));
    }

    /// <summary>Does a table or partition of this name exist?</summary>
    private Task<bool> RelationExistsAsync(string name) =>
        InScope(async scope =>
        {
            await using var command = scope.Db.Database.GetDbConnection().CreateCommand();
            command.CommandText = "SELECT to_regclass(@name) IS NOT NULL";

            var parameter = command.CreateParameter();
            parameter.ParameterName = "name";
            parameter.Value = name;
            command.Parameters.Add(parameter);

            await scope.Db.Database.OpenConnectionAsync();

            try
            {
                return await command.ExecuteScalarAsync() is true;
            }
            finally
            {
                await scope.Db.Database.CloseConnectionAsync();
            }
        });

    [Theory]
    [InlineData(null, null)]
    [InlineData("DEFAULT", null)]
    [InlineData("FOR VALUES FROM ('2026-04-01 00:00:00+00') TO ('2026-05-01 00:00:00+00')", "2026-05-01")]
    [InlineData("something this code has never seen", null)]
    public void An_unreadable_bound_is_treated_as_do_not_drop(string? bound, string? expected)
    {
        var parsed = LocationPartitions.UpperBoundOf(bound);

        if (expected is null)
        {
            Assert.Null(parsed);
        }
        else
        {
            Assert.Equal(DateTime.Parse(expected).Date, parsed!.Value.Date);
        }
    }
}
