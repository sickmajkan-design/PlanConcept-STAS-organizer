namespace Construction.Domain.Enums;

/// <summary>Which of the three reminders about a request's deadline a claim stands for.</summary>
public enum DocumentRequestReminderKind
{
    /// <summary>The person is told the day before the deadline.</summary>
    DayBefore = 1,

    /// <summary>The person is told on the deadline itself.</summary>
    OnDue = 2,

    /// <summary>The office is told that the deadline has passed and nothing has been sent.</summary>
    Overdue = 3
}
