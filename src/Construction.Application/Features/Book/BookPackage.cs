using System.Globalization;
using System.IO.Compression;
using System.Text;
using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Common.Spreadsheets;
using Construction.Application.Features.Attachments.Queries.ExportAttachments;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Construction.Application.Features.Book;

/// <summary>
/// The filtered book as one ZIP for the accountant: the spreadsheet and, beside it, every document filed on
/// the lines it holds, in a folder per line named after its date, description and amount.
/// </summary>
public record GetBookPackageQuery : IBookRowFilter, IRequest<AttachmentExport>
{
    public DateOnly? From { get; init; }

    public DateOnly? To { get; init; }

    public Domain.Enums.BookEntryKind? Kind { get; init; }

    public Guid? CategoryId { get; init; }

    public Guid? ProjectId { get; init; }

    public bool? Open { get; init; }

    public string? Search { get; init; }

    public Guid? BranchId { get; init; }
}

public class GetBookPackageQueryHandler : IRequestHandler<GetBookPackageQuery, AttachmentExport>
{
    /// <summary>Most bytes in one package, before compression.</summary>
    public const long MaxTotalBytes = 500L * 1024 * 1024;

    private const int MaxRows = 20_000;

    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly GetBookEntriesQueryHandler _entries;
    private readonly GetBookExportQueryHandler _sheet;
    private readonly IFileStorage _storage;
    private readonly ILogger<GetBookPackageQueryHandler> _logger;

    public GetBookPackageQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ISpreadsheetWriter writer,
        IDateTimeProvider dateTimeProvider,
        IFileStorage storage,
        ILogger<GetBookPackageQueryHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _entries = new GetBookEntriesQueryHandler(context, currentUserService);
        _sheet = new GetBookExportQueryHandler(context, currentUserService, writer, dateTimeProvider);
        _storage = storage;
        _logger = logger;
    }

    private sealed record Found(BookRowDto Row, string FileName, string StorageKey, long SizeBytes);

    public async Task<AttachmentExport> Handle(GetBookPackageQuery request, CancellationToken cancellationToken)
    {
        // Whoever may read the book gets the lines; the same handlers decide that for the screen and the sheet.
        var page = await _entries.Handle(new GetBookEntriesQuery
        {
            PageNumber = 1,
            PageSize = MaxRows,
            From = request.From,
            To = request.To,
            Kind = request.Kind,
            CategoryId = request.CategoryId,
            ProjectId = request.ProjectId,
            Open = request.Open,
            Search = request.Search,
            BranchId = request.BranchId,
        }, cancellationToken);

        var spreadsheet = await _sheet.Handle(new GetBookExportQuery
        {
            From = request.From,
            To = request.To,
            Kind = request.Kind,
            CategoryId = request.CategoryId,
            ProjectId = request.ProjectId,
            Open = request.Open,
            Search = request.Search,
            BranchId = request.BranchId,
        }, cancellationToken);

        var withDocuments = page.Items.Where(r => r.DocumentCount > 0).ToList();
        var files = new List<Found>();

        List<BookRowDto> RowsOf(string source) => withDocuments.Where(r => r.Source == source).ToList();

        var entryRows = RowsOf("BookEntry");
        if (entryRows.Count > 0)
        {
            var ids = entryRows.Select(r => r.Id).Distinct().ToList();
            var found = await _context.Attachments.AsNoTracking()
                .Where(a => a.BookEntryId != null && ids.Contains(a.BookEntryId.Value))
                .Select(a => new { Owner = a.BookEntryId!.Value, a.FileName, a.StorageKey, a.SizeBytes })
                .ToListAsync(cancellationToken);
            foreach (var a in found)
                foreach (var row in entryRows.Where(r => r.Id == a.Owner))
                    files.Add(new Found(row, a.FileName, a.StorageKey, a.SizeBytes));
        }

        var vehicleRows = RowsOf("VehicleExpense");
        if (vehicleRows.Count > 0)
        {
            var ids = vehicleRows.Select(r => r.Id).Distinct().ToList();
            var found = await _context.Attachments.AsNoTracking()
                .Where(a => a.VehicleExpenseId != null && ids.Contains(a.VehicleExpenseId.Value))
                .Select(a => new { Owner = a.VehicleExpenseId!.Value, a.FileName, a.StorageKey, a.SizeBytes })
                .ToListAsync(cancellationToken);
            foreach (var a in found)
                foreach (var row in vehicleRows.Where(r => r.Id == a.Owner))
                    files.Add(new Found(row, a.FileName, a.StorageKey, a.SizeBytes));
        }

        var toolRows = RowsOf("ToolExpense");
        if (toolRows.Count > 0)
        {
            var ids = toolRows.Select(r => r.Id).Distinct().ToList();
            var found = await _context.Attachments.AsNoTracking()
                .Where(a => a.ToolExpenseId != null && ids.Contains(a.ToolExpenseId.Value))
                .Select(a => new { Owner = a.ToolExpenseId!.Value, a.FileName, a.StorageKey, a.SizeBytes })
                .ToListAsync(cancellationToken);
            foreach (var a in found)
                foreach (var row in toolRows.Where(r => r.Id == a.Owner))
                    files.Add(new Found(row, a.FileName, a.StorageKey, a.SizeBytes));
        }

        var generalRows = RowsOf("GeneralExpense");
        if (generalRows.Count > 0)
        {
            var ids = generalRows.Select(r => r.Id).Distinct().ToList();
            var found = await _context.Attachments.AsNoTracking()
                .Where(a => a.GeneralExpenseId != null && ids.Contains(a.GeneralExpenseId.Value))
                .Select(a => new { Owner = a.GeneralExpenseId!.Value, a.FileName, a.StorageKey, a.SizeBytes })
                .ToListAsync(cancellationToken);
            foreach (var a in found)
                foreach (var row in generalRows.Where(r => r.Id == a.Owner))
                    files.Add(new Found(row, a.FileName, a.StorageKey, a.SizeBytes));
        }

        if (files.Sum(f => f.SizeBytes) > MaxTotalBytes)
        {
            throw new ConflictException(
                $"The documents are larger than {MaxTotalBytes / (1024 * 1024)} MB. Narrow the period and export again.");
        }

        // A temp file, removed once the response has been sent, for the same reasons as the document export.
        var archiveStream = new FileStream(
            Path.Combine(Path.GetTempPath(), $"book-{Guid.NewGuid():N}.zip"),
            FileMode.CreateNew,
            FileAccess.ReadWrite,
            FileShare.None,
            81920,
            FileOptions.DeleteOnClose | FileOptions.Asynchronous);

        var missing = new List<string>();

        try
        {
            using (var archive = new ZipArchive(archiveStream, ZipArchiveMode.Create, leaveOpen: true))
            {
                var sheetEntry = archive.CreateEntry("knjiga.xlsx", CompressionLevel.Fastest);
                await using (var target = sheetEntry.Open())
                {
                    await target.WriteAsync(spreadsheet.Content, cancellationToken);
                }

                var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var file in files.OrderBy(f => f.Row.OccurredOn).ThenBy(f => f.Row.Description).ThenBy(f => f.FileName))
                {
                    await using var source = await _storage.OpenReadAsync(file.StorageKey, cancellationToken);

                    if (source is null)
                    {
                        missing.Add(file.FileName);
                        continue;
                    }

                    var path = Unique(used, $"Dokumenti/{FolderOf(file.Row)}", Segment(file.FileName));

                    var entry = archive.CreateEntry(path, CompressionLevel.Fastest);

                    await using var target = entry.Open();
                    await source.CopyToAsync(target, cancellationToken);
                }

                var undocumented = page.Items.Count(r => r.DocumentCount == 0 && r.Kind == "Cost");

                var text = new StringBuilder();
                text.AppendLine("knjiga.xlsx - zapisi knjige za izabrani period.");
                text.AppendLine("Dokumenti/ - po jedna mapa za svaki zapis: datum, opis i iznos.");
                text.AppendLine($"Troskova bez dokumenta u ovom izvozu: {undocumented}.");

                if (missing.Count > 0)
                {
                    text.AppendLine();
                    text.AppendLine("Sljedeci fajlovi su zabiljezeni, ali nisu pronadjeni u skladistu:");
                    foreach (var name in missing) text.AppendLine(name);
                }

                var note = archive.CreateEntry("_napomena.txt");
                await using var noteStream = note.Open();
                await noteStream.WriteAsync(Encoding.UTF8.GetBytes(text.ToString()), cancellationToken);
            }

            archiveStream.Position = 0;
        }
        catch
        {
            await archiveStream.DisposeAsync();
            throw;
        }

        _logger.LogInformation(
            "User {UserId} exported the book with {Count} document(s) ({Missing} missing) as a ZIP.",
            _currentUserService.UserId,
            files.Count - missing.Count,
            missing.Count);

        var from = request.From?.ToString("yyyy-MM-dd") ?? "pocetak";
        var to = request.To?.ToString("yyyy-MM-dd") ?? "danas";

        return new AttachmentExport(archiveStream, $"knjiga-{from}-{to}.zip");
    }

    /// <summary>"2026-05-03 Osiguranje -250.00": readable, and sorted by date in a folder listing.</summary>
    private static string FolderOf(BookRowDto row)
    {
        var what = row.Description ?? row.CategoryName ?? row.Counterparty ?? "zapis";
        var sign = row.Kind is "Income" or "Payment" ? string.Empty : "-";
        var amount = sign + row.Amount.ToString("0.00", CultureInfo.InvariantCulture);

        return Segment($"{row.OccurredOn:yyyy-MM-dd} {what} {amount}");
    }

    /// <summary>One path component made safe: no separators, no characters Windows refuses, no leading or trailing dots.</summary>
    private static string Segment(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(
            value.Select(c => invalid.Contains(c) || c is '/' or '\\' or ':' ? '_' : c).ToArray())
            .Trim()
            .Trim('.');

        return cleaned.Length == 0 ? "_" : cleaned.Length > 120 ? cleaned[..120] : cleaned;
    }

    private static string Unique(HashSet<string> used, string directory, string fileName)
    {
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
