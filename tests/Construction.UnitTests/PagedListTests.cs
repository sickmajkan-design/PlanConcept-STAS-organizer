using Construction.Application.Common.Models;

namespace Construction.UnitTests;

/// <summary>
/// The paging envelope every list endpoint returns. Both clients drive their
/// infinite scroll and grid pagination off HasNextPage / TotalPages, so the
/// arithmetic here is load-bearing.
/// </summary>
public class PagedListTests
{
    private static PagedList<string> Page(int totalCount, int pageNumber, int pageSize) =>
        new(Array.Empty<string>(), totalCount, pageNumber, pageSize);

    [Theory]
    [InlineData(0, 20, 0)]
    [InlineData(1, 20, 1)]
    [InlineData(20, 20, 1)]
    [InlineData(21, 20, 2)]
    [InlineData(40, 20, 2)]
    [InlineData(41, 20, 3)]
    public void Total_pages_rounds_a_partial_last_page_up(int totalCount, int pageSize, int expected)
    {
        Assert.Equal(expected, Page(totalCount, 1, pageSize).TotalPages);
    }

    [Fact]
    public void First_page_of_several_has_a_next_but_no_previous()
    {
        var page = Page(totalCount: 45, pageNumber: 1, pageSize: 20);

        Assert.False(page.HasPreviousPage);
        Assert.True(page.HasNextPage);
    }

    [Fact]
    public void Middle_page_has_both_neighbours()
    {
        var page = Page(totalCount: 45, pageNumber: 2, pageSize: 20);

        Assert.True(page.HasPreviousPage);
        Assert.True(page.HasNextPage);
    }

    [Fact]
    public void Last_page_has_a_previous_but_no_next()
    {
        // Stops the mobile app's infinite scroll from requesting page 4 forever.
        var page = Page(totalCount: 45, pageNumber: 3, pageSize: 20);

        Assert.True(page.HasPreviousPage);
        Assert.False(page.HasNextPage);
    }

    [Fact]
    public void An_empty_result_offers_no_pages_to_move_to()
    {
        var page = Page(totalCount: 0, pageNumber: 1, pageSize: 20);

        Assert.Equal(0, page.TotalPages);
        Assert.False(page.HasPreviousPage);
        Assert.False(page.HasNextPage);
    }

    [Fact]
    public void A_single_full_page_reports_no_next_page()
    {
        // Exactly pageSize rows must not look like there is more to fetch.
        var page = Page(totalCount: 20, pageNumber: 1, pageSize: 20);

        Assert.Equal(1, page.TotalPages);
        Assert.False(page.HasNextPage);
    }

    [Fact]
    public void A_zero_page_size_cannot_divide_by_zero()
    {
        Assert.Equal(0, Page(totalCount: 10, pageNumber: 1, pageSize: 0).TotalPages);
    }

    [Fact]
    public void The_envelope_carries_the_items_and_the_unpaged_total()
    {
        var page = new PagedList<string>(["a", "b"], totalCount: 57, pageNumber: 2, pageSize: 2);

        Assert.Equal(["a", "b"], page.Items);
        Assert.Equal(57, page.TotalCount);
        Assert.Equal(2, page.PageNumber);
        Assert.Equal(2, page.PageSize);
    }
}
