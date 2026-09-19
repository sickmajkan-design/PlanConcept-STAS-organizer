using System.Linq.Expressions;
using Construction.Domain.Entities;

namespace Construction.Application.Features.Accommodations.Stays;

public class AccommodationStayDto
{
    public Guid Id { get; init; }

    public Guid AccommodationId { get; init; }

    public string AccommodationName { get; init; } = null!;

    public string AccommodationAddress { get; init; } = null!;

    public Guid EmployeeId { get; init; }

    public string EmployeeName { get; init; } = null!;

    public DateOnly StartDate { get; init; }

    public DateOnly? EndDate { get; init; }

    public Guid? ProjectId { get; init; }

    public string? ProjectName { get; init; }

    public string? Note { get; init; }

    public DateTime CreatedAt { get; init; }
}

public static class AccommodationStayMapping
{
    public static readonly Expression<Func<AccommodationStay, AccommodationStayDto>> Projection = stay =>
        new AccommodationStayDto
        {
            Id = stay.Id,
            AccommodationId = stay.AccommodationId,
            AccommodationName = stay.Accommodation.Name ?? stay.Accommodation.Address,
            AccommodationAddress = stay.Accommodation.Address,
            EmployeeId = stay.EmployeeId,
            EmployeeName = stay.Employee.FirstName + " " + stay.Employee.LastName,
            StartDate = stay.StartDate,
            EndDate = stay.EndDate,
            ProjectId = stay.ProjectId,
            ProjectName = stay.Project != null ? stay.Project.Name : null,
            Note = stay.Note,
            CreatedAt = stay.CreatedAt,
        };
}
