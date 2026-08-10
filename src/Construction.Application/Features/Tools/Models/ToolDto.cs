using System.Linq.Expressions;
using Construction.Domain.Entities;

namespace Construction.Application.Features.Tools.Models;

public class ToolDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = null!;

    public string? Category { get; init; }

    public string? SerialNumber { get; init; }

    public string? QrCode { get; init; }

    public string Status { get; init; } = null!;

    public Guid? AssignedEmployeeId { get; init; }

    public string? AssignedEmployeeName { get; init; }

    public string? AssignedEmployeeNumber { get; init; }

    public Guid? AssignedProjectId { get; init; }

    public string? AssignedProjectName { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime? UpdatedAt { get; init; }
}

/// <summary>
/// How a <see cref="Tool"/> becomes an <see cref="ToolDto"/>.
/// </summary>
/// <remarks>
/// One expression, used two ways: EF Core translates <see cref="Projection"/>
/// into the SELECT list of a query, and <see cref="ToDto"/> runs the same
/// expression compiled, in memory. See <c>EmployeeMapping</c> for why this
/// replaced AutoMapper.
/// </remarks>
public static class ToolMapping
{
    public static readonly Expression<Func<Tool, ToolDto>> Projection = tool =>
        new ToolDto
        {
            Id = tool.Id,
            Name = tool.Name,
            Category = tool.Category,
            SerialNumber = tool.SerialNumber,
            QrCode = tool.QrCode,
            Status = tool.Status.ToString(),
            AssignedEmployeeId = tool.AssignedEmployeeId,
            AssignedEmployeeName = tool.AssignedEmployee != null
                ? tool.AssignedEmployee.FirstName + " " + tool.AssignedEmployee.LastName
                : null,
            AssignedEmployeeNumber = tool.AssignedEmployee != null
                ? tool.AssignedEmployee.EmployeeNumber
                : null,
            AssignedProjectId = tool.AssignedProjectId,
            AssignedProjectName = tool.AssignedProject != null ? tool.AssignedProject.Name : null,
            CreatedAt = tool.CreatedAt,
            UpdatedAt = tool.UpdatedAt,
        };

    private static readonly Func<Tool, ToolDto> Compiled = Projection.Compile();

    /// <summary>Maps a record already in memory.</summary>
    public static ToolDto ToDto(Tool tool) => Compiled(tool);
}
