using Construction.Application.Common.Interfaces;
using Microsoft.Extensions.Options;

namespace Construction.Infrastructure.Storage;

/// <summary>
/// Free space of the disk the file storage root is on. In the container that is the host's disk, because the storage is
/// a volume on it, and the database's volume sits on the same disk, so one number covers both.
/// </summary>
public sealed class DriveInfoDiskSpace : IDiskSpace
{
    private readonly string _root;

    public DriveInfoDiskSpace(IOptions<FileStorageSettings> settings)
    {
        _root = settings.Value.RootPath;
    }

    public DiskSpaceReading? Read()
    {
        try
        {
            var path = Path.GetFullPath(_root);

            // The folder may not exist yet; the disk it will be on is the one its nearest existing parent is on.
            while (!Directory.Exists(path) && Directory.GetParent(path) is { } parent)
            {
                path = parent.FullName;
            }

            var drive = new DriveInfo(OperatingSystem.IsWindows() ? Path.GetPathRoot(path)! : path);

            return new DiskSpaceReading(drive.TotalSize, drive.AvailableFreeSpace);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return null;
        }
    }
}
