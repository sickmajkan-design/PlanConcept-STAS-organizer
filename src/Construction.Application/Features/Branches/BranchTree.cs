using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Branches;

/// <summary>
/// The nesting of business units: which unit sits under which, how deep, and the path column that
/// lets every report ask "this unit and everything under it" with one string test.
/// </summary>
public static class BranchTree
{
    /// <summary>
    /// What a query looks for in <see cref="Branch.Path"/> to know a unit is the one asked for or
    /// lies under it. The commas stop one id matching inside another.
    /// </summary>
    public static string Token(Guid branchId) => $",{branchId:D},";

    /// <summary>How many levels a path has: a unit directly under the company is 1.</summary>
    public static int DepthOf(string path) =>
        path.Split(',', StringSplitOptions.RemoveEmptyEntries).Length;

    /// <summary>
    /// Puts a unit under another (or directly under the company when <paramref name="parentId"/> is
    /// null), refusing a move that would make a loop or nest deeper than <see cref="Branch.MaxDepth"/>,
    /// and recomputes the path of the unit and everything under it. Saves nothing: the caller saves.
    /// </summary>
    public static async Task PlaceAsync(
        IApplicationDbContext context,
        Branch branch,
        Guid? parentId,
        CancellationToken cancellationToken)
    {
        // A handful of units, so the whole tree is read and worked out in memory: simpler than
        // recursive SQL and it sees the unit being added, which is not saved yet.
        var all = await context.Branches.ToListAsync(cancellationToken);

        if (!all.Contains(branch))
        {
            all.Add(branch);
        }

        var byId = all.ToDictionary(b => b.Id);

        if (parentId is { } pid)
        {
            if (pid == branch.Id)
            {
                throw new ConflictException("A business unit cannot be placed under itself.");
            }

            if (!byId.TryGetValue(pid, out var parent))
            {
                throw new NotFoundException(nameof(Branch), pid);
            }

            if (PathOf(parent, byId).Contains(Token(branch.Id), StringComparison.Ordinal))
            {
                throw new ConflictException("A business unit cannot be placed under one of its own sub-units.");
            }

            var below = HeightBelow(branch, all);

            if (DepthOf(PathOf(parent, byId)) + 1 + below > Branch.MaxDepth)
            {
                throw new ConflictException(
                    $"Business units can be nested {Branch.MaxDepth} levels deep at most.");
            }
        }

        branch.ParentBranchId = parentId;
        branch.Parent = parentId is { } p ? byId[p] : null;

        foreach (var unit in all)
        {
            unit.Path = PathOf(unit, byId);
        }
    }

    /// <summary>The ids from the top down to the unit, each wrapped in commas.</summary>
    private static string PathOf(Branch unit, IReadOnlyDictionary<Guid, Branch> byId)
    {
        var chain = new List<Guid> { unit.Id };
        var current = unit;

        while (current.ParentBranchId is { } up && byId.TryGetValue(up, out var next) && chain.Count < 16)
        {
            chain.Insert(0, next.Id);
            current = next;
        }

        return "," + string.Join(',', chain) + ",";
    }

    /// <summary>How many levels of units lie under this one (0 for a unit with none).</summary>
    private static int HeightBelow(Branch unit, IReadOnlyList<Branch> all)
    {
        var token = Token(unit.Id);
        var deepest = DepthOf(unit.Path);

        foreach (var other in all)
        {
            if (other.Id != unit.Id && other.Path.Contains(token, StringComparison.Ordinal))
            {
                deepest = Math.Max(deepest, DepthOf(other.Path));
            }
        }

        return deepest - DepthOf(unit.Path);
    }
}
