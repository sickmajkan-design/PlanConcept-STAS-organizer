using Construction.Infrastructure.Monitoring;
using Microsoft.Extensions.Configuration;

namespace Construction.UnitTests.SystemStatus;

public class ServerAlertRecipientsTests
{
    private static ServerAlertRecipients From(string? value) =>
        new(new ConfigurationBuilder()
            .AddInMemoryCollection(value is null ? [] : new Dictionary<string, string?> { ["ServerAlerts:ExtraEmails"] = value })
            .Build());

    [Fact]
    public void Nothing_is_set_by_default()
    {
        Assert.Empty(From(null).ExtraEmails);
        Assert.Empty(From("  ").ExtraEmails);
    }

    [Fact]
    public void Addresses_are_split_trimmed_deduplicated_and_junk_is_dropped()
    {
        var extra = From(" a@example.com ; b@example.com, A@example.com ,not-an-address").ExtraEmails;

        Assert.Equal(["a@example.com", "b@example.com"], extra);
    }
}
