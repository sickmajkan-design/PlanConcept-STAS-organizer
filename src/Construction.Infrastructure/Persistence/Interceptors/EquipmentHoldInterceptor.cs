using Construction.Application.Common.Interfaces;
using Construction.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Construction.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Keeps <see cref="EquipmentHold"/> in step with where every vehicle and tool is. Whatever code changes
/// <c>AssignedEmployeeId</c> or <c>AssignedProjectId</c> (the office, a worker taking one, a return, a delete), the
/// history is written here, once, so the places that move equipment do not each have to remember to.
/// </summary>
/// <remarks>
/// A change made on the day a stretch began corrects that stretch instead of splitting it, so the history shows what was
/// true at the end of each day, not every intermediate click.
/// </remarks>
public class EquipmentHoldInterceptor : SaveChangesInterceptor
{
    private readonly IDateTimeProvider _clock;

    public EquipmentHoldInterceptor(IDateTimeProvider clock)
    {
        _clock = clock;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (eventData.Context is { } context)
        {
            var changes = Changes(context).ToList();
            var ids = changes.Select(c => c.Id).ToList();

            Record(context, changes, context.Set<EquipmentHold>().Where(h => h.To == null && ids.Contains(h.ItemId)).ToList());
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
            var changes = Changes(context).ToList();

            if (changes.Count > 0)
            {
                var ids = changes.Select(c => c.Id).ToList();
                var open = await context.Set<EquipmentHold>()
                    .Where(h => h.To == null && ids.Contains(h.ItemId))
                    .ToListAsync(cancellationToken);

                Record(context, changes, open);
            }
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private record Change(EquipmentKind Kind, Guid Id, Guid? EmployeeId, Guid? ProjectId);

    /// <summary>The items whose holder or site changed in this save, new items included (they start somewhere).</summary>
    private static IEnumerable<Change> Changes(DbContext context)
    {
        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified))
            {
                continue;
            }

            var (kind, employee, project) = entry.Entity switch
            {
                Vehicle v => (EquipmentKind.Vehicle, v.AssignedEmployeeId, v.AssignedProjectId),
                Tool t => (EquipmentKind.Tool, t.AssignedEmployeeId, t.AssignedProjectId),
                _ => ((EquipmentKind?)null, (Guid?)null, (Guid?)null),
            };

            if (kind is null)
            {
                continue;
            }

            if (entry.State == EntityState.Modified && !Moved(entry))
            {
                continue;
            }

            yield return new Change(kind.Value, ((Domain.Common.BaseEntity)entry.Entity).Id, employee, project);
        }
    }

    private static bool Moved(EntityEntry entry) =>
        entry.Properties.Any(p => p.Metadata.Name is "AssignedEmployeeId" or "AssignedProjectId" && p.IsModified
            && !Equals(p.OriginalValue, p.CurrentValue));

    private void Record(DbContext context, List<Change> changes, List<EquipmentHold> open)
    {
        var today = _clock.Today;
        var holds = context.Set<EquipmentHold>();

        foreach (var change in changes)
        {
            var current = open.FirstOrDefault(h => h.ItemId == change.Id && h.Kind == change.Kind);

            if (current is not null && current.EmployeeId == change.EmployeeId && current.ProjectId == change.ProjectId)
            {
                continue;
            }

            if (current is not null)
            {
                if (current.From >= today)
                {
                    holds.Remove(current);
                }
                else
                {
                    current.To = today.AddDays(-1);
                }
            }

            holds.Add(new EquipmentHold
            {
                Kind = change.Kind,
                ItemId = change.Id,
                EmployeeId = change.EmployeeId,
                ProjectId = change.ProjectId,
                From = today,
                // The audit-stamp interceptor has already run, so the time is set here.
                CreatedAt = _clock.UtcNow,
            });
        }
    }
}
