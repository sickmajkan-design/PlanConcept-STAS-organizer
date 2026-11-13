using Construction.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.SystemStatus;

/// <summary>How full the disk is and how much of it is the uploaded documents. For the Super Admin.</summary>
public class StorageStatusDto
{
    /// <summary>False when the disk could not be read; the disk numbers are then zero.</summary>
    public bool Known { get; init; }

    public long TotalBytes { get; init; }

    public long FreeBytes { get; init; }

    public long UsedBytes { get; init; }

    public double FreePercent { get; init; }

    /// <summary>0 = fine, 1 = low, 2 = critical (see <see cref="DiskSpaceLevel"/>).</summary>
    public int Level { get; init; }

    /// <summary>What the documents in the file store add up to, by what the database says (not a scan of the disk).</summary>
    public long DocumentsBytes { get; init; }

    public int DocumentsCount { get; init; }
}

public record GetStorageStatusQuery : IRequest<StorageStatusDto>;

public class GetStorageStatusQueryHandler : IRequestHandler<GetStorageStatusQuery, StorageStatusDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IDiskSpace _disk;

    public GetStorageStatusQueryHandler(IApplicationDbContext context, IDiskSpace disk)
    {
        _context = context;
        _disk = disk;
    }

    public async Task<StorageStatusDto> Handle(GetStorageStatusQuery request, CancellationToken cancellationToken)
    {
        var documents = await _context.Attachments
            .AsNoTracking()
            .GroupBy(_ => 1)
            .Select(g => new { Count = g.Count(), Bytes = g.Sum(a => a.SizeBytes) })
            .FirstOrDefaultAsync(cancellationToken);

        if (_disk.Read() is not { } reading)
        {
            return new StorageStatusDto { DocumentsBytes = documents?.Bytes ?? 0, DocumentsCount = documents?.Count ?? 0 };
        }

        return new StorageStatusDto
        {
            Known = true,
            TotalBytes = reading.TotalBytes,
            FreeBytes = reading.FreeBytes,
            UsedBytes = reading.TotalBytes - reading.FreeBytes,
            FreePercent = Math.Round(reading.FreeFraction * 100, 1),
            Level = (int)reading.Level,
            DocumentsBytes = documents?.Bytes ?? 0,
            DocumentsCount = documents?.Count ?? 0,
        };
    }
}
