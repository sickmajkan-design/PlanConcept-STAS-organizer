using AutoMapper;
using Construction.Domain.Entities;

namespace Construction.Application.Features.Notifications.Models;

public class NotificationDto
{
    public Guid Id { get; init; }

    public string Type { get; init; } = null!;

    public string Title { get; init; } = null!;

    public string Body { get; init; } = null!;

    /// <summary>Optional JSON payload with deep-link data (entity ids etc.).</summary>
    public string? DataJson { get; init; }

    public bool IsRead { get; init; }

    public DateTime? ReadAt { get; init; }

    public DateTime CreatedAt { get; init; }
}

public class NotificationDtoMappingProfile : Profile
{
    public NotificationDtoMappingProfile()
    {
        CreateMap<Notification, NotificationDto>()
            .ForMember(d => d.Type, opt => opt.MapFrom(s => s.Type.ToString()));
    }
}
