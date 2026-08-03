using Construction.Application.Features.Absences;
using Construction.Application.Features.Absences.Commands.RequestAbsence;
using Construction.Application.Features.Absences.Commands.ReviewAbsence;
using Construction.Application.Features.Absences.Queries.GetSchedule;
using Construction.Domain.Enums;
using Construction.UnitTests.Fakes;

namespace Construction.UnitTests.Validation;

public class AbsenceValidatorTests
{
    private readonly FixedDateTimeProvider _clock = new();
    private readonly RequestAbsenceCommandValidator _request;

    public AbsenceValidatorTests()
    {
        _request = new RequestAbsenceCommandValidator(_clock);
    }

    private DateOnly Today => DateOnly.FromDateTime(_clock.UtcNow);

    private RequestAbsenceCommand ValidRequest() => new()
    {
        Type = AbsenceType.AnnualLeave,
        StartDate = Today.AddDays(14),
        EndDate = Today.AddDays(21)
    };

    // ---- requesting leave ------------------------------------------------

    [Fact]
    public void Accepts_a_week_off_next_month()
    {
        ValidationAssert.Valid(_request, ValidRequest());
    }

    [Fact]
    public void Accepts_a_single_day()
    {
        // Start and end on the same date is one day, not zero.
        ValidationAssert.Valid(
            _request,
            ValidRequest() with { StartDate = Today.AddDays(3), EndDate = Today.AddDays(3) });
    }

    [Fact]
    public void Refuses_leave_that_ends_before_it_starts()
    {
        ValidationAssert.Invalid(
            _request,
            ValidRequest() with { StartDate = Today.AddDays(10), EndDate = Today.AddDays(3) },
            nameof(RequestAbsenceCommand.EndDate));
    }

    [Fact]
    public void Refuses_a_missing_start_date()
    {
        ValidationAssert.Invalid(
            _request,
            ValidRequest() with { StartDate = default },
            nameof(RequestAbsenceCommand.StartDate));
    }

    [Fact]
    public void Refuses_a_missing_end_date()
    {
        ValidationAssert.Invalid(
            _request,
            ValidRequest() with { StartDate = Today, EndDate = default },
            nameof(RequestAbsenceCommand.EndDate));
    }

    [Fact]
    public void Accepts_a_sick_day_entered_after_the_fact()
    {
        // The usual case: somebody was ill on Monday and the office types it
        // in on Wednesday.
        ValidationAssert.Valid(
            _request,
            ValidRequest() with
            {
                Type = AbsenceType.SickLeave,
                StartDate = Today.AddDays(-2),
                EndDate = Today.AddDays(-1)
            });
    }

    [Fact]
    public void Refuses_leave_backdated_past_the_limit()
    {
        ValidationAssert.Invalid(
            _request,
            ValidRequest() with
            {
                StartDate = Today.AddDays(-AbsenceRules.MaxBackdatingDays - 1),
                EndDate = Today.AddDays(-AbsenceRules.MaxBackdatingDays)
            },
            nameof(RequestAbsenceCommand.StartDate));
    }

    [Fact]
    public void Accepts_leave_exactly_at_the_backdating_limit()
    {
        // The boundary belongs inside the window, not outside it.
        ValidationAssert.Valid(
            _request,
            ValidRequest() with
            {
                StartDate = Today.AddDays(-AbsenceRules.MaxBackdatingDays),
                EndDate = Today.AddDays(-AbsenceRules.MaxBackdatingDays)
            });
    }

    [Fact]
    public void Refuses_leave_booked_further_ahead_than_anyone_plans()
    {
        // Catches a mistyped year, which is what this rule is really for.
        ValidationAssert.Invalid(
            _request,
            ValidRequest() with
            {
                StartDate = Today.AddYears(10),
                EndDate = Today.AddYears(10).AddDays(5)
            },
            nameof(RequestAbsenceCommand.StartDate));
    }

    [Fact]
    public void Refuses_an_absence_longer_than_leave_can_be()
    {
        // Past this it is a change of employment status, and filing it as
        // leave hides that from everyone.
        ValidationAssert.Invalid(
            _request,
            ValidRequest() with
            {
                StartDate = Today,
                EndDate = Today.AddDays(AbsenceRules.MaxDays)
            },
            nameof(RequestAbsenceCommand.EndDate));
    }

    [Fact]
    public void Accepts_an_absence_exactly_at_the_length_limit()
    {
        ValidationAssert.Valid(
            _request,
            ValidRequest() with
            {
                StartDate = Today,
                EndDate = Today.AddDays(AbsenceRules.MaxDays - 1)
            });
    }

    [Theory]
    [InlineData(0)]   // what a client sending "type": 0 produces
    [InlineData(42)]
    public void Refuses_a_type_outside_the_enum(int type)
    {
        ValidationAssert.Invalid(
            _request,
            ValidRequest() with { Type = (AbsenceType)type },
            nameof(RequestAbsenceCommand.Type));
    }

    [Fact]
    public void Accepts_every_type_the_domain_defines()
    {
        // Including Other = 99, whose out-of-sequence value is easy to mistake
        // for a sentinel.
        foreach (var type in Enum.GetValues<AbsenceType>())
        {
            ValidationAssert.Valid(_request, ValidRequest() with { Type = type });
        }
    }

    [Fact]
    public void Refuses_a_reason_longer_than_the_column()
    {
        ValidationAssert.Invalid(
            _request,
            ValidRequest() with { Reason = new string('a', 1001) },
            nameof(RequestAbsenceCommand.Reason));
    }

    // ---- reviewing it ----------------------------------------------------

    [Fact]
    public void Granting_leave_needs_no_note()
    {
        var validator = new ReviewAbsenceCommandValidator();

        ValidationAssert.Valid(
            validator,
            new ReviewAbsenceCommand { Id = Guid.NewGuid(), Approve = true });
    }

    [Fact]
    public void Refusing_leave_needs_a_reason()
    {
        // Being turned down without one is the complaint this prevents.
        var validator = new ReviewAbsenceCommandValidator();

        ValidationAssert.Invalid(
            validator,
            new ReviewAbsenceCommand { Id = Guid.NewGuid(), Approve = false },
            nameof(ReviewAbsenceCommand.Note));
    }

    [Fact]
    public void Accepts_a_refusal_with_its_reason()
    {
        var validator = new ReviewAbsenceCommandValidator();

        ValidationAssert.Valid(
            validator,
            new ReviewAbsenceCommand
            {
                Id = Guid.NewGuid(),
                Approve = false,
                Note = "Rok na gradilištu te nedelje"
            });
    }

    // ---- the board window ------------------------------------------------

    [Fact]
    public void Accepts_a_week()
    {
        var validator = new GetScheduleQueryValidator();

        ValidationAssert.Valid(
            validator,
            new GetScheduleQuery { From = Today, To = Today.AddDays(6) });
    }

    [Fact]
    public void Accepts_a_window_exactly_at_the_limit()
    {
        var validator = new GetScheduleQueryValidator();

        ValidationAssert.Valid(
            validator,
            new GetScheduleQuery
            {
                From = Today,
                To = Today.AddDays(GetScheduleQuery.MaxDays - 1)
            });
    }

    [Fact]
    public void Refuses_a_window_past_the_limit()
    {
        // The board is one query; an unbounded window makes it an unbounded one.
        var validator = new GetScheduleQueryValidator();

        ValidationAssert.Invalid(
            validator,
            new GetScheduleQuery { From = Today, To = Today.AddDays(GetScheduleQuery.MaxDays) },
            nameof(GetScheduleQuery.To));
    }

    [Fact]
    public void Refuses_a_window_that_ends_before_it_starts()
    {
        var validator = new GetScheduleQueryValidator();

        ValidationAssert.Invalid(
            validator,
            new GetScheduleQuery { From = Today, To = Today.AddDays(-1) },
            nameof(GetScheduleQuery.To));
    }

    [Fact]
    public void Refuses_a_window_with_no_dates()
    {
        var validator = new GetScheduleQueryValidator();

        ValidationAssert.Invalid(
            validator,
            new GetScheduleQuery(),
            nameof(GetScheduleQuery.From));
    }
}
