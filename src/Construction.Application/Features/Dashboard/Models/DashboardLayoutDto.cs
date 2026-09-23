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
}
