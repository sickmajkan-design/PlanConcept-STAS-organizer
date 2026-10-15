using Construction.Application.Common.Interfaces;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.DocumentRequests;

/// <summary>Who may ask for a document, who may answer, and what an answer has to look like.</summary>
public static class DocumentRequestRules
{
    /// <summary>Most requests that may be waiting on one person at once. Guards against flooding them.</summary>
    public const int MaxOpenPerEmployee = 15;

    /// <summary>Most people one call may ask.</summary>
    public const int MaxEmployeesPerCall = 100;

    /// <summary>Most documents one call may ask each person for.</summary>
    public const int MaxItemsPerCall = 10;

    /// <summary>Most files one answer may carry — the pages of one document.</summary>
    public const int MaxFilesPerSubmission = 5;

    /// <summary>Times a person may send for one request before the office has to sort it out by hand.</summary>
    public const int MaxAttempts = 5;

    public const int MaxTitleLength = 200;

    public const int MaxNoteLength = 500;

    public const int MinReasonLength = 5;

    /// <summary>
    /// What a person may send from the phone. Narrower than the office's list: photographs and PDFs
    /// of paperwork, not spreadsheets or text files.
    /// </summary>
    public static readonly string[] SubmittableExtensions = [".pdf", ".jpg", ".jpeg", ".png", ".webp", ".heic"];

    /// <summary>The office asks and checks: Admin and above.</summary>
    public static bool CanManage(UserRole? role) => role is UserRole.SuperAdmin or UserRole.Admin;

    /// <summary>Categories whose documents lapse, so accepting one needs the date it is valid until.</summary>
    public static bool NeedsExpiry(AttachmentCategory category) => category
        is AttachmentCategory.Identity
        or AttachmentCategory.Visa
        or AttachmentCategory.Licence
        or AttachmentCategory.Certificate
        or AttachmentCategory.MedicalCheck;

    /// <summary>A request that can still be answered.</summary>
    public static bool CanBeAnswered(DocumentRequestStatus status) =>
        status is DocumentRequestStatus.Open or DocumentRequestStatus.Returned;

    /// <summary>The accounts that should hear about something concerning this employee: theirs.</summary>
    public static Task<List<Guid>> UserIdsOfEmployeeAsync(
        IApplicationDbContext context, Guid employeeId, CancellationToken cancellationToken) =>
        context.Users
            .Where(u => u.IsActive && u.EmployeeId == employeeId)
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

    /// <summary>
    /// The office accounts that hear when a person answers: whoever asked, or — when they have gone —
    /// every Admin and Super Admin.
    /// </summary>
    public static async Task<List<Guid>> OfficeRecipientsAsync(
        IApplicationDbContext context,
        DocumentRequest request,
        CancellationToken cancellationToken)
    {
        var requester = await context.Users
            .Where(u => u.Id == request.RequestedByUserId && u.IsActive)
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        if (requester.Count > 0)
        {
            return requester;
        }

        return await context.Users
            .Where(u => u.IsActive && (u.Role == UserRole.SuperAdmin || u.Role == UserRole.Admin))
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);
    }
}
