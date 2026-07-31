using Construction.Application.Features.Employees.Commands.CreateEmployee;
using Construction.Domain.Enums;

namespace Construction.UnitTests.Validation;

public class EmployeeValidatorTests
{
    private readonly CreateEmployeeCommandValidator _validator = new();

    private static CreateEmployeeCommand Valid() => new()
    {
        EmployeeNumber = "EMP-001",
        FirstName = "Ivan",
        LastName = "Horvat",
        Position = "Site Manager",
        EmploymentDate = new DateOnly(2020, 3, 1),
        Status = EmployeeStatus.Active
    };

    [Fact]
    public void Accepts_a_complete_request()
    {
        ValidationAssert.Valid(_validator, Valid());
    }

    [Fact]
    public void Accepts_a_request_with_only_the_required_fields()
    {
        // Phone, email, address, date of birth and photo are all optional.
        ValidationAssert.Valid(_validator, Valid() with
        {
            Phone = null,
            Email = null,
            Address = null,
            DateOfBirth = null,
            PhotoUrl = null
        });
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Requires_an_employee_number(string number)
    {
        ValidationAssert.Invalid(
            _validator, Valid() with { EmployeeNumber = number }, "EmployeeNumber");
    }

    [Fact]
    public void Caps_the_employee_number_at_the_column_width()
    {
        ValidationAssert.Valid(_validator, Valid() with { EmployeeNumber = new string('E', 32) });
        ValidationAssert.Invalid(
            _validator, Valid() with { EmployeeNumber = new string('E', 33) }, "EmployeeNumber");
    }

    [Fact]
    public void Requires_a_first_and_last_name()
    {
        ValidationAssert.Invalid(_validator, Valid() with { FirstName = "" }, "FirstName");
        ValidationAssert.Invalid(_validator, Valid() with { LastName = "" }, "LastName");
    }

    [Fact]
    public void Requires_a_position()
    {
        ValidationAssert.Invalid(_validator, Valid() with { Position = "" }, "Position");
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("@example.com")]
    [InlineData("ivan@")]
    public void Rejects_a_malformed_email(string email)
    {
        ValidationAssert.Invalid(_validator, Valid() with { Email = email }, "Email");
    }

    [Fact]
    public void Accepts_a_well_formed_email()
    {
        ValidationAssert.Valid(_validator, Valid() with { Email = "ivan.horvat@example.com" });
    }

    [Fact]
    public void Accepts_a_dotless_domain_the_way_ASP_NET_Core_does()
    {
        // FluentValidation's EmailAddress() follows ASP.NET Core's rule, which
        // only requires an '@' with something either side — intranet addresses
        // such as ivan@localhost are valid. Documented so a future switch to a
        // stricter regex is a deliberate choice rather than a silent change.
        ValidationAssert.Valid(_validator, Valid() with { Email = "ivan@localhost" });
    }

    [Fact]
    public void Rejects_a_date_of_birth_in_the_future()
    {
        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));

        ValidationAssert.Invalid(_validator, Valid() with { DateOfBirth = tomorrow }, "DateOfBirth");
    }

    [Fact]
    public void Rejects_a_date_of_birth_after_the_employment_date()
    {
        // Someone cannot be hired before they were born.
        ValidationAssert.Invalid(
            _validator,
            Valid() with
            {
                DateOfBirth = new DateOnly(2021, 1, 1),
                EmploymentDate = new DateOnly(2020, 3, 1)
            },
            "DateOfBirth");
    }

    [Fact]
    public void Accepts_a_date_of_birth_before_the_employment_date()
    {
        ValidationAssert.Valid(_validator, Valid() with { DateOfBirth = new DateOnly(1988, 4, 12) });
    }

    [Fact]
    public void Rejects_a_status_outside_the_enum()
    {
        ValidationAssert.Invalid(_validator, Valid() with { Status = (EmployeeStatus)99 }, "Status");
    }
}
