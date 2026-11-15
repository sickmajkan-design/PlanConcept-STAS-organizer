using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Common.Behaviours;

/// <summary>
/// Something a command works on, as far as a site-level role is concerned: the project it belongs to, and the person it
/// is about (so that a foreman may still do what concerns their own record).
/// </summary>
/// <param name="ProjectId">The project the record is booked to, or null when it is not tied to one.</param>
/// <param name="OwnerEmployeeId">The person the record is about; a record about the caller is always theirs to touch.</param>
/// <param name="Entity">What a "not found" answer names, the same as the handler's own answer for a missing record.</param>
/// <param name="Key">What a "not found" answer calls the id.</param>
public readonly record struct SiteTarget(Guid? ProjectId, Guid? OwnerEmployeeId, string Entity, object Key);

/// <summary>
/// Marks a command that changes something belonging to a site, so a Foreman or Project Manager is refused when it is
/// not one of theirs. The rule is the one the reads already follow (<see cref="SiteScope"/>): the projects they are
/// posted to. Every other role passes untouched.
/// </summary>
public interface ISiteScoped
{
    /// <summary>
    /// The records this command would touch, resolved with the context. A record that does not exist is simply left out,
    /// so the handler gives its own "not found" and nothing here confirms a guessed id.
    /// </summary>
    Task<IReadOnlyList<SiteTarget>> SiteTargetsAsync(IApplicationDbContext context, CancellationToken cancellationToken);
}

/// <summary>Resolvers for the records commands usually name, so a command states what it touches in one line.</summary>
public static class SiteTargets
{
    public delegate Task<SiteTarget?> Resolver(IApplicationDbContext context, CancellationToken cancellationToken);

    public static async Task<IReadOnlyList<SiteTarget>> ResolveAsync(
        IApplicationDbContext context,
        CancellationToken cancellationToken,
        params Resolver[] resolvers)
    {
        var targets = new List<SiteTarget>();

        foreach (var resolver in resolvers)
        {
            if (await resolver(context, cancellationToken) is { } target)
            {
                targets.Add(target);
            }
        }

        return targets;
    }

    /// <summary>A project named in the request. Nothing when none is named.</summary>
    public static Resolver Project(Guid? projectId) =>
        (_, _) => Task.FromResult<SiteTarget?>(projectId is { } id ? new SiteTarget(id, null, nameof(Project), id) : null);

    /// <summary>
    /// A new or changed record that carries a project and a person. With no project it is only the caller's to touch when it
    /// is about them; with neither there is no site to protect.
    /// </summary>
    public static Resolver Record(Guid? projectId, Guid? employeeId, string entity, object key) =>
        (_, _) => Task.FromResult<SiteTarget?>(
            projectId is null && employeeId is null ? null : new SiteTarget(projectId, employeeId, entity, key));

    public static Resolver WorkItem(Guid id) => async (context, ct) =>
    {
        var item = await context.WorkItems.AsNoTracking()
            .Where(w => w.Id == id)
            .Select(w => new { w.ProjectId, w.AssignedEmployeeId })
            .FirstOrDefaultAsync(ct);

        // Work tied to no site is nobody's site, whoever it is assigned to.
        return item is null || item.ProjectId is null
            ? null
            : new SiteTarget(item.ProjectId, item.AssignedEmployeeId, nameof(WorkItem), id);
    };

    /// <summary>The work item an upload is filed against. Nothing when the upload is not for a work item.</summary>
    public static Resolver WorkItemOwner(Guid? id) => id is { } workItemId ? WorkItem(workItemId) : (_, _) => Task.FromResult<SiteTarget?>(null);

    public static Resolver TimeEntry(Guid id) => async (context, ct) =>
    {
        var entry = await context.TimeEntries.AsNoTracking()
            .Where(t => t.Id == id)
            .Select(t => new { t.ProjectId, t.EmployeeId })
            .FirstOrDefaultAsync(ct);

        return entry is null ? null : new SiteTarget(entry.ProjectId, entry.EmployeeId, nameof(TimeEntry), id);
    };

    /// <summary>A tool's current posting. A tool posted nowhere is free equipment and is nobody's site.</summary>
    public static Resolver Tool(Guid id) => async (context, ct) =>
    {
        var projectId = await context.Tools.AsNoTracking()
            .Where(t => t.Id == id)
            .Select(t => t.AssignedProjectId)
            .FirstOrDefaultAsync(ct);

        return projectId is { } p ? new SiteTarget(p, null, nameof(Tool), id) : null;
    };

    /// <summary>A vehicle's current posting. A vehicle posted nowhere is free equipment and is nobody's site.</summary>
    public static Resolver Vehicle(Guid id) => async (context, ct) =>
    {
        var projectId = await context.Vehicles.AsNoTracking()
            .Where(v => v.Id == id)
            .Select(v => v.AssignedProjectId)
            .FirstOrDefaultAsync(ct);

        return projectId is { } p ? new SiteTarget(p, null, nameof(Vehicle), id) : null;
    };

    /// <summary>A material's project. Stock held for the company in the warehouse is nobody's site.</summary>
    public static Resolver Material(Guid id) => async (context, ct) =>
    {
        var projectId = await context.Materials.AsNoTracking()
            .Where(m => m.Id == id)
            .Select(m => m.ProjectId)
            .FirstOrDefaultAsync(ct);

        return projectId is { } p ? new SiteTarget(p, null, nameof(Material), id) : null;
    };

    public static Resolver ArticleOrder(Guid id) => async (context, ct) =>
    {
        var projectId = await context.ArticleOrders.AsNoTracking()
            .Where(o => o.Id == id)
            .Select(o => o.ProjectId)
            .FirstOrDefaultAsync(ct);

        return projectId is { } p ? new SiteTarget(p, null, nameof(ArticleOrder), id) : null;
    };
}

/// <summary>
/// Refuses an <see cref="ISiteScoped"/> command that touches a site the Foreman or Project Manager is not posted to,
/// with the same "not found" a missing record gets, ahead of validation (as <see cref="FinanceAccessBehaviour{TRequest,TResponse}"/>
/// does), so nothing about a record outside their sites is confirmed.
/// </summary>
public class SiteScopeBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;

    public SiteScopeBehaviour(IApplicationDbContext context, ICurrentUserService currentUser, IDateTimeProvider clock)
    {
        _context = context;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is ISiteScoped scoped
            && await SiteScope.OwnProjectIdsAsync(_context, _currentUser, _clock.Today, cancellationToken) is { } own)
        {
            var self = _currentUser.EmployeeId;

            foreach (var target in await scoped.SiteTargetsAsync(_context, cancellationToken))
            {
                var mine = (target.ProjectId is { } project && own.Contains(project))
                    || (self is not null && target.OwnerEmployeeId == self);

                if (!mine)
                {
                    throw new NotFoundException(target.Entity, target.Key);
                }
            }
        }

        return await next();
    }
}
