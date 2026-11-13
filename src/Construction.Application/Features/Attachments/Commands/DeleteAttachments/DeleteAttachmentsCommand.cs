using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Construction.Application.Features.Attachments.Commands.DeleteAttachments;

/// <summary>What one bulk delete did.</summary>
public class DeleteAttachmentsResultDto
{
    public int Removed { get; init; }

    public long FreedBytes { get; init; }

    /// <summary>Left alone because a retention date is still in the future.</summary>
    public int KeptForRetention { get; init; }

    /// <summary>Left alone because those documents are removed from their own screen (the book of money, signed timesheets).</summary>
    public int NotAllowed { get; init; }

    /// <summary>Ids that were already gone.</summary>
    public int NotFound { get; init; }
}

/// <summary>
/// Removes many documents for good: the file and the record, with no trace left. For the documents nobody needs any
/// more, picked from the list of all documents.
/// </summary>
/// <remarks>
/// The same rights as deleting one (Admin and above), the same protection for a retention date, and the same order
/// (bytes first, then the row). It differs from the single delete in one way, on purpose: that one keeps the row as a
/// trace of who removed what; this one is the "delete totally" a person asks for when the document should not exist
/// at all. The log keeps the user and the count.
/// <para>
/// Documents of the book of money and signed timesheets are refused here: their rights live on their own screens
/// (the book has its own grant), and a bulk list is not the place to bypass them.
/// </para>
/// </remarks>
public record DeleteAttachmentsCommand : IRequest<DeleteAttachmentsResultDto>
{
    /// <summary>Most documents one call may remove.</summary>
    public const int MaxIds = 500;

    public IReadOnlyList<Guid> Ids { get; init; } = [];
}

public class DeleteAttachmentsCommandValidator : AbstractValidator<DeleteAttachmentsCommand>
{
    public DeleteAttachmentsCommandValidator()
    {
        RuleFor(x => x.Ids)
            .NotEmpty().WithMessage("Pick at least one document.")
            .Must(ids => ids.Count <= DeleteAttachmentsCommand.MaxIds)
            .WithMessage($"At most {DeleteAttachmentsCommand.MaxIds} documents at a time.");
    }
}

public class DeleteAttachmentsCommandHandler : IRequestHandler<DeleteAttachmentsCommand, DeleteAttachmentsResultDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IFileStorage _storage;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<DeleteAttachmentsCommandHandler> _logger;

    public DeleteAttachmentsCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IFileStorage storage,
        IDateTimeProvider clock,
        ILogger<DeleteAttachmentsCommandHandler> logger)
    {
        _context = context;
        _currentUser = currentUser;
        _storage = storage;
        _clock = clock;
        _logger = logger;
    }

    public async Task<DeleteAttachmentsResultDto> Handle(
        DeleteAttachmentsCommand request,
        CancellationToken cancellationToken)
    {
        if (!AttachmentRules.CanDelete(_currentUser.Role))
        {
            throw new ForbiddenAccessException("You may not delete attachments.");
        }

        var ids = request.Ids.Distinct().ToList();
        var today = _clock.Today;

        var rows = await _context.Attachments
            .AsNoTracking()
            .Where(a => ids.Contains(a.Id))
            .ToListAsync(cancellationToken);

        var removable = new List<(Guid Id, string Key, long Size)>();
        var retained = 0;
        var notAllowed = 0;

        foreach (var row in rows)
        {
            if (AttachmentOwner.Of(row).Type is AttachmentOwnerType.BookEntry or AttachmentOwnerType.SignedTimesheet)
            {
                notAllowed++;
            }
            else if (row.IsRetainedOn(today))
            {
                retained++;
            }
            else
            {
                removable.Add((row.Id, row.StorageKey, row.SizeBytes));
            }
        }

        // Bytes first, row second, like every delete here: a row left without its file is a document that cannot be
        // opened, while a file left without its row is an orphan the cleanup screen can sweep.
        foreach (var document in removable)
        {
            await _storage.DeleteAsync(document.Key, cancellationToken);
        }

        var removed = 0;

        foreach (var batch in removable.Select(d => d.Id).Chunk(500))
        {
            var batchIds = batch;
            removed += await _context.Attachments.IgnoreQueryFilters()
                .Where(a => batchIds.Contains(a.Id))
                .ExecuteDeleteAsync(cancellationToken);
        }

        // Who removed what, in the log: the rows are gone, and the log is what is left to answer the question.
        _logger.LogWarning(
            "Bulk delete by user {UserId}: {Removed} document(s) removed for good, {Retained} kept for retention, {NotAllowed} refused. Ids: {Ids}",
            _currentUser.UserId, removed, retained, notAllowed, string.Join(',', removable.Select(d => d.Id)));

        return new DeleteAttachmentsResultDto
        {
            Removed = removed,
            FreedBytes = removable.Sum(d => d.Size),
            KeptForRetention = retained,
            NotAllowed = notAllowed,
            NotFound = ids.Count - rows.Count,
        };
    }
}
