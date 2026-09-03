using System.Linq.Expressions;
using Construction.Domain.Entities;

namespace Construction.Application.Features.Customers.Models;

public class CustomerDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = null!;

    public string? ContactPerson { get; init; }

    public string? Phone { get; init; }

    public string? Email { get; init; }

    public string? Note { get; init; }

    /// <summary>How many projects (Main and Sub together) currently belong to this customer.</summary>
    public int ProjectCount { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime? UpdatedAt { get; init; }
}

/// <summary>
/// How a <see cref="Customer"/> becomes a <see cref="CustomerDto"/>.
/// </summary>
/// <remarks>
/// One expression, used two ways: EF Core translates <see cref="Projection"/>
/// into the SELECT list of a query, and <see cref="ToDto"/> runs the same
/// expression compiled, in memory. See <c>EmployeeMapping</c> for why this
/// replaced AutoMapper.
/// </remarks>
public static class CustomerMapping
{
    public static readonly Expression<Func<Customer, CustomerDto>> Projection = customer =>
        new CustomerDto
        {
            Id = customer.Id,
            Name = customer.Name,
            ContactPerson = customer.ContactPerson,
            Phone = customer.Phone,
            Email = customer.Email,
            Note = customer.Note,
            ProjectCount = customer.Projects.Count,
            CreatedAt = customer.CreatedAt,
            UpdatedAt = customer.UpdatedAt,
        };

    private static readonly Func<Customer, CustomerDto> Compiled = Projection.Compile();

    /// <summary>Maps a record already in memory.</summary>
    public static CustomerDto ToDto(Customer customer) => Compiled(customer);
}
