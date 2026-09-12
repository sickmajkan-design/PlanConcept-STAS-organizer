namespace Construction.Application.Common.Interfaces;

/// <summary>A file to attach to an outgoing email, such as a scheduled report's spreadsheet.</summary>
public record EmailAttachment(string FileName, string ContentType, byte[] Content);

public interface IEmailSender
{
    Task SendAsync(
        string to,
        string subject,
        string htmlBody,
        EmailAttachment? attachment = null,
        CancellationToken cancellationToken = default);
}
