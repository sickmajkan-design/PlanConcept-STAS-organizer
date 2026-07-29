using Construction.Application.Features.Authentication.Commands.ForgotPassword;
using Microsoft.Extensions.Configuration;

namespace Construction.Infrastructure.Authentication;

/// <summary>
/// Builds the password-reset deep link pointing at the admin web app
/// (configured under ClientApp:PasswordResetUrl).
/// </summary>
public class ResetLinkBuilder : IResetLinkBuilder
{
    private readonly string _baseUrl;

    public ResetLinkBuilder(IConfiguration configuration)
    {
        _baseUrl = configuration["ClientApp:PasswordResetUrl"]
                   ?? throw new InvalidOperationException(
                       "Configuration value 'ClientApp:PasswordResetUrl' is missing.");
    }

    public string Build(string email, string rawToken)
    {
        var separator = _baseUrl.Contains('?') ? '&' : '?';
        return $"{_baseUrl}{separator}email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(rawToken)}";
    }
}
