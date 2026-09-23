using System.IO.Compression;
using System.Text;
using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Attachments.Models;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Construction.Application.Features.Attachments.Queries.ExportAttachments;

/// <summary>A ZIP archive, ready to stream, and the name to offer it under.</summary>
public record AttachmentExport(Stream Content, string FileName);

/// <summary>
/// Packs the chosen documents into one ZIP, filed the way a person would file
/// them: <c>Radnici/Marko Petrović/Ljekarski pregledi/nalaz.pdf</c>.
/// </summary>
/// <remarks>
/// <para>
/// The readable structure exists only inside the archive. On disk the files
/// keep their opaque keys on purpose: a name or a medical category in a storage
/// path ends up in backups, logs and object-store listings, and renaming an
/// employee would otherwise mean moving files. The archive is the one place a
/// human-readable layout is wanted, and it is built per request.
/// </para>
/// <para>
/// Each document is checked with the same rule as opening it on its own, so an
/// export can never hand over more than the caller could already download one
/// file at a time. Documents the caller may not read are left out silently, as
/// the single-file endpoint answers "not found" for them.
/// </para>
/// </remarks>
public record ExportAttachmentsQuery(IReadOnlyList<Guid> Ids) : IRequest<AttachmentExport>;

public class ExportAttachmentsQueryValidator : AbstractValidator<ExportAttachmentsQuery>
{
    public ExportAttachmentsQueryValidator()
    {
        RuleFor(x => x.Ids)
            .NotEmpty().WithMessage("Choose at least one document.")
            .Must(ids => ids.Count <= ExportAttachmentsQueryHandler.MaxDocuments)
            .WithMessage(
                $"At most {ExportAttachmentsQueryHandler.MaxDocuments} documents can be exported at once.");
    }
}

public class ExportAttachmentsQueryHandler : IRequestHandler<ExportAttachmentsQuery, AttachmentExport>
{
    /// <summary>Most documents in one archive — the archive is built on disk, so this bounds the work.</summary>
    public const int MaxDocuments = 500;

    /// <summary>Most bytes in one archive, before compression.</summary>
    public const long MaxTotalBytes = 500L * 1024 * 1024;

    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IFileStorage _storage;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<ExportAttachmentsQueryHandler> _logger;

    public ExportAttachmentsQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IFileStorage storage,
        IDateTimeProvider dateTimeProvider,
        ILogger<ExportAttachmentsQueryHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _storage = storage;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<AttachmentExport> Handle(
        ExportAttachmentsQuery request,
        CancellationToken cancellationToken)
    {
        var ids = request.Ids.Distinct().ToList();

        var dtos = await _context.Attachments
            .AsNoTracking()
            .Where(a => ids.Contains(a.Id))
            .Select(AttachmentMapping.Projection)
            .ToListAsync(cancellationToken);

        var stored = await _context.Attachments
            .AsNoTracking()
            .Where(a => ids.Contains(a.Id))
            .Select(a => new { a.Id, a.StorageKey, a.EmployeeId })
            .ToDictionaryAsync(a => a.Id, cancellationToken);

        var rows = dtos
            .Select(d => new
            {
                Dto = d,
                stored[d.Id].StorageKey,
                stored[d.Id].EmployeeId,
                d.SizeBytes,
            })
            .ToList();

        var readable = rows
            .Where(r =>
                AttachmentRules.CanRead(_currentUserService.Role, r.Dto.OwnerType)
                || (_currentUserService.EmployeeId is not null
                    && r.EmployeeId == _currentUserService.EmployeeId))
            .OrderBy(r => r.Dto.OwnerType)
            .ThenBy(r => r.Dto.OwnerName)
            .ThenBy(r => r.Dto.Category)
            .ThenBy(r => r.Dto.FileName)
            .ToList();

        if (readable.Sum(r => r.SizeBytes) > MaxTotalBytes)
        {
            throw new ConflictException(
                $"The selection is larger than {MaxTotalBytes / (1024 * 1024)} MB. Export fewer documents at a time.");
        }

        // A temp file rather than the response stream: ZipArchive writes
        // synchronously, which the web server does not allow on a response, and
        // holding the archive in memory would put a 500 MB ceiling on RAM.
        // DeleteOnClose removes it as soon as the response has been sent.
        var archiveStream = new FileStream(
            Path.Combine(Path.GetTempPath(), $"export-{Guid.NewGuid():N}.zip"),
            FileMode.CreateNew,
            FileAccess.ReadWrite,
            FileShare.None,
            81920,
            FileOptions.DeleteOnClose | FileOptions.Asynchronous);

        var skipped = new List<string>();

        try
        {
            using (var archive = new ZipArchive(archiveStream, ZipArchiveMode.Create, leaveOpen: true))
            {
                var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var row in readable)
                {
                    await using var source = await _storage.OpenReadAsync(
                        row.StorageKey, cancellationToken);

                    if (source is null)
                    {
                        skipped.Add(row.Dto.FileName);
                        continue;
                    }

                    var path = UniquePath(
                        used,
                        OwnerFolder(row.Dto.OwnerType),
                        Segment(row.Dto.OwnerName ?? "Nepoznato"),
                        CategoryFolder(row.Dto.Category),
                        Segment(row.Dto.FileName));

                    var entry = archive.CreateEntry(path, CompressionLevel.Fastest);

                    await using var target = entry.Open();
                    await source.CopyToAsync(target, cancellationToken);
                }

                if (skipped.Count > 0)
                {
                    var note = archive.CreateEntry("_napomena.txt");

                    await using var noteStream = note.Open();
                    await noteStream.WriteAsync(
                        Encoding.UTF8.GetBytes(
                            "Sljedeci fajlovi su zabiljezeni, ali nisu pronadjeni u skladistu:\n"
                            + string.Join('\n', skipped)),
                        cancellationToken);
                }
            }

            archiveStream.Position = 0;
        }
        catch
        {
            await archiveStream.DisposeAsync();
            throw;
        }

        // Who took what out is exactly what an incident review asks first.
        _logger.LogInformation(
            "User {UserId} exported {Count} document(s) ({Skipped} missing) as a ZIP.",
            _currentUserService.UserId,
            readable.Count - skipped.Count,
            skipped.Count);

        var stamp = _dateTimeProvider.UtcNow.ToString("yyyy-MM-dd");

        return new AttachmentExport(archiveStream, $"dokumenti-{stamp}.zip");
    }

    private static string OwnerFolder(AttachmentOwnerType type) => type switch
    {
        AttachmentOwnerType.Employee => "Radnici",
        AttachmentOwnerType.Project => "Projekti",
        AttachmentOwnerType.Vehicle => "Vozila",
        AttachmentOwnerType.Tool => "Alati",
        _ => "Ostalo",
    };

    private static string CategoryFolder(AttachmentCategory category) => category switch
    {
        AttachmentCategory.Contract => "Ugovori",
        AttachmentCategory.Certificate => "Sertifikati",
        AttachmentCategory.MedicalCheck => "Ljekarski pregledi",
        AttachmentCategory.Licence => "Dozvole",
        AttachmentCategory.Insurance => "Osiguranje",
        AttachmentCategory.SiteDocument => "Dokumenti gradilista",
        AttachmentCategory.Photo => "Fotografije",
        _ => "Ostalo",
    };

    /// <summary>
    /// One path component made safe: no separators, no characters Windows
    /// refuses, no leading or trailing dots — so a name can never climb out of
    /// its folder or produce an archive that will not extract.
    /// </summary>
    private static string Segment(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(
            value.Select(c => invalid.Contains(c) || c is '/' or '\\' or ':' ? '_' : c).ToArray())
            .Trim()
            .Trim('.');

        return cleaned.Length == 0 ? "_" : cleaned.Length > 120 ? cleaned[..120] : cleaned;
    }

    /// <summary>Two documents with the same name in the same folder both survive: the second becomes "name (2).ext".</summary>
    private static string UniquePath(
        HashSet<string> used, string ownerFolder, string ownerName, string categoryFolder, string fileName)
    {
        var directory = $"{ownerFolder}/{ownerName}/{categoryFolder}";
        var candidate = $"{directory}/{fileName}";
        var counter = 2;

        while (!used.Add(candidate))
        {
            var stem = Path.GetFileNameWithoutExtension(fileName);
            var extension = Path.GetExtension(fileName);
            candidate = $"{directory}/{stem} ({counter++}){extension}";
        }

        return candidate;
    }
}
