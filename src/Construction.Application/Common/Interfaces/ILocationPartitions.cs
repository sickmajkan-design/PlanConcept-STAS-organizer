namespace Construction.Application.Common.Interfaces;

/// <summary>
/// Keeps the monthly partitions of <c>location_records</c> in step with time.
/// </summary>
/// <remarks>
/// <para>
/// The table holds one row per employee per minute. Deleting a retention
/// window's worth of that a row at a time works, and is what the purge used to
/// do, but it writes as much WAL as the inserts did, leaves the table bloated
/// until autovacuum catches up, and takes longer every month. Dropping a
/// partition is a catalogue change: the same amount of data disappears in
/// milliseconds and gives the space straight back.
/// </para>
/// <para>
/// The price is that a partitioned table can only accept a row it has a
/// partition for. That turns a maintenance task nobody ran into rejected GPS
/// pings, so this interface exists to make sure the partitions are always
/// there before the rows are — and the table keeps a DEFAULT partition
/// underneath as the net for the case where they are not.
/// </para>
/// </remarks>
public interface ILocationPartitions
{
    /// <summary>
    /// Creates any missing monthly partitions from last month to
    /// <paramref name="monthsAhead"/> months out, and returns how many it made.
    /// </summary>
    /// <remarks>
    /// Ahead rather than on demand: a partition created at the moment the
    /// first ping of the month arrives is a partition created during a write,
    /// under a lock, on the busiest path in the system.
    /// </remarks>
    Task<int> EnsureAsync(DateTime utcNow, int monthsAhead, CancellationToken cancellationToken);

    /// <summary>
    /// Drops every partition whose whole range is older than
    /// <paramref name="cutoff"/>, and returns their names.
    /// </summary>
    /// <remarks>
    /// Whole range only. A partition that straddles the cutoff still holds
    /// rows that must be kept, so it is left alone and the row-level purge
    /// takes the expired part of it.
    /// </remarks>
    Task<IReadOnlyList<string>> DropExpiredAsync(DateTime cutoff, CancellationToken cancellationToken);

    /// <summary>
    /// How many rows have landed in the DEFAULT partition.
    /// </summary>
    /// <remarks>
    /// Should be zero. Anything else means a ping arrived for a month nobody
    /// had created a partition for — the net caught it, no data was lost, and
    /// something is wrong with the maintenance that wants looking at. Those
    /// rows are still purged by row, they simply cannot be dropped wholesale.
    /// </remarks>
    Task<long> CountInDefaultAsync(CancellationToken cancellationToken);
}
