namespace Construction.Infrastructure.Storage;

public class FileStorageSettings
{
    public const string SectionName = "FileStorage";

    /// <summary>
    /// Directory used by the filesystem implementation. Ignored once
    /// <see cref="Bucket"/> is set.
    /// </summary>
    public string RootPath { get; set; } = "storage";

    /// <summary>Object-storage bucket. Setting it selects S3 over the disk.</summary>
    public string? Bucket { get; set; }

    /// <summary>
    /// Endpoint of an S3-compatible service (MinIO, Backblaze, Hetzner).
    /// Left empty for AWS itself, where the region determines the endpoint.
    /// </summary>
    public string? ServiceUrl { get; set; }

    public string? Region { get; set; }

    public string? AccessKey { get; set; }

    public string? SecretKey { get; set; }

    /// <summary>
    /// Required by MinIO and most non-AWS services, which address buckets as a
    /// path rather than as a subdomain.
    /// </summary>
    public bool ForcePathStyle { get; set; } = true;

    public bool UsesObjectStorage => !string.IsNullOrWhiteSpace(Bucket);
}
