using AutoMapper;
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

public class AbsenceDtoMappingProfile : Profile
{
    public AbsenceDtoMappingProfile()
    {
        CreateMap<Absence, AbsenceDto>()
            .ForMember(d => d.EmployeeName, opt => opt.MapFrom(s =>
                s.Employee.FirstName + " " + s.Employee.LastName))
            // Spelled out so it becomes SQL; the entity's computed property
            // cannot be translated by ProjectTo.
            .ForMember(d => d.DayCount, opt => opt.MapFrom(s =>
                s.EndDate.DayNumber - s.StartDate.DayNumber + 1))
            .ForMember(d => d.RequestedByName, opt => opt.MapFrom(s =>
                s.RequestedByUser != null ? s.RequestedByUser.Email : null))
            .ForMember(d => d.ReviewedByName, opt => opt.MapFrom(s =>
                s.ReviewedByUser != null ? s.ReviewedByUser.Email : null));
    }
}
