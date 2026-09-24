namespace Construction.Application.Features.Dashboard.Models;

public record DashboardLayoutDto
{
    public IReadOnlyList<DashboardWidgetDto> Widgets { get; init; } = [];
}

/// <summary>
/// Position and size on the free-form board, in grid units — not a fixed
/// column/order pair. The admin panel drags to (X, Y) and resizes to (W, H)
/// directly; nothing here is a preset bucket.
/// </summary>
public record DashboardWidgetDto
{
    public Guid Id { get; init; }

    public string Type { get; init; } = null!;

    public int X { get; init; }

    public int Y { get; init; }

    public int W { get; init; }

    public int H { get; init; }

    /// <summary>
    /// Choices made on this one widget — which project it shows, how many rows
    /// — as plain text keyed by name. Null when it has none. Bounded on save;
    /// what the keys mean is the widget's own business.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Settings { get; init; }
}
