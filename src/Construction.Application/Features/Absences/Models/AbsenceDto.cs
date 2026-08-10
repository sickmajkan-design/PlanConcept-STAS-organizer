using System.Linq.Expressions;
using Construction.Domain.Entities;
using Construction.Domain.Enums;

namespace Construction.Application.Features.Absences.Models;

public class AbsenceDto
{
    public Guid Id { get; init; }

    public Guid EmployeeId { get; init; }

    public string EmployeeName { get; init; } = null!;

    public AbsenceType Type { get; init; }

    public AbsenceStatus Status { get; init; }

    public DateOnly StartDate { get; init; }

    public DateOnly EndDate { get; init; }

    public int DayCount { get; init; }

    public string? Reason { get; init; }

    public string? RequestedByName { get; init; }

    public string? ReviewedByName { get; init; }

    public DateTime? ReviewedAt { get; init; }

    public string? ReviewNote { get; init; }

    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// How a <see cref="Absence"/> becomes an <see cref="AbsenceDto"/>.
/// </summary>
/// <remarks>
/// One expression, used two ways: EF Core translates <see cref="Projection"/>
/// into the SELECT list of a query, and <see cref="ToDto"/> runs the same
/// expression compiled, in memory. See <c>EmployeeMapping</c> for why this
/// replaced AutoMapper.
/// </remarks>
public static class AbsenceMapping
{
    public static readonly Expression<Func<Absence, AbsenceDto>> Projection = absence =>
        new AbsenceDto
        {
            Id = absence.Id,
            EmployeeId = absence.EmployeeId,
            EmployeeName = absence.Employee.FirstName + " " + absence.Employee.LastName,
            Type = absence.Type,
            Status = absence.Status,
            StartDate = absence.StartDate,
            EndDate = absence.EndDate,
            // Spelled out so it becomes SQL; the entity's computed property
            // cannot be translated.
            DayCount = absence.EndDate.DayNumber - absence.StartDate.DayNumber + 1,
            Reason = absence.Reason,
            RequestedByName = absence.RequestedByUser != null ? absence.RequestedByUser.Email : null,
            ReviewedByName = absence.ReviewedByUser != null ? absence.ReviewedByUser.Email : null,
            ReviewedAt = absence.ReviewedAt,
            ReviewNote = absence.ReviewNote,
            CreatedAt = absence.CreatedAt,
        };

    private static readonly Func<Absence, AbsenceDto> Compiled = Projection.Compile();

    /// <summary>Maps a record already in memory.</summary>
    public static AbsenceDto ToDto(Absence absence) => Compiled(absence);
}
