using Construction.Application.Features.Planning;
using Xunit;

namespace Construction.UnitTests.Planning;

public class PostingRangesTests
{
    private static readonly Guid SiteA = Guid.NewGuid();
    private static readonly Guid SiteB = Guid.NewGuid();

    private static DateOnly D(int day) => new(2026, 10, day);

    private static PostingSpan Span(Guid site, int from, int? to, bool withId = true) => new()
    {
        Id = withId ? Guid.NewGuid() : null,
        ProjectId = site,
        Start = D(from),
        End = to is null ? null : D(to.Value),
    };

    [Fact]
    public void Clearing_the_middle_of_a_posting_splits_it_and_keeps_the_row_on_the_left_part()
    {
        var span = Span(SiteA, 1, 20);

        var result = PostingRanges.Clear([span], D(8), D(12));

        Assert.Equal(2, result.Count);
        var left = result.Single(s => s.Start == D(1));
        var right = result.Single(s => s.Start == D(13));
        Assert.Equal(span.Id, left.Id);
        Assert.Equal(D(7), left.End);
        Assert.Null(right.Id);
        Assert.Equal(D(20), right.End);
    }

    [Fact]
    public void Clearing_the_end_of_a_posting_shortens_it()
    {
        var result = Assert.Single(PostingRanges.Clear([Span(SiteA, 1, 20)], D(15), D(25)));

        Assert.Equal(D(14), result.End);
    }

    [Fact]
    public void Clearing_the_start_of_an_open_posting_leaves_it_open()
    {
        var result = Assert.Single(PostingRanges.Clear([Span(SiteA, 1, null)], D(1), D(5)));

        Assert.Equal(D(6), result.Start);
        Assert.Null(result.End);
    }

    [Fact]
    public void Clearing_a_posting_wholly_inside_the_range_removes_it()
    {
        Assert.Empty(PostingRanges.Clear([Span(SiteA, 5, 6)], D(1), D(10)));
    }

    [Fact]
    public void Clearing_leaves_postings_outside_the_range_alone()
    {
        var result = PostingRanges.Clear([Span(SiteA, 1, 3), Span(SiteB, 20, 25)], D(10), D(12));

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void Adding_next_to_a_posting_on_the_same_site_joins_them()
    {
        var existing = Span(SiteA, 1, 7);

        var result = Assert.Single(PostingRanges.Add([existing], SiteA, D(8), D(14)));

        Assert.Equal(existing.Id, result.Id);
        Assert.Equal(D(1), result.Start);
        Assert.Equal(D(14), result.End);
    }

    [Fact]
    public void Adding_inside_an_open_posting_on_the_same_site_changes_nothing()
    {
        var result = Assert.Single(PostingRanges.Add([Span(SiteA, 1, null)], SiteA, D(5), D(9)));

        Assert.Equal(D(1), result.Start);
        Assert.Null(result.End);
    }

    [Fact]
    public void Adding_on_another_site_keeps_both()
    {
        var result = PostingRanges.Add([Span(SiteA, 1, 7)], SiteB, D(8), D(14));

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void Moving_someone_into_the_middle_of_their_stay_on_another_site_splits_that_stay()
    {
        var spans = PostingRanges.Clear([Span(SiteA, 1, 30)], D(10), D(14));
        spans = PostingRanges.Add(spans, SiteB, D(10), D(14));

        Assert.Equal(3, spans.Count);
        Assert.Contains(spans, s => s.ProjectId == SiteB && s.Start == D(10) && s.End == D(14));
        Assert.Contains(spans, s => s.ProjectId == SiteA && s.Start == D(1) && s.End == D(9));
        Assert.Contains(spans, s => s.ProjectId == SiteA && s.Start == D(15) && s.End == D(30));
    }

    [Fact]
    public void Free_gaps_are_the_days_nothing_covers()
    {
        var gaps = PostingRanges.FreeGaps([Span(SiteA, 3, 5), Span(SiteB, 9, null)], D(1), D(12));

        Assert.Equal([(D(1), D(2)), (D(6), D(8))], gaps);
    }

    [Fact]
    public void Free_gaps_of_a_fully_covered_range_are_empty()
    {
        Assert.Empty(PostingRanges.FreeGaps([Span(SiteA, 1, null)], D(5), D(9)));
    }

    [Fact]
    public void Segments_clip_postings_to_the_range()
    {
        var segments = PostingRanges.Segments([Span(SiteA, 1, 10), Span(SiteB, 12, null)], D(8), D(14));

        Assert.Equal(2, segments.Count);
        Assert.Contains(segments, s => s.ProjectId == SiteA && s.Start == D(8) && s.End == D(10));
        Assert.Contains(segments, s => s.ProjectId == SiteB && s.Start == D(12) && s.End == D(14));
    }

    [Fact]
    public void Normalise_joins_touching_postings_on_the_same_site()
    {
        var result = Assert.Single(PostingRanges.Normalise([Span(SiteA, 1, 4), Span(SiteA, 5, 9, withId: false)]));

        Assert.Equal(D(1), result.Start);
        Assert.Equal(D(9), result.End);
    }

    [Fact]
    public void Normalise_leaves_a_gap_between_postings_alone()
    {
        Assert.Equal(2, PostingRanges.Normalise([Span(SiteA, 1, 4), Span(SiteA, 6, 9)]).Count);
    }
}
