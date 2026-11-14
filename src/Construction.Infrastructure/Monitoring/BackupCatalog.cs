using System.Globalization;
using Construction.Application.Features.SystemStatus;
using Microsoft.Extensions.Configuration;

namespace Construction.Infrastructure.Monitoring;

/// <summary>
/// Lists the backups the <c>backup</c> container leaves in its volume, which the API mounts read-only. File names are
/// <c>construction-&lt;stamp&gt;.dump</c>, <c>…-files.tar.gz</c> and a <c>.offsite</c> receipt beside whatever was uploaded
/// (see <c>scripts/backup.sh</c>, <c>scripts/offsite.sh</c>).
/// </summary>
public sealed class BackupCatalog : IBackupCatalog
{
    private const string Stamp = "yyyyMMdd'T'HHmmss'Z'";
    private readonly string _directory;

    public BackupCatalog(IConfiguration configuration)
    {
        _directory = configuration["Backup:Directory"] ?? "/backups";
    }

    public BackupStatusDto Read()
    {
        try
        {
            if (!Directory.Exists(_directory))
            {
                return new BackupStatusDto();
            }

            var items = new List<BackupItemDto>();

            foreach (var dump in Directory.EnumerateFiles(_directory, "*.dump"))
            {
                var stem = Path.GetFileNameWithoutExtension(dump);
                var taken = TakenAt(stem) ?? File.GetLastWriteTimeUtc(dump);
                var archive = Path.Combine(_directory, stem + "-files.tar.gz");
                var hasArchive = File.Exists(archive);

                items.Add(new BackupItemDto
                {
                    Name = Path.GetFileName(dump),
                    TakenAtUtc = taken,
                    SizeBytes = new FileInfo(dump).Length + (hasArchive ? new FileInfo(archive).Length : 0),
                    HasDocuments = hasArchive,
                    Offsite = File.Exists(dump + ".offsite"),
                });
            }

            var check = Path.Combine(_directory, "restore-check.ok");

            return new BackupStatusDto
            {
                Known = true,
                Items = items.OrderByDescending(i => i.TakenAtUtc).Take(10).ToList(),
                RestoreCheckedAtUtc = File.Exists(check) ? File.GetLastWriteTimeUtc(check) : null,
            };
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return new BackupStatusDto();
        }
    }

    /// <summary>The time in the name, which survives a copy of the file that resets its modified time.</summary>
    private static DateTime? TakenAt(string stem)
    {
        var at = stem.LastIndexOf('-');

        return at >= 0 && DateTime.TryParseExact(stem[(at + 1)..], Stamp, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsed)
            ? parsed
            : null;
    }
}
