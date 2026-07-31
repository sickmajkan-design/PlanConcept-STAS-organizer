using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using Construction.Infrastructure.Authentication;
using Construction.UnitTests.Fakes;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Construction.UnitTests.Security;

public class JwtProviderTests
{
    private const string SecretKey = "unit-test-signing-key-at-least-32-characters-long";

    private readonly FixedDateTimeProvider _clock = new();
    private readonly JwtProvider _provider;

    public JwtProviderTests()
    {
        var settings = new JwtSettings
        {
            Issuer = "construction-api",
            Audience = "construction-clients",
            SecretKey = SecretKey,
            AccessTokenLifetimeMinutes = 15,
            RefreshTokenLifetimeDays = 7
        };

        _provider = new JwtProvider(Options.Create(settings), _clock);
    }

    private static User UserWith(UserRole role, Guid? employeeId = null) => new()
    {
        Id = Guid.Parse("019fad65-d635-76f2-880f-d8d25aea67d0"),
        Email = "ivan@construction.local",
        Role = role,
        EmployeeId = employeeId
    };

    private static JwtSecurityToken Read(string token) =>
        new JwtSecurityTokenHandler().ReadJwtToken(token);

    [Fact]
    public void Access_token_carries_the_identity_the_API_authorizes_on()
    {
        var result = _provider.GenerateAccessToken(UserWith(UserRole.Foreman));

        var jwt = Read(result.Token);

        Assert.Equal(
            "019fad65-d635-76f2-880f-d8d25aea67d0",
            jwt.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Sub).Value);
        Assert.Equal(
            "ivan@construction.local",
            jwt.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Email).Value);
        Assert.Equal(
            nameof(UserRole.Foreman),
            jwt.Claims.Single(c => c.Type == ClaimTypes.Role).Value);
    }

    [Fact]
    public void Access_token_includes_the_employee_link_when_the_account_has_one()
    {
        var employeeId = Guid.Parse("019fad73-e894-791b-a6c3-715bddf61164");

        var jwt = Read(_provider.GenerateAccessToken(UserWith(UserRole.Worker, employeeId)).Token);

        Assert.Equal(
            employeeId.ToString(),
            jwt.Claims.Single(c => c.Type == JwtProvider.EmployeeIdClaim).Value);
    }

    [Fact]
    public void Access_token_omits_the_employee_claim_for_an_office_account()
    {
        // Admins have no employee record; GPS reporting keys off this claim's
        // absence, so it must not be emitted as an empty value.
        var jwt = Read(_provider.GenerateAccessToken(UserWith(UserRole.Admin)).Token);

        Assert.DoesNotContain(jwt.Claims, c => c.Type == JwtProvider.EmployeeIdClaim);
    }

    [Fact]
    public void Access_token_expires_after_the_configured_lifetime()
    {
        var result = _provider.GenerateAccessToken(UserWith(UserRole.Admin));

        Assert.Equal(_clock.UtcNow.AddMinutes(15), result.ExpiresAtUtc);
        // The reported expiry must match what is actually signed into the token.
        Assert.Equal(result.ExpiresAtUtc, Read(result.Token).ValidTo);
    }

    [Fact]
    public void Access_token_is_issued_by_and_for_the_configured_parties()
    {
        var jwt = Read(_provider.GenerateAccessToken(UserWith(UserRole.Admin)).Token);

        Assert.Equal("construction-api", jwt.Issuer);
        Assert.Contains("construction-clients", jwt.Audiences);
    }

    [Fact]
    public void Access_token_validates_against_the_signing_key_and_fails_with_another()
    {
        var token = _provider.GenerateAccessToken(UserWith(UserRole.Admin)).Token;
        var handler = new JwtSecurityTokenHandler();

        var parameters = new TokenValidationParameters
        {
            ValidIssuer = "construction-api",
            ValidAudience = "construction-clients",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey)),
            ClockSkew = TimeSpan.Zero,
            LifetimeValidator = (_, _, _, _) => true // the frozen clock is in the future
        };

        handler.ValidateToken(token, parameters, out _);

        parameters.IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes("a-completely-different-key-also-32-chars-long"));

        Assert.Throws<SecurityTokenSignatureKeyNotFoundException>(
            () => handler.ValidateToken(token, parameters, out _));
    }

    [Fact]
    public void Every_access_token_gets_its_own_jti()
    {
        var first = Read(_provider.GenerateAccessToken(UserWith(UserRole.Admin)).Token);
        var second = Read(_provider.GenerateAccessToken(UserWith(UserRole.Admin)).Token);

        Assert.NotEqual(
            first.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Jti).Value,
            second.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Jti).Value);
    }

    [Fact]
    public void Refresh_tokens_are_random_and_long_enough_to_be_unguessable()
    {
        var tokens = Enumerable.Range(0, 50)
            .Select(_ => _provider.GenerateRefreshToken())
            .ToList();

        Assert.Equal(tokens.Count, tokens.Distinct().Count());
        Assert.All(tokens, token => Assert.Equal(64, Convert.FromBase64String(token).Length));
    }

    [Fact]
    public void Refresh_token_lifetime_comes_from_configuration()
    {
        Assert.Equal(TimeSpan.FromDays(7), _provider.RefreshTokenLifetime);
    }
}
