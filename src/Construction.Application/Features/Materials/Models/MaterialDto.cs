using System.Linq.Expressions;
using Construction.Domain.Entities;

namespace Construction.Application.Features.Materials.Models;

public class MaterialDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = null!;

    public string Unit { get; init; } = null!;

    public decimal Quantity { get; init; }

    public string? Warehouse { get; init; }

    public Guid? ProjectId { get; init; }

    public string? ProjectName { get; init; }

    public DateTime LastUpdated { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime? UpdatedAt { get; init; }
}

/// <summary>
/// How a <see cref="Material"/> becomes an <see cref="MaterialDto"/>.
/// </summary>
/// <remarks>
/// One expression, used two ways: EF Core translates <see cref="Projection"/>
/// into the SELECT list of a query, and <see cref="ToDto"/> runs the same
/// expression compiled, in memory. See <c>EmployeeMapping</c> for why this
/// replaced AutoMapper.
/// </remarks>
public static class MaterialMapping
{
    public static readonly Expression<Func<Material, MaterialDto>> Projection = material =>
        new MaterialDto
        {
            Id = material.Id,
            Name = material.Name,
            Unit = material.Unit,
            Quantity = material.Quantity,
            Warehouse = material.Warehouse,
            ProjectId = material.ProjectId,
            ProjectName = material.Project != null ? material.Project.Name : null,
            LastUpdated = material.LastUpdated,
            CreatedAt = material.CreatedAt,
            UpdatedAt = material.UpdatedAt,
        };

    private static readonly Func<Material, MaterialDto> Compiled = Projection.Compile();

    /// <summary>Maps a record already in memory.</summary>
    public static MaterialDto ToDto(Material material) => Compiled(material);
}
