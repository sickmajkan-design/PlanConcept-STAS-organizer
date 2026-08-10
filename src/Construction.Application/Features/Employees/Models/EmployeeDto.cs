using System.Linq.Expressions;
using Construction.Domain.Entities;

namespace Construction.Application.Features.Employees.Models;

public class EmployeeDto
{
    public Guid Id { get; init; }

    public string EmployeeNumber { get; init; } = null!;

    public string FirstName { get; init; } = null!;

    public string LastName { get; init; } = null!;

    public string FullName { get; init; } = null!;

    public string? Phone { get; init; }

    public string? Email { get; init; }

    public string? Address { get; init; }

    public DateOnly? DateOfBirth { get; init; }

    public DateOnly EmploymentDate { get; init; }

    public string Position { get; init; } = null!;

    public string Status { get; init; } = null!;

    public DateTime CreatedAt { get; init; }

    public DateTime? UpdatedAt { get; init; }
}

/// <summary>
/// How an <see cref="Employee"/> becomes an <see cref="EmployeeDto"/>.
/// </summary>
/// <remarks>
/// <para>
/// One expression, used two ways: EF Core translates
/// <see cref="Projection"/> into the SELECT list of a query, and
/// <see cref="ToDto"/> runs the same expression compiled, in memory. Two
/// mappings that must agree cannot be written twice without eventually
/// disagreeing, so they are not.
/// </para>
/// <para>
/// This is the shape every DTO in this layer follows. It replaced AutoMapper,
/// which is licensed under RPL-1.5 — reciprocal in a way that covers deploying
/// to users, not only shipping source — and whose only permissively licensed
/// releases carry an unfixed high-severity advisory. See the audit, C8.
/// </para>
/// <para>
/// Everything the projection touches must be translatable to SQL: a column, or
/// an expression over columns. Entity properties computed in C# — such as
/// <c>Employee.FullName</c> — are spelled out here instead, because EF cannot
/// turn a compiled property getter into SQL and would otherwise fetch every row
/// to ask it.
/// </para>
/// </remarks>
public static class EmployeeMapping
{
    public static readonly Expression<Func<Employee, EmployeeDto>> Projection = employee =>
        new EmployeeDto
        {
            Id = employee.Id,
            EmployeeNumber = employee.EmployeeNumber,
            FirstName = employee.FirstName,
            LastName = employee.LastName,
            FullName = employee.FirstName + " " + employee.LastName,
            Phone = employee.Phone,
            Email = employee.Email,
            Address = employee.Address,
            DateOfBirth = employee.DateOfBirth,
            EmploymentDate = employee.EmploymentDate,
            Position = employee.Position,
            Status = employee.Status.ToString(),
            CreatedAt = employee.CreatedAt,
            UpdatedAt = employee.UpdatedAt,
        };

    private static readonly Func<Employee, EmployeeDto> Compiled = Projection.Compile();

    /// <summary>Maps an employee already in memory.</summary>
    /// <remarks>
    /// Named rather than an extension method on purpose: two DTOs in different
    /// features both map from <c>User</c>, and a pair of <c>ToDto</c>
    /// extensions in scope together is an ambiguity the compiler reports at
    /// whichever call site happens to import both.
    /// </remarks>
    public static EmployeeDto ToDto(Employee employee) => Compiled(employee);
}
