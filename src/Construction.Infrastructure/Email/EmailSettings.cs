namespace Construction.Infrastructure.Email;

public class EmailSettings
{
    public const string SectionName = "EmailSettings";

    public string? Host { get; set; }

    public int Port { get; set; } = 587;

    public bool UseStartTls { get; set; } = true;

    public string? Username { get; set; }

    public string? Password { get; set; }

    public string FromAddress { get; set; } = "no-reply@localhost";

    public string FromName { get; set; } = "Construction Workforce";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(Host);
}
