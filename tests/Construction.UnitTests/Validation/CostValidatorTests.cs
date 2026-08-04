using Construction.Application.Features.Costs;
using Construction.Application.Features.Costs.Commands.RecordMaterialMovement;
using Construction.Application.Features.Costs.Commands.RecordVehicleExpense;
using Construction.Application.Features.Costs.Commands.SetEmployeeRate;
using Construction.Application.Features.Costs.Queries.GetProjectCosts;
using Construction.Domain.Enums;
using Construction.UnitTests.Fakes;

namespace Construction.UnitTests.Validation;

public class CostValidatorTests
{
    private readonly FixedDateTimeProvider _clock = new();

    private DateOnly Today => DateOnly.FromDateTime(_clock.UtcNow);

    // ---- rates -----------------------------------------------------------

    private static SetEmployeeRateCommand ValidRate() => new()
    {
        EmployeeId = Guid.NewGuid(),
        HourlyRate = 800m
    };

    [Fact]
    public void Accepts_an_ordinary_rate()
    {
        ValidationAssert.Valid(new SetEmployeeRateCommandValidator(), ValidRate());
    }

    [Fact]
    public void Refuses_a_free_hour()
    {
        foreach (var rate in new[] { 0m, -1m })
        {
            ValidationAssert.Invalid(
                new SetEmployeeRateCommandValidator(),
                ValidRate() with { HourlyRate = rate },
                nameof(SetEmployeeRateCommand.HourlyRate));
        }
    }

    [Fact]
    public void Refuses_a_rate_with_an_extra_three_zeroes()
    {
        // The mistake worth catching: it multiplies every project total that
        // touches this person, and still looks like a number.
        ValidationAssert.Invalid(
            new SetEmployeeRateCommandValidator(),
            ValidRate() with { HourlyRate = CostRules.MaxHourlyRate + 1 },
            nameof(SetEmployeeRateCommand.HourlyRate));
    }

    [Fact]
    public void Refuses_a_rate_that_ends_before_it_starts()
    {
        ValidationAssert.Invalid(
            new SetEmployeeRateCommandValidator(),
            ValidRate() with { StartDate = Today, EndDate = Today.AddDays(-1) },
            nameof(SetEmployeeRateCommand.EndDate));
    }

    // ---- stock movements -------------------------------------------------

    private RecordMaterialMovementCommand ValidMovement() => new()
    {
        MaterialId = Guid.NewGuid(),
        Kind = MaterialMovementKind.In,
        Quantity = 100m,
        UnitPrice = 20m
    };

    [Fact]
    public void Accepts_a_delivery()
    {
        ValidationAssert.Valid(
            new RecordMaterialMovementCommandValidator(_clock), ValidMovement());
    }

    [Fact]
    public void Refuses_a_movement_of_nothing()
    {
        ValidationAssert.Invalid(
            new RecordMaterialMovementCommandValidator(_clock),
            ValidMovement() with { Quantity = 0m },
            nameof(RecordMaterialMovementCommand.Quantity));
    }

    [Theory]
    [InlineData(MaterialMovementKind.In)]
    [InlineData(MaterialMovementKind.Out)]
    public void Refuses_a_negative_delivery_or_issue(MaterialMovementKind kind)
    {
        // Direction lives in Kind for these two; a negative would silently
        // move stock the wrong way.
        ValidationAssert.Invalid(
            new RecordMaterialMovementCommandValidator(_clock),
            ValidMovement() with
            {
                Kind = kind,
                Quantity = -5m,
                ProjectId = Guid.NewGuid()
            },
            nameof(RecordMaterialMovementCommand.Quantity));
    }

    [Fact]
    public void Accepts_a_correction_in_either_direction()
    {
        // Unlike the other two, a correction genuinely is a signed delta: the
        // shelf was counted and there is more or less than the books said.
        foreach (var quantity in new[] { 5m, -5m })
        {
            ValidationAssert.Valid(
                new RecordMaterialMovementCommandValidator(_clock),
                ValidMovement() with
                {
                    Kind = MaterialMovementKind.Adjustment,
                    Quantity = quantity,
                    UnitPrice = null
                });
        }
    }

    [Fact]
    public void Refuses_issuing_stock_with_no_site()
    {
        // Otherwise the material leaves the shelf and lands on no report.
        ValidationAssert.Invalid(
            new RecordMaterialMovementCommandValidator(_clock),
            ValidMovement() with { Kind = MaterialMovementKind.Out, UnitPrice = null },
            nameof(RecordMaterialMovementCommand.ProjectId));
    }

    [Fact]
    public void Refuses_stock_moving_in_the_future()
    {
        ValidationAssert.Invalid(
            new RecordMaterialMovementCommandValidator(_clock),
            ValidMovement() with { OccurredOn = Today.AddDays(1) },
            nameof(RecordMaterialMovementCommand.OccurredOn));
    }

    [Fact]
    public void Accepts_an_invoice_that_turned_up_months_late()
    {
        // June's fuel invoice arriving in August has to go against June.
        ValidationAssert.Valid(
            new RecordMaterialMovementCommandValidator(_clock),
            ValidMovement() with
            {
                OccurredOn = Today.AddDays(-CostRules.MaxBackdatingDays)
            });

        ValidationAssert.Invalid(
            new RecordMaterialMovementCommandValidator(_clock),
            ValidMovement() with
            {
                OccurredOn = Today.AddDays(-CostRules.MaxBackdatingDays - 1)
            },
            nameof(RecordMaterialMovementCommand.OccurredOn));
    }

    // ---- vehicle expenses ------------------------------------------------

    private RecordVehicleExpenseCommand ValidExpense() => new()
    {
        VehicleId = Guid.NewGuid(),
        Kind = VehicleExpenseKind.Service,
        Amount = 28_000m
    };

    [Fact]
    public void Accepts_a_service()
    {
        ValidationAssert.Valid(
            new RecordVehicleExpenseCommandValidator(_clock), ValidExpense());
    }

    [Fact]
    public void Refuses_a_fill_up_with_no_litres()
    {
        // Mirrors the database's check constraint, which itself had to be
        // written as a CASE — the obvious OR form let this through.
        ValidationAssert.Invalid(
            new RecordVehicleExpenseCommandValidator(_clock),
            ValidExpense() with { Kind = VehicleExpenseKind.Fuel },
            nameof(RecordVehicleExpenseCommand.Litres));
    }

    [Fact]
    public void Refuses_a_fill_up_of_zero_litres()
    {
        ValidationAssert.Invalid(
            new RecordVehicleExpenseCommandValidator(_clock),
            ValidExpense() with { Kind = VehicleExpenseKind.Fuel, Litres = 0m },
            nameof(RecordVehicleExpenseCommand.Litres));
    }

    [Theory]
    [InlineData(VehicleExpenseKind.Service)]
    [InlineData(VehicleExpenseKind.Insurance)]
    [InlineData(VehicleExpenseKind.Registration)]
    public void Refuses_litres_on_anything_but_fuel(VehicleExpenseKind kind)
    {
        // Without this the fuel report starts counting insurance premiums.
        ValidationAssert.Invalid(
            new RecordVehicleExpenseCommandValidator(_clock),
            ValidExpense() with { Kind = kind, Litres = 40m },
            nameof(RecordVehicleExpenseCommand.Litres));
    }

    [Fact]
    public void Accepts_a_fill_up_with_litres()
    {
        ValidationAssert.Valid(
            new RecordVehicleExpenseCommandValidator(_clock),
            ValidExpense() with
            {
                Kind = VehicleExpenseKind.Fuel,
                Amount = 10_000m,
                Litres = 50m,
                OdometerKm = 142_000
            });
    }

    [Fact]
    public void Refuses_a_negative_amount()
    {
        ValidationAssert.Invalid(
            new RecordVehicleExpenseCommandValidator(_clock),
            ValidExpense() with { Amount = -1m },
            nameof(RecordVehicleExpenseCommand.Amount));
    }

    // ---- the report window -----------------------------------------------

    [Fact]
    public void Accepts_a_year()
    {
        ValidationAssert.Valid(
            new GetProjectCostsQueryValidator(),
            new GetProjectCostsQuery { From = Today, To = Today.AddDays(364) });
    }

    [Fact]
    public void Refuses_a_period_past_the_limit()
    {
        ValidationAssert.Invalid(
            new GetProjectCostsQueryValidator(),
            new GetProjectCostsQuery
            {
                From = Today,
                To = Today.AddDays(GetProjectCostsQuery.MaxDays)
            },
            nameof(GetProjectCostsQuery.To));
    }

    [Fact]
    public void Refuses_a_period_with_no_dates()
    {
        ValidationAssert.Invalid(
            new GetProjectCostsQueryValidator(),
            new GetProjectCostsQuery(),
            nameof(GetProjectCostsQuery.From));
    }
}
