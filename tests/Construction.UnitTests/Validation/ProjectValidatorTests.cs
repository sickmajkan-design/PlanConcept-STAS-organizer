using Construction.Application.Features.Projects.Commands.CreateProject;
using Construction.Domain.Enums;

namespace Construction.UnitTests.Validation;

public class ProjectValidatorTests
{
    private readonly CreateProjectCommandValidator _validator = new();

    private static CreateProjectCommand Valid() => new()
    {
        Name = "Riverside Apartments",
        Status = ProjectStatus.Planned
    };

    [Fact]
    public void Accepts_a_minimal_request()
    {
        ValidationAssert.Valid(_validator, Valid());
    }

    [Fact]
    public void Requires_a_name()
    {
        ValidationAssert.Invalid(_validator, Valid() with { Name = "" }, "Name");
    }

    [Theory]
    [InlineData(-90.0)]
    [InlineData(0.0)]
    [InlineData(90.0)]
    public void Accepts_latitudes_within_range(double latitude)
    {
        ValidationAssert.Valid(_validator, Valid() with { Latitude = latitude, Longitude = 15.96 });
    }

    [Theory]
    [InlineData(-90.1)]
    [InlineData(90.1)]
    public void Rejects_latitudes_outside_range(double latitude)
    {
        ValidationAssert.Invalid(
            _validator, Valid() with { Latitude = latitude, Longitude = 15.96 }, "Latitude");
    }

    [Theory]
    [InlineData(-180.1)]
    [InlineData(180.1)]
    public void Rejects_longitudes_outside_range(double longitude)
    {
        ValidationAssert.Invalid(
            _validator, Valid() with { Latitude = 45.8, Longitude = longitude }, "Longitude");
    }

    [Fact]
    public void Rejects_a_latitude_without_a_longitude()
    {
        // Half a coordinate would put a pin nowhere useful on the map.
        ValidationAssert.Invalid(
            _validator, Valid() with { Latitude = 45.8, Longitude = null }, "Latitude");
    }

    [Fact]
    public void Rejects_a_longitude_without_a_latitude()
    {
        ValidationAssert.Invalid(
            _validator, Valid() with { Latitude = null, Longitude = 15.96 }, "Latitude");
    }

    [Fact]
    public void Accepts_a_project_with_no_coordinates_at_all()
    {
        ValidationAssert.Valid(_validator, Valid() with { Latitude = null, Longitude = null });
    }

    [Fact]
    public void Rejects_an_end_date_before_the_start_date()
    {
        ValidationAssert.Invalid(
            _validator,
            Valid() with
            {
                StartDate = new DateOnly(2026, 3, 1),
                EndDate = new DateOnly(2026, 2, 1)
            },
            "EndDate");
    }

    [Fact]
    public void Accepts_an_end_date_equal_to_the_start_date()
    {
        // A single-day job is legitimate.
        ValidationAssert.Valid(_validator, Valid() with
        {
            StartDate = new DateOnly(2026, 3, 1),
            EndDate = new DateOnly(2026, 3, 1)
        });
    }

    [Fact]
    public void Accepts_a_start_date_with_no_end_date()
    {
        ValidationAssert.Valid(_validator, Valid() with
        {
            StartDate = new DateOnly(2026, 3, 1),
            EndDate = null
        });
    }

    [Fact]
    public void Rejects_a_status_outside_the_enum()
    {
        ValidationAssert.Invalid(_validator, Valid() with { Status = (ProjectStatus)99 }, "Status");
    }
}
