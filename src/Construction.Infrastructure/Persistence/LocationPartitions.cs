using System.Globalization;
using Construction.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Construction.Infrastructure.Persistence;

/// <summary>
/// The monthly partitions of <c>location_records</c>, in PostgreSQL.
/// </summary>
/// <remarks>
/// <para>
/// Raw SQL rather than anything through the model: partitions are DDL, EF has
/// no concept of them, and the names are derived here rather than supplied by
/// a caller — nothing from outside reaches the statement text.
/// </para>
/// </remarks>
public class LocationPartitions : ILocationPartitions
{
    /// <summary>The partitioned table itself.</summary>
    public const string ParentTable = "location_records";

    /// <summary>Where a row lands when its month has no partition.</summary>
    public const string DefaultPartition = "location_records_unpartitioned";

    private const string NamePrefix = "location_records_";

    private readonly ApplicationDbContext _context;
    private readonly ILogger<LocationPartitions> _logger;

    public LocationPartitions(ApplicationDbContext context, ILogger<LocationPartitions> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>The partition holding a given moment, e.g. location_records_2026_08.</summary>
    public static string NameFor(DateTime moment) =>
        $"{NamePrefix}{moment:yyyy}_{moment:MM}";

    private static DateTime StartOfMonth(DateTime moment) =>
        new(moment.Year, moment.Month, 1, 0, 0, 0, DateTimeKind.Utc);

    public async Task<int> EnsureAsync(
        DateTime utcNow,
        int monthsAhead,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(monthsAhead);

        var created = 0;

        // From last month, not this one. A deployment that starts on the first
        // of the month still receives pings recorded minutes earlier, on a
        // phone that was offline over the boundary.
        var month = StartOfMonth(utcNow).AddMonths(-1);
        var last = StartOfMonth(utcNow).AddMonths(monthsAhead);

        while (month <= last)
        {
            var name = NameFor(month);
            var from = month;
            var to = month.AddMonths(1);

            // IF NOT EXISTS, so this is safe to run on every start-up and from
            // every sweep, and safe when two instances run it at once.
            var sql = $"""
                CREATE TABLE IF NOT EXISTS "{name}"
                PARTITION OF "{ParentTable}"
                FOR VALUES FROM ('{from:yyyy-MM-dd}') TO ('{to:yyyy-MM-dd}')
                """;

            try
            {
                var before = await ExistsAsync(name, cancellationToken);

                await _context.Database.ExecuteSqlRawAsync(sql, cancellationToken);

                if (!before)
                {
                    created++;
                    _logger.LogInformation(
                        "Created location partition {Partition} for {From:yyyy-MM}.", name, from);
                }
            }
            catch (PostgresException exception)
            {
                // The one failure worth naming. Rows for this month already
                // sit in the DEFAULT partition, so PostgreSQL will not carve
                // the range out from under them. Nothing is lost — those rows
                // are still in the table and still purged by row — but this
                // month will not be droppable, and it needs a person.
                _logger.LogError(
                    exception,
                    "Could not create location partition {Partition}: the default partition "
                    + "already holds rows for {From:yyyy-MM}. Those pings are safe and still "
                    + "readable, but that month cannot be dropped as a unit until they are "
                    + "moved. See docs/PROVISIONING.md.",
                    name,
                    from);
            }

            month = month.AddMonths(1);
        }

        return created;
    }

    public async Task<IReadOnlyList<string>> DropExpiredAsync(
        DateTime cutoff,
        CancellationToken cancellationToken)
    {
        var dropped = new List<string>();

        foreach (var (name, upperBound) in await PartitionsAsync(cancellationToken))
        {
            // Strictly older: the upper bound is exclusive, so a partition
            // ending exactly at the cutoff holds nothing that must be kept.
            if (upperBound is null || upperBound > cutoff)
            {
                continue;
            }

            await _context.Database.ExecuteSqlRawAsync(
                $"DROP TABLE IF EXISTS \"{name}\"", cancellationToken);

            dropped.Add(name);

            _logger.LogInformation(
                "Dropped location partition {Partition}; everything in it was older than "
                + "{Cutoff:u}.", name, cutoff);
        }

        return dropped;
    }

    public async Task<long> CountInDefaultAsync(CancellationToken cancellationToken)
    {
        if (!await ExistsAsync(DefaultPartition, cancellationToken))
        {
            return 0;
        }

        await using var command = _context.Database.GetDbConnection().CreateCommand();

        command.CommandText = $"SELECT count(*) FROM \"{DefaultPartition}\"";

        await _context.Database.OpenConnectionAsync(cancellationToken);

        try
        {
            var result = await command.ExecuteScalarAsync(cancellationToken);

            return result is long count ? count : 0;
        }
        finally
        {
            await _context.Database.CloseConnectionAsync();
        }
    }

    private async Task<bool> ExistsAsync(string name, CancellationToken cancellationToken)
    {
        await using var command = _context.Database.GetDbConnection().CreateCommand();

        command.CommandText = "SELECT to_regclass(@name) IS NOT NULL";

        var parameter = command.CreateParameter();
        parameter.ParameterName = "name";
        parameter.Value = name;
        command.Parameters.Add(parameter);

        await _context.Database.OpenConnectionAsync(cancellationToken);

        try
        {
            return await command.ExecuteScalarAsync(cancellationToken) is true;
        }
        finally
        {
            await _context.Database.CloseConnectionAsync();
        }
    }

    /// <summary>
    /// Every partition of the table with the upper bound of its range, taken
    /// from the catalogue rather than from the name.
    /// </summary>
    /// <remarks>
    /// The name says which month it was made for; the bound says what it
    /// actually accepts, and dropping data is not a place to trust a naming
    /// convention. DEFAULT has no bound and comes back null, which is what
    /// keeps it from ever being dropped here.
    /// </remarks>
    private async Task<IReadOnlyList<(string Name, DateTime? UpperBound)>> PartitionsAsync(
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT child.relname,
                   pg_get_expr(child.relpartbound, child.oid)
            FROM pg_inherits
            JOIN pg_class parent ON parent.oid = pg_inherits.inhparent
            JOIN pg_class child  ON child.oid  = pg_inherits.inhrelid
            WHERE parent.relname = @parent
            """;

        var partitions = new List<(string, DateTime?)>();

        await using var command = _context.Database.GetDbConnection().CreateCommand();
        command.CommandText = sql;

        var parameter = command.CreateParameter();
        parameter.ParameterName = "parent";
        parameter.Value = ParentTable;
        command.Parameters.Add(parameter);

        await _context.Database.OpenConnectionAsync(cancellationToken);

        try
        {
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                partitions.Add((reader.GetString(0), UpperBoundOf(reader.GetString(1))));
            }
        }
        finally
        {
            await _context.Database.CloseConnectionAsync();
        }

        return partitions;
    }

    /// <summary>
    /// Reads the TO value out of "FOR VALUES FROM ('…') TO ('…')".
    /// </summary>
    /// <remarks>
    /// Null for DEFAULT, and null for anything this cannot parse — both mean
    /// "do not drop it", which is the safe answer to give about a table whose
    /// contents are not understood.
    /// </remarks>
    public static DateTime? UpperBoundOf(string? partitionBound)
    {
        if (string.IsNullOrWhiteSpace(partitionBound))
        {
            return null;
        }

        var to = partitionBound.LastIndexOf(" TO (", StringComparison.Ordinal);

        if (to < 0)
        {
            return null;
        }

        var opening = partitionBound.IndexOf('\'', to);
        var closing = opening < 0 ? -1 : partitionBound.IndexOf('\'', opening + 1);

        if (opening < 0 || closing < 0)
        {
            return null;
        }

        var value = partitionBound[(opening + 1)..closing];

        return DateTime.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
            out var parsed)
            ? parsed
            : null;
    }
}
