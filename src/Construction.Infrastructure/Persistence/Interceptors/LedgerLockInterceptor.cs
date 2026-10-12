using Construction.Application.Common.Exceptions;
using Construction.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Construction.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Refuses to save a change to the cells, columns, sections, rows or summary boxes of a month that has been
/// closed. It runs inside the same save as the change, so a month cannot be closed and changed in a race.
/// </summary>
/// <remarks>
/// Done once here rather than in each of the twenty-odd commands that touch a ledger: a new command that
/// forgot the check would otherwise be a way around the lock.
/// </remarks>
public class LedgerLockInterceptor : SaveChangesInterceptor
{
    private const string Message = "This month is closed. Reopen it, with a reason, to change it.";

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        if (eventData.Context is { } context)
        {
            var ids = ChangedIds(context);

            if (ids.Any && IsLockedAsync(context, ids, CancellationToken.None).GetAwaiter().GetResult())
            {
                throw new ConflictException(Message);
            }
        }

        return base.SavingChanges(eventData, result);
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is { } context)
        {
            var ids = ChangedIds(context);

            if (ids.Any && await IsLockedAsync(context, ids, cancellationToken))
            {
                throw new ConflictException(Message);
            }
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private sealed record Ids(HashSet<Guid> Ledgers, HashSet<Guid> Sections, HashSet<Guid> Rows)
    {
        public bool Any => Ledgers.Count + Sections.Count + Rows.Count > 0;
    }

    private static Ids ChangedIds(DbContext context)
    {
        var ledgers = new HashSet<Guid>();
        var sections = new HashSet<Guid>();
        var rows = new HashSet<Guid>();

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
            {
                continue;
            }

            switch (entry.Entity)
            {
                case LedgerColumn column:
                    ledgers.Add(column.LedgerId);
                    break;
                case LedgerSummaryBox box:
                    ledgers.Add(box.LedgerId);
                    break;
                case LedgerSection section:
                    ledgers.Add(section.LedgerId);
                    break;
                case LedgerRow row:
                    sections.Add(row.SectionId);
                    break;
                case LedgerCell cell:
                    rows.Add(cell.RowId);
                    break;
            }
        }

        // A key that EF has not generated yet is a new row of a new month, which cannot be locked.
        ledgers.Remove(Guid.Empty);
        sections.Remove(Guid.Empty);
        rows.Remove(Guid.Empty);

        return new Ids(ledgers, sections, rows);
    }

    private static async Task<bool> IsLockedAsync(DbContext context, Ids ids, CancellationToken cancellationToken)
    {
        if (ids.Ledgers.Count > 0
            && await context.Set<Ledger>().IgnoreQueryFilters().AsNoTracking()
                .AnyAsync(l => ids.Ledgers.Contains(l.Id) && l.LockedAt != null, cancellationToken))
        {
            return true;
        }

        if (ids.Sections.Count > 0
            && await context.Set<LedgerSection>().IgnoreQueryFilters().AsNoTracking()
                .AnyAsync(s => ids.Sections.Contains(s.Id) && s.Ledger.LockedAt != null, cancellationToken))
        {
            return true;
        }

        return ids.Rows.Count > 0
            && await context.Set<LedgerRow>().IgnoreQueryFilters().AsNoTracking()
                .AnyAsync(r => ids.Rows.Contains(r.Id) && r.Section.Ledger.LockedAt != null, cancellationToken);
    }
}
