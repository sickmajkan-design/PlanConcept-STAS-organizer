using System.Linq.Expressions;
using Construction.Domain.Entities;
using Construction.Domain.Enums;

namespace Construction.Application.Features.DocumentRequests.Models;

/// <summary>One file sent in answer to a request.</summary>
public record DocumentRequestFileDto(Guid Id, string FileName, string ContentType, long SizeBytes);

/// <summary>One step of a request's history.</summary>
/// <remarks>
/// <see cref="ActorName"/> is filled for the office and null for the person the request is about,
/// who is only told whether it was them or the office (<see cref="ByEmployee"/>).
/// </remarks>
public record DocumentRequestEventDto(
    DocumentRequestEventType Type,
    DateTime At,
    bool ByEmployee,
    string? ActorName,
    string? Reason,
    IReadOnlyList<DocumentRequestFileDto> Files);

public record DocumentRequestDto
{
    public Guid Id { get; init; }

    public Guid EmployeeId { get; init; }

    public string EmployeeName { get; init; } = null!;

    public AttachmentCategory Category { get; init; }

    public string Title { get; init; } = null!;

    public string? Note { get; init; }

    public DateOnly? DueDate { get; init; }

    public DocumentRequestStatus Status { get; init; }

    public int Attempts { get; init; }

    /// <summary>Who asked. Left out for the person being asked.</summary>
    public string? RequestedByName { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime? ReviewedAt { get; init; }

    public string? ReturnReason { get; init; }

    /// <summary>The files of the latest answer, when there is one. Only on a single request.</summary>
    public IReadOnlyList<DocumentRequestFileDto> Files { get; init; } = [];

    /// <summary>The history, oldest first. Only on a single request.</summary>
    public IReadOnlyList<DocumentRequestEventDto> Events { get; init; } = [];
}

public static class DocumentRequestMapping
{
    public static readonly Expression<Func<DocumentRequest, DocumentRequestDto>> Projection = r =>
        new DocumentRequestDto
        {
            Id = r.Id,
            EmployeeId = r.EmployeeId,
            EmployeeName = r.Employee.FirstName + " " + r.Employee.LastName,
            Category = r.Category,
            Title = r.Title,
            Note = r.Note,
            DueDate = r.DueDate,
            Status = r.Status,
            Attempts = r.Attempts,
            RequestedByName = r.RequestedByUser.Email,
            CreatedAt = r.CreatedAt,
            ReviewedAt = r.ReviewedAt,
            ReturnReason = r.ReturnReason,
            // Filled in when a single request is loaded; a list row carries neither.
            Files = Array.Empty<DocumentRequestFileDto>(),
            Events = Array.Empty<DocumentRequestEventDto>()
        };
}
