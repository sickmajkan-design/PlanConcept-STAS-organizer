namespace Construction.Application.Common.Security;

/// <summary>
/// Builds the <c>LIKE</c> pattern used by every list endpoint's search.
/// </summary>
public static class SearchPattern
{
    /// <summary>
    /// The escape character the patterns are built with.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>It has to be passed to <c>EF.Functions.Like</c> explicitly.</strong>
    /// The two-argument overload translates to <c>LIKE @pattern ESCAPE ''</c>
    /// — an empty escape clause, which turns escaping <em>off</em>. Every
    /// backslash <see cref="Contains"/> adds then reaches PostgreSQL as an
    /// ordinary character that has to be matched literally, so a search for
    /// <c>50%</c> looks for a backslash and finds nothing.
    /// </para>
    /// <para>
    /// That is not visible in the C# — both overloads read the same at the
    /// call site, and a plain search term contains none of these characters,
    /// so the common case works and only terms with <c>%</c>, <c>_</c> or a
    /// backslash come back empty. A test asserts the pattern against a real
    /// database for exactly this reason.
    /// </para>
    /// </remarks>
    public const string Escape = "\\";

    /// <summary>
    /// Wraps a user's search term in wildcards, escaping the wildcard
    /// characters inside it first.
    ///
    /// <para>
    /// The term is already passed as a parameter, so this was never an SQL
    /// injection. It is still user-controlled pattern syntax: an unescaped
    /// <c>%</c> matches everything regardless of what was typed, and a term
    /// made of many wildcards turns a sequential scan into a far more
    /// expensive one. Escaping them makes the search mean what the user typed.
    /// </para>
    ///
    /// <para>
    /// The backslash is escaped first, otherwise the escape character added
    /// for <c>%</c> and <c>_</c> would itself be escaped.
    /// </para>
    /// </summary>
    public static string Contains(string term) =>
        $"%{term.Trim().ToLowerInvariant().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_")}%";
}
