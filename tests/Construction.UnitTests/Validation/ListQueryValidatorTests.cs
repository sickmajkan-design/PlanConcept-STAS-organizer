using Construction.Application.Features.Employees.Queries.GetEmployees;
using Construction.Application.Features.Materials.Queries.GetMaterials;
using Construction.Application.Features.Projects.Queries.GetProjects;
using Construction.Application.Features.Tools.Queries.GetTools;
using Construction.Application.Features.Vehicles.Queries.GetVehicles;

namespace Construction.UnitTests.Validation;

/// <summary>
/// The list endpoints accept a client-supplied sort field. It is checked
/// against an allow-list rather than passed through, so these tests guard
/// both the paging bounds and that guard.
/// </summary>
public class ListQueryValidatorTests
{
    [Fact]
    public void Employees_reject_a_page_number_below_one()
    {
        var validator = new GetEmployeesQueryValidator();

        ValidationAssert.Invalid(validator, new GetEmployeesQuery { PageNumber = 0 }, "PageNumber");
        ValidationAssert.Valid(validator, new GetEmployeesQuery { PageNumber = 1 });
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Employees_reject_a_page_size_outside_the_allowed_range(int pageSize)
    {
        ValidationAssert.Invalid(
            new GetEmployeesQueryValidator(),
            new GetEmployeesQuery { PageSize = pageSize },
            "PageSize");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    public void Employees_accept_page_sizes_at_the_boundaries(int pageSize)
    {
        ValidationAssert.Valid(
            new GetEmployeesQueryValidator(), new GetEmployeesQuery { PageSize = pageSize });
    }

    [Fact]
    public void Employees_reject_an_unknown_sort_field()
    {
        ValidationAssert.Invalid(
            new GetEmployeesQueryValidator(),
            new GetEmployeesQuery { SortBy = "; drop table employees" },
            "SortBy");
    }

    [Fact]
    public void Every_advertised_sort_field_is_accepted_case_insensitively()
    {
        foreach (var field in GetEmployeesQuery.AllowedSortFields)
        {
            ValidationAssert.Valid(
                new GetEmployeesQueryValidator(), new GetEmployeesQuery { SortBy = field });
            ValidationAssert.Valid(
                new GetEmployeesQueryValidator(),
                new GetEmployeesQuery { SortBy = field.ToUpperInvariant() });
        }

        foreach (var field in GetProjectsQuery.AllowedSortFields)
        {
            ValidationAssert.Valid(
                new GetProjectsQueryValidator(), new GetProjectsQuery { SortBy = field });
        }

        foreach (var field in GetVehiclesQuery.AllowedSortFields)
        {
            ValidationAssert.Valid(
                new GetVehiclesQueryValidator(), new GetVehiclesQuery { SortBy = field });
        }

        foreach (var field in GetToolsQuery.AllowedSortFields)
        {
            ValidationAssert.Valid(new GetToolsQueryValidator(), new GetToolsQuery { SortBy = field });
        }

        foreach (var field in GetMaterialsQuery.AllowedSortFields)
        {
            ValidationAssert.Valid(
                new GetMaterialsQueryValidator(), new GetMaterialsQuery { SortBy = field });
        }
    }

    [Fact]
    public void An_absent_sort_field_is_allowed_and_falls_back_to_the_default()
    {
        ValidationAssert.Valid(new GetVehiclesQueryValidator(), new GetVehiclesQuery { SortBy = null });
        ValidationAssert.Valid(new GetToolsQueryValidator(), new GetToolsQuery { SortBy = "  " });
    }

    [Fact]
    public void Unknown_sort_fields_are_rejected_on_every_resource()
    {
        ValidationAssert.Invalid(
            new GetProjectsQueryValidator(), new GetProjectsQuery { SortBy = "hackme" }, "SortBy");
        ValidationAssert.Invalid(
            new GetVehiclesQueryValidator(), new GetVehiclesQuery { SortBy = "hackme" }, "SortBy");
        ValidationAssert.Invalid(
            new GetToolsQueryValidator(), new GetToolsQuery { SortBy = "hackme" }, "SortBy");
        ValidationAssert.Invalid(
            new GetMaterialsQueryValidator(), new GetMaterialsQuery { SortBy = "hackme" }, "SortBy");
    }

    [Fact]
    public void Materials_reject_a_negative_max_quantity_filter()
    {
        ValidationAssert.Invalid(
            new GetMaterialsQueryValidator(),
            new GetMaterialsQuery { MaxQuantity = -1m },
            "MaxQuantity");
        ValidationAssert.Valid(
            new GetMaterialsQueryValidator(), new GetMaterialsQuery { MaxQuantity = 0m });
    }
}
