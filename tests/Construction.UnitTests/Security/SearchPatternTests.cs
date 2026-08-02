using Construction.Application.Common.Security;

namespace Construction.UnitTests.Security;

/// <summary>
/// The search term is already a parameter, so this was never SQL injection —
/// but it is user-controlled LIKE syntax, and unescaped wildcards make the
/// search mean something other than what was typed.
/// </summary>
public class SearchPatternTests
{
    [Fact]
    public void Wraps_an_ordinary_term_in_wildcards()
    {
        Assert.Equal("%excavator%", SearchPattern.Contains("excavator"));
    }

    [Fact]
    public void Lower_cases_and_trims()
    {
        Assert.Equal("%excavator%", SearchPattern.Contains("  ExCaVaTor  "));
    }

    [Fact]
    public void Escapes_a_percent_so_it_matches_a_literal_one()
    {
        // Without this, searching for "%" returns every row in the table.
        Assert.Equal("%\\%%", SearchPattern.Contains("%"));
    }

    [Fact]
    public void Escapes_an_underscore_so_it_matches_a_literal_one()
    {
        // "a_c" would otherwise match "abc" as well as "a_c".
        Assert.Equal("%a\\_c%", SearchPattern.Contains("a_c"));
    }

    [Fact]
    public void Escapes_a_backslash_before_adding_its_own()
    {
        // Escaping in the wrong order would turn the user's backslash into an
        // escape character and swallow the following one.
        Assert.Equal("%a\\\\b%", SearchPattern.Contains("a\\b"));
    }

    [Fact]
    public void A_term_of_only_wildcards_becomes_a_literal_search()
    {
        // This is the expensive-scan case: many wildcards against a
        // sequential scan. Escaped, it simply matches nothing.
        Assert.Equal("%\\%\\%\\%\\_\\_%", SearchPattern.Contains("%%%__"));
    }
}
