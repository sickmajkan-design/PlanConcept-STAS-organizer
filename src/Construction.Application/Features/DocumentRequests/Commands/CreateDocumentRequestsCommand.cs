using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.DocumentRequests.Models;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.DocumentRequests.Commands.CreateDocumentRequests;

/// <summary>One document asked for.</summary>
public record DocumentRequestItem
{
    public AttachmentCategory Category { get; init; }

    public string Title { get; init; } = null!;

    public string? Note { get; init; }

    public DateOnly? DueDate { get; init; }
}

/// <summary>What was created and how many were left out because the same one was already open.</summary>
public record CreateDocumentRequestsResult(IReadOnlyList<DocumentRequestDto> Created, int Skipped);

/// <summary>
/// Asks one or more people for one or more documents. A "new worker" package is the same call
/// with several items; each becomes its own request with its own status.
/// </summary>
public record CreateDocumentRequestsCommand : IRequest<CreateDocumentRequestsResult>
{
    public List<Guid> EmployeeIds { get; init; } = [];

    public List<DocumentRequestItem> Items { get; init; } = [];
}

public class CreateDocumentRequestsCommandValidator : AbstractValidator<CreateDocumentRequestsCommand>
{
    public CreateDocumentRequestsCommandValidator()
    {
        RuleFor(x => x.EmployeeIds)
            .NotEmpty().WithMessage("Choose at least one person.")
            .Must(ids => ids.Count <= DocumentRequestRules.MaxEmployeesPerCall)
            .WithMessage($"At most {DocumentRequestRules.MaxEmployeesPerCall} people at a time.");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Choose at least one document.")
            .Must(items => items.Count <= DocumentRequestRules.MaxItemsPerCall)
            .WithMessage($"At most {DocumentRequestRules.MaxItemsPerCall} documents at a time.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.Category)
                .IsInEnum()
                .Must(c => c != AttachmentCategory.Photo)
                .WithMessage("A photograph cannot be asked for as a document.");
            item.RuleFor(i => i.Title)
                .NotEmpty().WithMessage("A title is required.")
                .MaximumLength(DocumentRequestRules.MaxTitleLength);
            item.RuleFor(i => i.Note).MaximumLength(DocumentRequestRules.MaxNoteLength);
        });
    }
}

public class CreateDocumentRequestsCommandHandler
    : IRequestHandler<CreateDocumentRequestsCommand, CreateDocumentRequestsResult>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly INotificationService _notifications;

    public CreateDocumentRequestsCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        INotificationService notifications)
    {
        _context = context;
        _currentUserService = currentUserService;
        _notifications = notifications;
    }

    public async Task<CreateDocumentRequestsResult> Handle(
        CreateDocumentRequestsCommand request,
        CancellationToken cancellationToken)
    {
        if (!DocumentRequestRules.CanManage(_currentUserService.Role))
        {
            throw new ForbiddenAccessException("You may not ask for documents.");
        }

        var userId = _currentUserService.UserId
            ?? throw new ForbiddenAccessException("Sign in to ask for documents.");

        var employeeIds = request.EmployeeIds.Distinct().ToList();

        var employees = await _context.Employees
            .AsNoTracking()
            .Where(e => employeeIds.Contains(e.Id))
            .Select(e => e.Id)
            .ToListAsync(cancellationToken);

        if (employees.Count != employeeIds.Count)
        {
            throw new NotFoundException(nameof(Employee), employeeIds.First(i => !employees.Contains(i)));
        }

        var open = await _context.DocumentRequests
            .AsNoTracking()
            .Where(r => employeeIds.Contains(r.EmployeeId)
                && (r.Status == DocumentRequestStatus.Open
                    || r.Status == DocumentRequestStatus.Submitted
                    || r.Status == DocumentRequestStatus.Returned))
            .Select(r => new { r.EmployeeId, r.Category, r.Title })
            .ToListAsync(cancellationToken);

        var created = new List<DocumentRequest>();
        var skipped = 0;

        foreach (var employeeId in employeeIds)
        {
            var openCount = open.Count(o => o.EmployeeId == employeeId);

            foreach (var item in request.Items)
            {
                var title = item.Title.Trim();

                // The same thing already waiting on the same person is not asked twice.
                var duplicate = open.Any(o => o.EmployeeId == employeeId
                    && o.Category == item.Category
                    && string.Equals(o.Title, title, StringComparison.OrdinalIgnoreCase))
                    || created.Any(c => c.EmployeeId == employeeId
                        && c.Category == item.Category
                        && string.Equals(c.Title, title, StringComparison.OrdinalIgnoreCase));

                if (duplicate)
                {
                    skipped++;
                    continue;
                }

                if (openCount + created.Count(c => c.EmployeeId == employeeId)
                    >= DocumentRequestRules.MaxOpenPerEmployee)
                {
                    throw new ConflictException(
                        $"A person can have at most {DocumentRequestRules.MaxOpenPerEmployee} open requests.");
                }

                var entity = new DocumentRequest
                {
                    Id = Guid.NewGuid(),
                    EmployeeId = employeeId,
                    RequestedByUserId = userId,
                    Category = item.Category,
                    Title = title,
                    Note = string.IsNullOrWhiteSpace(item.Note) ? null : item.Note.Trim(),
                    DueDate = item.DueDate,
                    Status = DocumentRequestStatus.Open
                };

                entity.Events.Add(new DocumentRequestEvent
                {
                    Id = Guid.NewGuid(),
                    Type = DocumentRequestEventType.Created,
                    ActorUserId = userId
                });

                created.Add(entity);
            }
        }

        _context.DocumentRequests.AddRange(created);
        await _context.SaveChangesAsync(cancellationToken);

        // One generic notice per person, however many requests they got: nothing that says what is
        // asked for, since a notice is read on a lock screen.
        foreach (var group in created.GroupBy(c => c.EmployeeId))
        {
            var recipients = await DocumentRequestRules.UserIdsOfEmployeeAsync(
                _context, group.Key, cancellationToken);

            if (recipients.Count == 0)
            {
                continue;
            }

            await _notifications.NotifyUsersAsync(
                recipients,
                NotificationType.DocumentRequested,
                "Request from the office",
                "The office sent you a request. Open the app to see it.",
                new Dictionary<string, string>
                {
                    ["requestId"] = group.First().Id.ToString(),
                    ["count"] = group.Count().ToString()
                },
                cancellationToken: cancellationToken);
        }

        var ids = created.Select(c => c.Id).ToList();

        var dtos = await _context.DocumentRequests
            .AsNoTracking()
            .Where(r => ids.Contains(r.Id))
            .Select(DocumentRequestMapping.Projection)
            .ToListAsync(cancellationToken);

        return new CreateDocumentRequestsResult(dtos, skipped);
    }
}
