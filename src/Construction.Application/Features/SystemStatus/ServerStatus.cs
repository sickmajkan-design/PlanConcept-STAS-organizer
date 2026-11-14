using Construction.Application.Common.Interfaces;
using Construction.Application.Features.StorageCleanup;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.SystemStatus;

/// <summary>The machine the platform runs on, as the API can see it. Every number is null when it cannot be read.</summary>
public class HostStatusDto
{
    /// <summary>False when the host does not expose its figures (a development laptop on Windows, say).</summary>
    public bool Known { get; init; }

    public string Os { get; init; } = string.Empty;

    public long? UptimeSeconds { get; init; }

    public int Cores { get; init; }

    public double? Load1 { get; init; }

    public double? Load5 { get; init; }

    public double? Load15 { get; init; }

    public long? MemoryTotalBytes { get; init; }

    public long? MemoryAvailableBytes { get; init; }

    public long? SwapTotalBytes { get; init; }

    public long? SwapFreeBytes { get; init; }
}

/// <summary>The API process itself.</summary>
public class ApplicationStatusDto
{
    public DateTime StartedAtUtc { get; init; }

    public long UptimeSeconds { get; init; }

    public string Version { get; init; } = string.Empty;

    public string Environment { get; init; } = string.Empty;

    public string Runtime { get; init; } = string.Empty;

    public long ProcessMemoryBytes { get; init; }

    public long ManagedHeapBytes { get; init; }

    public int Threads { get; init; }
}

public class TableSizeDto
{
    public string Name { get; init; } = string.Empty;

    public long Bytes { get; init; }
}

public class DatabaseStatusDto
{
    public bool Reachable { get; init; }

    public string? Version { get; init; }

    public long SizeBytes { get; init; }

    public int Connections { get; init; }

    public int ActiveConnections { get; init; }

    public int MaxConnections { get; init; }

    /// <summary>Share of reads served from memory, the usual sign that the database has enough of it. Null before any traffic.</summary>
    public double? CacheHitPercent { get; init; }

    public List<TableSizeDto> LargestTables { get; init; } = [];
}

/// <summary>What the platform leans on, besides the database.</summary>
public class ServicesStatusDto
{
    public bool VirusScannerEnabled { get; init; }

    public bool VirusScannerReachable { get; init; }

    /// <summary>Where uploaded files live: <c>disk</c> or <c>objectStorage</c>.</summary>
    public string FileStorage { get; init; } = "disk";

    /// <summary>Messages (e-mail, push) waiting to be sent.</summary>
    public int OutboxPending { get; init; }

    /// <summary>Messages given up on after repeated failures: somebody should look.</summary>
    public int OutboxAbandoned { get; init; }

    public DateTime? OldestPendingUtc { get; init; }
}

/// <summary>One backup: a dump, the documents archive taken with it, and whether a copy left the machine.</summary>
public class BackupItemDto
{
    public string Name { get; init; } = string.Empty;

    public DateTime TakenAtUtc { get; init; }

    /// <summary>Dump plus documents archive.</summary>
    public long SizeBytes { get; init; }

    public bool HasDocuments { get; init; }

    /// <summary>A verified copy exists off this machine (the receipt <c>offsite.sh</c> writes).</summary>
    public bool Offsite { get; init; }
}

/// <summary>What the backup folder says. Read-only; the backup container does the work.</summary>
public class BackupStatusDto
{
    /// <summary>False when the folder is not visible to the API (no volume mounted, or a development machine).</summary>
    public bool Known { get; init; }

    /// <summary>Newest first, at most 10.</summary>
    public List<BackupItemDto> Items { get; init; } = [];

    /// <summary>When the restore rehearsal last passed; null if it never has.</summary>
    public DateTime? RestoreCheckedAtUtc { get; init; }
}

/// <summary>Reads the folder the nightly backup writes to.</summary>
public interface IBackupCatalog
{
    BackupStatusDto Read();
}

public class ServerStatusDto
{
    public DateTime AtUtc { get; init; }

    public BackupStatusDto Backups { get; init; } = new();

    public HostStatusDto Host { get; init; } = new();

    public ApplicationStatusDto Application { get; init; } = new();

    public DatabaseStatusDto Database { get; init; } = new();

    public ServicesStatusDto Services { get; init; } = new();

    public StorageStatusDto Storage { get; init; } = new();
}

/// <summary>Reads what only the running process and the machine know. Implemented where the operating system is reachable.</summary>
public interface IServerProbe
{
    HostStatusDto ReadHost();

    ApplicationStatusDto ReadApplication();

    Task<DatabaseStatusDto> ReadDatabaseAsync(CancellationToken cancellationToken);

    /// <summary>Whether files live on the disk or in a bucket.</summary>
    string FileStorageKind { get; }
}

public record GetServerStatusQuery : IRequest<ServerStatusDto>;

public class GetServerStatusQueryHandler : IRequestHandler<GetServerStatusQuery, ServerStatusDto>
{
    private readonly IServerProbe _probe;
    private readonly IApplicationDbContext _context;
    private readonly IVirusScanner _scanner;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly ISender _sender;
    private readonly IBackupCatalog _backups;

    public GetServerStatusQueryHandler(
        IBackupCatalog backups,
        IServerProbe probe,
        IApplicationDbContext context,
        IVirusScanner scanner,
        ICurrentUserService currentUser,
        IDateTimeProvider clock,
        ISender sender)
    {
        _backups = backups;
        _probe = probe;
        _context = context;
        _scanner = scanner;
        _currentUser = currentUser;
        _clock = clock;
        _sender = sender;
    }

    public async Task<ServerStatusDto> Handle(GetServerStatusQuery request, CancellationToken cancellationToken)
    {
        StorageCleanupGuard.EnsureSuperAdmin(_currentUser);

        var database = await _probe.ReadDatabaseAsync(cancellationToken);
        var storage = await _sender.Send(new GetStorageStatusQuery(), cancellationToken);

        var pending = _context.OutboxMessages.AsNoTracking().Where(m => m.SentAt == null && m.AbandonedAt == null);

        var outbox = database.Reachable
            ? new
            {
                Pending = await pending.CountAsync(cancellationToken),
                Abandoned = await _context.OutboxMessages.AsNoTracking().CountAsync(m => m.AbandonedAt != null, cancellationToken),
                Oldest = await pending.OrderBy(m => m.CreatedAt).Select(m => (DateTime?)m.CreatedAt).FirstOrDefaultAsync(cancellationToken),
            }
            : new { Pending = 0, Abandoned = 0, Oldest = (DateTime?)null };

        return new ServerStatusDto
        {
            AtUtc = _clock.UtcNow,
            Backups = _backups.Read(),
            Host = _probe.ReadHost(),
            Application = _probe.ReadApplication(),
            Database = database,
            Storage = storage,
            Services = new ServicesStatusDto
            {
                VirusScannerEnabled = _scanner.IsEnabled,
                VirusScannerReachable = !_scanner.IsEnabled || await _scanner.IsReachableAsync(cancellationToken),
                FileStorage = _probe.FileStorageKind,
                OutboxPending = outbox.Pending,
                OutboxAbandoned = outbox.Abandoned,
                OldestPendingUtc = outbox.Oldest,
            },
        };
    }
}
