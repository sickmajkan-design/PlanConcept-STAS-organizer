namespace Construction.Application.Features.Planning;

/// <summary>One posting as the planner edits it, detached from the database row it came from.</summary>
public sealed class PostingSpan
{
    /// <summary>The row this span continues. Null for a span that has to be created.</summary>
    public Guid? Id { get; init; }

    public Guid ProjectId { get; init; }

    public DateOnly Start { get; set; }

    /// <summary>Null while the posting is open-ended.</summary>
    public DateOnly? End { get; set; }

    public Guid? CustomerCompanyId { get; init; }

    public bool Covers(DateOnly day) => Start <= day && (End is null || End >= day);

    public PostingSpan Copy() => new()
    {
        Id = Id,
        ProjectId = ProjectId,
        Start = Start,
        End = End,
        CustomerCompanyId = CustomerCompanyId,
    };
}

/// <summary>
/// The date arithmetic behind "put this person on that site for these days": cutting a range out
/// of existing postings, putting a new one in, and merging what ends up touching. Pure, so it can be
/// tested without a database.
/// </summary>
/// <remarks>
/// A span keeps the <see cref="PostingSpan.Id"/> of the row it descends from whenever it can, so that
/// trimming a posting updates it instead of replacing it and the original "assigned at" and "assigned
/// by" survive. Only the second half of a posting that had to be split in two has no row yet.
/// </remarks>
public static class PostingRanges
{
    /// <summary>Takes <c>[from, to]</c> out of every span, whichever site it is on.</summary>
    public static List<PostingSpan> Clear(IEnumerable<PostingSpan> spans, DateOnly from, DateOnly to)
    {
        var result = new List<PostingSpan>();

        foreach (var span in spans.Select(s => s.Copy()))
        {
            var overlaps = span.Start <= to && (span.End is null || span.End >= from);

            if (!overlaps)
            {
                result.Add(span);
                continue;
            }

            var keepsLeft = span.Start < from;
            var keepsRight = span.End is null || span.End > to;

            if (keepsLeft && keepsRight)
            {
                var right = new PostingSpan
                {
                    ProjectId = span.ProjectId,
                    Start = to.AddDays(1),
                    End = span.End,
                    CustomerCompanyId = span.CustomerCompanyId,
                };

                span.End = from.AddDays(-1);
                result.Add(span);
                result.Add(right);
            }
            else if (keepsLeft)
            {
                span.End = from.AddDays(-1);
                result.Add(span);
            }
            else if (keepsRight)
            {
                span.Start = to.AddDays(1);
                result.Add(span);
            }

            // Otherwise the span lay wholly inside the range and is gone.
        }

        return result;
    }

    /// <summary>
    /// Puts <c>[from, to]</c> on <paramref name="projectId"/>, joining any posting on the same site
    /// that overlaps or touches it. The earliest of those joined keeps its row.
    /// </summary>
    public static List<PostingSpan> Add(
        IEnumerable<PostingSpan> spans,
        Guid projectId,
        DateOnly from,
        DateOnly to,
        Guid? customerCompanyId = null)
    {
        var all = spans.Select(s => s.Copy()).ToList();

        var joined = all
            .Where(s => s.ProjectId == projectId
                && s.Start <= to.AddDays(1)
                && (s.End is null || s.End >= from.AddDays(-1)))
            .OrderBy(s => s.Start)
            .ToList();

        var start = joined.Count > 0 ? DateOnly.FromDayNumber(Math.Min(joined.Min(s => s.Start.DayNumber), from.DayNumber)) : from;
        DateOnly? end = joined.Any(s => s.End is null) ? null : DateOnly.FromDayNumber(Math.Max(joined.Count > 0 ? joined.Max(s => s.End!.Value.DayNumber) : 0, to.DayNumber));

        var first = joined.FirstOrDefault();
        var merged = new PostingSpan
        {
            Id = first?.Id,
            ProjectId = projectId,
            Start = start,
            End = end,
            CustomerCompanyId = first?.CustomerCompanyId ?? joined.Select(j => j.CustomerCompanyId).FirstOrDefault(c => c is not null) ?? customerCompanyId,
        };

        all.RemoveAll(s => joined.Contains(s));
        all.Add(merged);

        return all;
    }

    /// <summary>The stretches of <c>[from, to]</c> that no span covers.</summary>
    public static List<(DateOnly From, DateOnly To)> FreeGaps(IEnumerable<PostingSpan> spans, DateOnly from, DateOnly to)
    {
        var gaps = new List<(DateOnly, DateOnly)>();
        var cursor = from;

        foreach (var span in spans.Where(s => s.Start <= to && (s.End is null || s.End >= from)).OrderBy(s => s.Start))
        {
            if (span.Start > cursor)
            {
                gaps.Add((cursor, span.Start.AddDays(-1)));
            }

            if (span.End is null)
            {
                return gaps;
            }

            if (span.End.Value.AddDays(1) > cursor)
            {
                cursor = span.End.Value.AddDays(1);
            }

            if (cursor > to)
            {
                return gaps;
            }
        }

        if (cursor <= to)
        {
            gaps.Add((cursor, to));
        }

        return gaps;
    }

    /// <summary>The parts of the spans that fall inside <c>[from, to]</c>, as new unattached spans.</summary>
    public static List<PostingSpan> Segments(IEnumerable<PostingSpan> spans, DateOnly from, DateOnly to) =>
        spans
            .Where(s => s.Start <= to && (s.End is null || s.End >= from))
            .Select(s => new PostingSpan
            {
                ProjectId = s.ProjectId,
                Start = s.Start < from ? from : s.Start,
                End = s.End is null || s.End > to ? to : s.End,
                CustomerCompanyId = s.CustomerCompanyId,
            })
            .ToList();

    /// <summary>Joins spans on the same site and company that overlap or touch.</summary>
    public static List<PostingSpan> Normalise(IEnumerable<PostingSpan> spans)
    {
        var result = new List<PostingSpan>();

        foreach (var group in spans.Select(s => s.Copy()).GroupBy(s => (s.ProjectId, s.CustomerCompanyId)))
        {
            PostingSpan? current = null;

            foreach (var span in group.OrderBy(s => s.Start))
            {
                if (current is not null && (current.End is null || span.Start <= current.End.Value.AddDays(1)))
                {
                    current.End = current.End is null || span.End is null
                        ? null
                        : span.End > current.End ? span.End : current.End;

                    continue;
                }

                if (current is not null)
                {
                    result.Add(current);
                }

                current = span;
            }

            if (current is not null)
            {
                result.Add(current);
            }
        }

        return result;
    }
}
