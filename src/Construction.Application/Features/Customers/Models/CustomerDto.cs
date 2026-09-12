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

    /// <summary>
    /// Null both when it was never set and when the caller may not see it —
    /// <see cref="ProjectionFor"/> is what tells the two apart, by never
    /// reading the real column for a caller without the grant.
    /// </summary>
    public string? TaxId { get; init; }

    /// <summary>Same visibility as <see cref="TaxId"/>.</summary>
    public string? RegistrationNumber { get; init; }

    /// <summary>Same visibility as <see cref="TaxId"/>.</summary>
    public string? VatNumber { get; init; }

    /// <summary>How many projects (Main and Sub together) currently belong to this customer.</summary>
    public int ProjectCount { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime? UpdatedAt { get; init; }
}

/// <summary>
/// How a <see cref="Customer"/> becomes a <see cref="CustomerDto"/>.
/// </summary>
/// <remarks>
/// Parametrized by the caller's own access to the tax fields — unlike every
/// other mapping in this codebase, which is caller-independent — because
/// <see cref="CustomerDto.TaxId"/>/<see cref="CustomerDto.RegistrationNumber"/>/
/// <see cref="CustomerDto.VatNumber"/> only mean something relative to who is
/// asking. When <paramref name="canViewTaxDetails"/> is false the projection
/// never reads those columns at all — EF turns it into a SQL <c>NULL</c>
/// literal in the SELECT list, so an unpermitted caller's response never
/// carries the value over the wire in the first place, rather than fetching
/// it and hiding it in the UI.
/// </remarks>
public static class CustomerMapping
{
    public static Expression<Func<Customer, CustomerDto>> ProjectionFor(bool canViewTaxDetails) =>
        customer => new CustomerDto
        {
            Id = customer.Id,
            Name = customer.Name,
            ContactPerson = customer.ContactPerson,
            Phone = customer.Phone,
            Email = customer.Email,
            Note = customer.Note,
            TaxId = canViewTaxDetails ? customer.TaxId : null,
            RegistrationNumber = canViewTaxDetails ? customer.RegistrationNumber : null,
            VatNumber = canViewTaxDetails ? customer.VatNumber : null,
            ProjectCount = customer.Projects.Count,
            CreatedAt = customer.CreatedAt,
            UpdatedAt = customer.UpdatedAt,
        };
}
