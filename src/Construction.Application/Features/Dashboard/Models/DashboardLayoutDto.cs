namespace Construction.Application.Features.Dashboard.Models;

public record DashboardLayoutDto
{
    public IReadOnlyList<DashboardWidgetDto> Widgets { get; init; } = [];
}

public record DashboardWidgetDto
{
    public Guid Id { get; init; }

    public string Type { get; init; } = null!;

    public int Column { get; init; }

    public int Order { get; init; }
}
