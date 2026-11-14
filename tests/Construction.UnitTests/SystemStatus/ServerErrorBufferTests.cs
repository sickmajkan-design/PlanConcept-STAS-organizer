using Construction.Application.Features.SystemStatus;

namespace Construction.UnitTests.SystemStatus;

public class ServerErrorBufferTests
{
    private static readonly DateTime Now = new(2026, 11, 14, 12, 0, 0, DateTimeKind.Utc);

    private static Exception Thrown(string message)
    {
        try
        {
            throw new InvalidOperationException(message);
        }
        catch (Exception exception)
        {
            return exception;
        }
    }

    [Fact]
    public void The_same_error_is_counted_not_listed_twice()
    {
        var buffer = new ServerErrorBuffer();

        buffer.Record(Thrown("a"), "GET /api/x/{id}", "1.1.9", Now);
        buffer.Record(Thrown("b"), "GET /api/x/{id}", "1.1.9", Now.AddMinutes(1));

        var entry = Assert.Single(buffer.Drain());
        Assert.Equal(2, entry.Count);
        Assert.Equal(Now.AddMinutes(1), entry.LastSeenAt);
        Assert.Empty(buffer.Drain());
    }

    [Fact]
    public void A_message_never_reaches_the_buffer()
    {
        var buffer = new ServerErrorBuffer();

        buffer.Record(Thrown("ana@example.com could not be saved"), "POST /api/y", "1.1.9", Now);

        var entry = Assert.Single(buffer.Drain());
        Assert.DoesNotContain("ana@example.com", string.Join("|", entry.ExceptionType, entry.Location, entry.Route));
        Assert.Equal("InvalidOperationException", entry.ExceptionType);
    }

    [Fact]
    public void A_different_route_is_a_different_error()
    {
        var buffer = new ServerErrorBuffer();
        var exception = Thrown("x");

        buffer.Record(exception, "GET /a", "1", Now);
        buffer.Record(exception, "GET /b", "1", Now);

        Assert.Equal(2, buffer.Drain().Count);
    }

    [Fact]
    public void What_could_not_be_saved_is_put_back_and_added_to_what_arrived_meanwhile()
    {
        var buffer = new ServerErrorBuffer();
        var exception = Thrown("x");

        buffer.Record(exception, "GET /a", "1", Now);
        var taken = buffer.Drain();
        buffer.Record(exception, "GET /a", "1", Now.AddMinutes(1));
        buffer.Return(taken);

        Assert.Equal(2, Assert.Single(buffer.Drain()).Count);
    }
}
