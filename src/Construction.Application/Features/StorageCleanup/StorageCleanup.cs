using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Construction.Application.Features.StorageCleanup;

/// <summary>What can be cleaned off the server by hand.</summary>
public enum StorageCleanupKind
{
    /// <summary>Files in the store that no record points at: left by a failed delete or a restore from an old backup.</summary>
    OrphanFiles = 1,

    /// <summary>Documents still filed on a person, project, vehicle, tool, work item or accommodation that has been deleted.</summary>
    DeletedRecordDocuments = 2,

    /// <summary>The rows kept as a trace that a deleted document once existed (its file is already gone).</summary>
    DeletionTraces = 3
}

public class StorageCleanupCategoryDto
{
    public StorageCleanupKind Kind { get; init; }

    public int Count { get; init; }

    public long Bytes { get; init; }

    /// <summary>A few names or keys, so the person can see what they are about to remove.</summary>
    public List<string> Examples { get; init; } = [];

    /// <summary>Documents that would be included but must be kept: a retention date is still in the future.</summary>
    public int KeptForRetention { get; init; }
}

public class StorageCleanupDto
{
    public List<StorageCleanupCategoryDto> Categories { get; init; } = [];

    /// <summary>Files younger than this are never called orphans: an upload saves the bytes before the row.</summary>
    public int OrphanGraceHours { get; init; }
}

public class StorageCleanupResultDto
{
    public StorageCleanupKind Kind { get; init; }

    public int RemovedFiles { get; init; }

    public int RemovedRows { get; init; }

    public long FreedBytes { get; init; }

    public int KeptForRetention { get; init; }
}

/// <summary>Finds what each kind of cleanup would remove. Shared by the scan and by the run, so they never disagree.</summary>
internal static class StorageCleanupFinder
{
    /// <summary>An upload stores the bytes first and the row second, so a young file without a row is not an orphan yet.</summary>
    public const int OrphanGraceHours = 24;

    public const int ExampleCount = 5;

    public static async Task<List<StoredObject>> FindOrphansAsync(
        IApplicationDbContext context,
        IFileStorage storage,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        // Referenced means a live record points at it. The object of a soft-deleted document should be gone already,
        // so one that is still there is leftover too. Records that are only hidden (a soft-deleted weekly report, a
        // deleted branch) still count as referenced: removing their files is not this cleanup's call.
        var referenced = new HashSet<string>(StringComparer.Ordinal);

        referenced.UnionWith(await context.Attachments.IgnoreQueryFilters()
            .Where(a => !a.IsDeleted).Select(a => a.StorageKey).ToListAsync(cancellationToken));
        referenced.UnionWith(await context.WeeklySiteReportPages.IgnoreQueryFilters()
            .Select(p => p.StorageKey).ToListAsync(cancellationToken));
        referenced.UnionWith(await context.WeeklySiteReports.IgnoreQueryFilters()
            .Select(r => r.StorageKey).ToListAsync(cancellationToken));

        foreach (var branch in await context.Branches.IgnoreQueryFilters()
                     .Select(b => new { b.LogoStorageKey, b.LogoDarkStorageKey, b.LogoRasterStorageKey })
                     .ToListAsync(cancellationToken))
        {
            AddKeys(referenced, branch.LogoStorageKey, branch.LogoDarkStorageKey, branch.LogoRasterStorageKey);
        }

        foreach (var company in await context.CompanySettings.IgnoreQueryFilters()
                     .Select(c => new { c.LogoStorageKey, c.LogoRasterStorageKey })
                     .ToListAsync(cancellationToken))
        {
            AddKeys(referenced, company.LogoStorageKey, company.LogoRasterStorageKey);
        }

        var cutoff = nowUtc.AddHours(-OrphanGraceHours);

        return (await storage.ListAsync(cancellationToken))
            .Where(o => !referenced.Contains(o.Key) && o.LastModifiedUtc < cutoff)
            .ToList();
    }

    private static void AddKeys(HashSet<string> keys, params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrEmpty(value))
            {
                keys.Add(value);
            }
        }
    }

    /// <summary>Live documents whose owner has been deleted (and whose retention, if any, has run out).</summary>
    public static IQueryable<Attachment> DeletedRecordDocuments(
        IApplicationDbContext context,
        DateOnly today,
        bool includeRetained)
    {
        var employees = context.Employees.IgnoreQueryFilters().Where(e => e.IsDeleted).Select(e => e.Id);
        var projects = context.Projects.IgnoreQueryFilters().Where(e => e.IsDeleted).Select(e => e.Id);
        var vehicles = context.Vehicles.IgnoreQueryFilters().Where(e => e.IsDeleted).Select(e => e.Id);
        var tools = context.Tools.IgnoreQueryFilters().Where(e => e.IsDeleted).Select(e => e.Id);
        var workItems = context.WorkItems.IgnoreQueryFilters().Where(e => e.IsDeleted).Select(e => e.Id);
        var accommodations = context.Accommodations.IgnoreQueryFilters().Where(e => e.IsDeleted).Select(e => e.Id);

        var query = context.Attachments.Where(a =>
            (a.EmployeeId != null && employees.Contains(a.EmployeeId.Value))
            || (a.ProjectId != null && projects.Contains(a.ProjectId.Value))
            || (a.VehicleId != null && vehicles.Contains(a.VehicleId.Value))
            || (a.ToolId != null && tools.Contains(a.ToolId.Value))
            || (a.WorkItemId != null && workItems.Contains(a.WorkItemId.Value))
            || (a.AccommodationId != null && accommodations.Contains(a.AccommodationId.Value)));

        return includeRetained ? query : query.Where(a => a.RetainUntil == null || a.RetainUntil < today);
    }

    /// <summary>Rows kept only as a trace of a deleted document.</summary>
    public static IQueryable<Attachment> Traces(IApplicationDbContext context) =>
        context.Attachments.IgnoreQueryFilters().Where(a => a.IsDeleted);
}

public record GetStorageCleanupQuery : IRequest<StorageCleanupDto>;

public class GetStorageCleanupQueryHandler : IRequestHandler<GetStorageCleanupQuery, StorageCleanupDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IFileStorage _storage;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;

    public GetStorageCleanupQueryHandler(
        IApplicationDbContext context,
        IFileStorage storage,
        ICurrentUserService currentUser,
        IDateTimeProvider clock)
    {
        _context = context;
        _storage = storage;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<StorageCleanupDto> Handle(GetStorageCleanupQuery request, CancellationToken cancellationToken)
    {
        StorageCleanupGuard.EnsureSuperAdmin(_currentUser);

        var orphans = await StorageCleanupFinder.FindOrphansAsync(_context, _storage, _clock.UtcNow, cancellationToken);

        var deadAll = StorageCleanupFinder.DeletedRecordDocuments(_context, _clock.Today, includeRetained: true);
        var deadRemovable = StorageCleanupFinder.DeletedRecordDocuments(_context, _clock.Today, includeRetained: false);
        var removable = await deadRemovable.GroupBy(_ => 1)
            .Select(g => new { Count = g.Count(), Bytes = g.Sum(a => a.SizeBytes) })
            .FirstOrDefaultAsync(cancellationToken);
        var kept = await deadAll.CountAsync(cancellationToken) - (removable?.Count ?? 0);
        var examples = await deadRemovable.OrderByDescending(a => a.SizeBytes)
            .Select(a => a.FileName).Take(StorageCleanupFinder.ExampleCount).ToListAsync(cancellationToken);

        var traceCount = await StorageCleanupFinder.Traces(_context).CountAsync(cancellationToken);

        return new StorageCleanupDto
        {
            OrphanGraceHours = StorageCleanupFinder.OrphanGraceHours,
            Categories =
            [
                new StorageCleanupCategoryDto
                {
                    Kind = StorageCleanupKind.OrphanFiles,
                    Count = orphans.Count,
                    Bytes = orphans.Sum(o => o.Size),
                    Examples = orphans.OrderByDescending(o => o.Size).Take(StorageCleanupFinder.ExampleCount).Select(o => o.Key).ToList(),
                },
                new StorageCleanupCategoryDto
                {
                    Kind = StorageCleanupKind.DeletedRecordDocuments,
                    Count = removable?.Count ?? 0,
                    Bytes = removable?.Bytes ?? 0,
                    Examples = examples,
                    KeptForRetention = kept,
                },
                new StorageCleanupCategoryDto
                {
                    Kind = StorageCleanupKind.DeletionTraces,
                    Count = traceCount,
                },
            ],
        };
    }
}

public record RunStorageCleanupCommand(StorageCleanupKind Kind) : IRequest<StorageCleanupResultDto>;

public class RunStorageCleanupCommandHandler : IRequestHandler<RunStorageCleanupCommand, StorageCleanupResultDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IFileStorage _storage;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<RunStorageCleanupCommandHandler> _logger;

    public RunStorageCleanupCommandHandler(
        IApplicationDbContext context,
        IFileStorage storage,
        ICurrentUserService currentUser,
        IDateTimeProvider clock,
        ILogger<RunStorageCleanupCommandHandler> logger)
    {
        _context = context;
        _storage = storage;
        _currentUser = currentUser;
        _clock = clock;
        _logger = logger;
    }

    public async Task<StorageCleanupResultDto> Handle(RunStorageCleanupCommand request, CancellationToken cancellationToken)
    {
        StorageCleanupGuard.EnsureSuperAdmin(_currentUser);

        var result = request.Kind switch
        {
            StorageCleanupKind.OrphanFiles => await RemoveOrphansAsync(cancellationToken),
            StorageCleanupKind.DeletedRecordDocuments => await RemoveDeletedRecordDocumentsAsync(cancellationToken),
            StorageCleanupKind.DeletionTraces => await RemoveTracesAsync(cancellationToken),
            _ => throw new ConflictException("Unknown kind of cleanup."),
        };

        // Who removed what, in the log: the rows are gone, and the log is what is left to answer the question.
        _logger.LogWarning(
            "Storage cleanup {Kind} by user {UserId}: {Files} file(s), {Rows} row(s), {Bytes} bytes freed.",
            request.Kind, _currentUser.UserId, result.RemovedFiles, result.RemovedRows, result.FreedBytes);

        return result;
    }

    private async Task<StorageCleanupResultDto> RemoveOrphansAsync(CancellationToken cancellationToken)
    {
        var orphans = await StorageCleanupFinder.FindOrphansAsync(_context, _storage, _clock.UtcNow, cancellationToken);

        foreach (var orphan in orphans)
        {
            await _storage.DeleteAsync(orphan.Key, cancellationToken);
        }

        return new StorageCleanupResultDto
        {
            Kind = StorageCleanupKind.OrphanFiles,
            RemovedFiles = orphans.Count,
            FreedBytes = orphans.Sum(o => o.Size),
        };
    }

    private async Task<StorageCleanupResultDto> RemoveDeletedRecordDocumentsAsync(CancellationToken cancellationToken)
    {
        var today = _clock.Today;

        var documents = await StorageCleanupFinder.DeletedRecordDocuments(_context, today, includeRetained: false)
            .Select(a => new { a.Id, a.StorageKey, a.SizeBytes })
            .ToListAsync(cancellationToken);

        var kept = await StorageCleanupFinder.DeletedRecordDocuments(_context, today, includeRetained: true)
            .CountAsync(cancellationToken) - documents.Count;

        // Bytes first, row second, like every other delete here: a row left without its file is a document that cannot
        // be opened; a file left without its row is an orphan this same screen can sweep.
        foreach (var document in documents)
        {
            await _storage.DeleteAsync(document.StorageKey, cancellationToken);
        }

        var removed = await RemoveRowsAsync(documents.Select(d => d.Id).ToList(), cancellationToken);

        return new StorageCleanupResultDto
        {
            Kind = StorageCleanupKind.DeletedRecordDocuments,
            RemovedFiles = documents.Count,
            RemovedRows = removed,
            FreedBytes = documents.Sum(d => d.SizeBytes),
            KeptForRetention = kept,
        };
    }

    private async Task<StorageCleanupResultDto> RemoveTracesAsync(CancellationToken cancellationToken)
    {
        var traces = await StorageCleanupFinder.Traces(_context)
            .Select(a => new { a.Id, a.StorageKey })
            .ToListAsync(cancellationToken);

        // The file of a deleted document is removed when it is deleted; this only makes sure none was left behind.
        foreach (var trace in traces)
        {
            await _storage.DeleteAsync(trace.StorageKey, cancellationToken);
        }

        var removed = await RemoveRowsAsync(traces.Select(t => t.Id).ToList(), cancellationToken);

        return new StorageCleanupResultDto
        {
            Kind = StorageCleanupKind.DeletionTraces,
            RemovedRows = removed,
        };
    }

    /// <summary>A real delete: the usual one only hides the row, which is what the traces are.</summary>
    private async Task<int> RemoveRowsAsync(List<Guid> ids, CancellationToken cancellationToken)
    {
        var removed = 0;

        foreach (var batch in ids.Chunk(500))
        {
            var batchIds = batch;
            removed += await _context.Attachments.IgnoreQueryFilters()
                .Where(a => batchIds.Contains(a.Id))
                .ExecuteDeleteAsync(cancellationToken);
        }

        return removed;
    }
}

internal static class StorageCleanupGuard
{
    public static void EnsureSuperAdmin(ICurrentUserService currentUser)
    {
        if (currentUser.Role != UserRole.SuperAdmin)
        {
            throw new ForbiddenAccessException("Only the Super Admin may clean the server.");
        }
    }
}
