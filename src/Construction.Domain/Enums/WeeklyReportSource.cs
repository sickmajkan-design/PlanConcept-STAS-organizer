namespace Construction.Domain.Enums;

/// <summary>Where a weekly site report came from.</summary>
public enum WeeklyReportSource
{
    /// <summary>The foreman sent it from the app or the panel.</summary>
    App = 1,

    /// <summary>The office entered it: it came by e-mail, by phone or on paper.</summary>
    Office = 2,
}
