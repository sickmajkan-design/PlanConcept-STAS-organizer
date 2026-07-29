namespace Construction.Infrastructure.Notifications;

public class FirebaseSettings
{
    public const string SectionName = "Firebase";

    /// <summary>Path to the Firebase service-account JSON file.</summary>
    public string? CredentialsPath { get; set; }

    /// <summary>
    /// Raw service-account JSON (alternative to CredentialsPath, convenient
    /// for secret managers / environment variables).
    /// </summary>
    public string? CredentialsJson { get; set; }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(CredentialsPath) || !string.IsNullOrWhiteSpace(CredentialsJson);
}
