namespace Construction.API.Authentication;

/// <summary>
/// The body of a refresh or logout call, where the token is optional.
/// </summary>
/// <remarks>
/// <para>
/// A separate shape from the command on purpose. <c>RefreshTokenCommand</c>
/// genuinely requires a token — a handler cannot rotate nothing — and it is
/// declared non-nullable to say so. But <c>[ApiController]</c> reads a
/// non-nullable reference type as a required field and rejects the request
/// during model binding, before any controller code runs. A browser sending
/// <c>{}</c> and a cookie would be answered "The RefreshToken field is
/// required" without the cookie ever being looked at.
/// </para>
/// <para>
/// So the HTTP surface accepts a body that may omit it, and the controller
/// assembles the command from whichever source actually had the token.
/// Deciding that is the controller's job; the handler's contract stays
/// honest.
/// </para>
/// </remarks>
public record TokenRequest
{
    /// <summary>
    /// Omitted by a browser, which holds its token in a cookie it cannot read.
    /// </summary>
    public string? RefreshToken { get; init; }
}
