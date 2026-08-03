using Construction.Application.Features.TimeEntries;
using Construction.Application.Features.TimeEntries.Commands.ClockOut;
using Construction.Application.Features.TimeEntries.Commands.CreateTimeEntry;
using Construction.Application.Features.TimeEntries.Commands.ReviewTimeEntry;
using Construction.Application.Features.TimeEntries.Queries.GetTimeEntrySummary;
using Construction.Domain.Enums;
using Construction.UnitTests.Fakes;

namespace Construction.UnitTests.Validation;

public class TimeEntryValidatorTests
{
    private readonly FixedDateTimeProvider _clock = new();
    private readonly CreateTimeEntryCommandValidator _validator;

    public TimeEntryValidatorTests()
    {
        _validator = new CreateTimeEntryCommandValidator(_clock);
    }

    private CreateTimeEntryCommand Valid() => new()
    {
        EmployeeId = Guid.NewGuid(),
        StartedAt = _clock.UtcNow.AddHours(-8),
        EndedAt = _clock.UtcNow,
        BreakMinutes = 30,
        WorkType = WorkType.Regular
    };

    [Fact]
    public void Accepts_a_complete_shift()
    {
        ValidationAssert.Valid(_validator, Valid());
    }

    [Fact]
    public void Accepts_a_shift_that_is_still_running()
    {
        // A supervisor may open a shift for someone whose phone is flat.
        ValidationAssert.Valid(_validator, Valid() with { EndedAt = null, BreakMinutes = 0 });
    }

    [Fact]
    public void Requires_an_employee()
    {
        ValidationAssert.Invalid(
            _validator,
            Valid() with { EmployeeId = Guid.Empty },
            nameof(CreateTimeEntryCommand.EmployeeId));
    }

    [Fact]
    public void Rejects_a_shift_starting_in_the_future()
    {
        ValidationAssert.Invalid(
            _validator,
            Valid() with
            {
                StartedAt = _clock.UtcNow.AddHours(2),
                EndedAt = _clock.UtcNow.AddHours(3)
            },
            nameof(CreateTimeEntryCommand.StartedAt));
    }

    [Fact]
    public void Allows_a_few_minutes_of_clock_skew_on_the_start()
    {
        // A phone a minute or two fast must not be unable to record work.
        ValidationAssert.Valid(
            _validator,
            Valid() with
            {
                StartedAt = _clock.UtcNow.AddMinutes(-60),
                EndedAt = _clock.UtcNow.AddMinutes(2)
            });
    }

    [Fact]
    public void Rejects_a_shift_older_than_the_backdating_limit()
    {
        // Otherwise a closed payroll period could be rewritten.
        var tooOld = _clock.UtcNow - TimeEntryRules.MaxBackdating - TimeSpan.FromDays(1);

        ValidationAssert.Invalid(
            _validator,
            Valid() with { StartedAt = tooOld, EndedAt = tooOld.AddHours(8) },
            nameof(CreateTimeEntryCommand.StartedAt));
    }

    [Fact]
    public void Rejects_a_shift_that_ends_before_it_starts()
    {
        ValidationAssert.Invalid(
            _validator,
            Valid() with
            {
                StartedAt = _clock.UtcNow.AddHours(-2),
                EndedAt = _clock.UtcNow.AddHours(-4)
            },
            nameof(CreateTimeEntryCommand.EndedAt));
    }

    [Fact]
    public void Rejects_a_shift_longer_than_the_maximum()
    {
        var start = _clock.UtcNow - TimeEntryRules.MaxShiftDuration - TimeSpan.FromHours(1);

        ValidationAssert.Invalid(
            _validator,
            Valid() with { StartedAt = start, EndedAt = _clock.UtcNow },
            nameof(CreateTimeEntryCommand.EndedAt));
    }

    [Fact]
    public void Rejects_a_negative_break()
    {
        ValidationAssert.Invalid(
            _validator,
            Valid() with { BreakMinutes = -5 },
            nameof(CreateTimeEntryCommand.BreakMinutes));
    }

    [Fact]
    public void Rejects_a_break_that_leaves_no_time_worked()
    {
        // Exactly the shift length, which would record zero paid minutes.
        ValidationAssert.Invalid(
            _validator,
            Valid() with
            {
                StartedAt = _clock.UtcNow.AddHours(-2),
                EndedAt = _clock.UtcNow,
                BreakMinutes = 120
            },
            nameof(CreateTimeEntryCommand.BreakMinutes));
    }

    [Fact]
    public void Accepts_a_break_one_minute_short_of_the_shift()
    {
        ValidationAssert.Valid(
            _validator,
            Valid() with
            {
                StartedAt = _clock.UtcNow.AddHours(-2),
                EndedAt = _clock.UtcNow,
                BreakMinutes = 119
            });
    }

    [Fact]
    public void Rejects_an_unknown_work_type()
    {
        ValidationAssert.Invalid(
            _validator,
            Valid() with { WorkType = (WorkType)99 },
            nameof(CreateTimeEntryCommand.WorkType));
    }
}

public class ClockOutValidatorTests
{
    private readonly ClockOutCommandValidator _validator = new();

    [Fact]
    public void Accepts_a_plain_clock_out()
    {
        ValidationAssert.Valid(_validator, new ClockOutCommand());
    }

    [Fact]
    public void Rejects_a_negative_break()
    {
        ValidationAssert.Invalid(
            _validator,
            new ClockOutCommand { BreakMinutes = -1 },
            nameof(ClockOutCommand.BreakMinutes));
    }

    [Fact]
    public void Accepts_a_position()
    {
        ValidationAssert.Valid(
            _validator,
            new ClockOutCommand { Latitude = 44.8, Longitude = 20.4 });
    }

    [Theory]
    [InlineData(44.8, null)]
    [InlineData(null, 20.4)]
    public void Rejects_half_a_position(double? latitude, double? longitude)
    {
        // Storing one half would put the marker on the null island.
        ValidationAssert.Invalid(
            _validator,
            new ClockOutCommand { Latitude = latitude, Longitude = longitude },
            nameof(ClockOutCommand.Longitude));
    }

    [Fact]
    public void Rejects_an_impossible_latitude()
    {
        ValidationAssert.Invalid(
            _validator,
            new ClockOutCommand { Latitude = 120, Longitude = 20.4 },
            nameof(ClockOutCommand.Latitude));
    }
}

public class ReviewTimeEntryValidatorTests
{
    private readonly ReviewTimeEntryCommandValidator _validator = new();

    [Fact]
    public void Approving_needs_no_note()
    {
        ValidationAssert.Valid(
            _validator,
            new ReviewTimeEntryCommand { Id = Guid.NewGuid(), Approve = true });
    }

    [Fact]
    public void Sending_an_entry_back_requires_a_reason()
    {
        // Otherwise the worker is told to fix something without being told what.
        ValidationAssert.Invalid(
            _validator,
            new ReviewTimeEntryCommand { Id = Guid.NewGuid(), Approve = false },
            nameof(ReviewTimeEntryCommand.Note));
    }

    [Fact]
    public void Accepts_a_rejection_with_a_reason()
    {
        ValidationAssert.Valid(
            _validator,
            new ReviewTimeEntryCommand
            {
                Id = Guid.NewGuid(),
                Approve = false,
                Note = "Break not recorded"
            });
    }
}

public class TimeEntrySummaryValidatorTests
{
    private readonly GetTimeEntrySummaryQueryValidator _validator = new();

    private static readonly DateTime From = new(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Accepts_a_month()
    {
        ValidationAssert.Valid(
            _validator,
            new GetTimeEntrySummaryQuery { From = From, To = From.AddDays(31) });
    }

    [Fact]
    public void Rejects_a_range_that_ends_before_it_starts()
    {
        ValidationAssert.Invalid(
            _validator,
            new GetTimeEntrySummaryQuery { From = From, To = From.AddDays(-1) },
            nameof(GetTimeEntrySummaryQuery.To));
    }

    [Fact]
    public void Rejects_a_range_wider_than_a_year()
    {
        // One request must not be able to scan the whole table.
        ValidationAssert.Invalid(
            _validator,
            new GetTimeEntrySummaryQuery
            {
                From = From,
                To = From + GetTimeEntrySummaryQueryValidator.MaxRange + TimeSpan.FromDays(1)
            },
            nameof(GetTimeEntrySummaryQuery.To));
    }
}
