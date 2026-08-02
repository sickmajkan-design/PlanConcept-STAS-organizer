namespace Construction.Application.Common.Security;

/// <summary>
/// Builds the <c>LIKE</c> pattern used by every list endpoint's search.
/// </summary>
public static class SearchPattern
{
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
