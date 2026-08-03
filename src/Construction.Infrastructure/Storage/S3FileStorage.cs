using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Construction.Application.Common.Interfaces;
using Microsoft.Extensions.Options;

namespace Construction.Infrastructure.Storage;

/// <summary>
/// Stores files in an S3-compatible bucket.
/// </summary>
/// <remarks>
/// Written against the S3 API rather than AWS specifically, so it also serves
/// MinIO, Backblaze B2 and the European providers a construction firm in the
/// region is more likely to use. The choice is a configuration value, not a
/// code change.
/// </remarks>
public class S3FileStorage : IFileStorage
{
    private readonly IAmazonS3 _client;
    private readonly string _bucket;

    public S3FileStorage(IOptions<FileStorageSettings> settings)
    {
        var options = settings.Value;

        _bucket = options.Bucket
            ?? throw new InvalidOperationException(
                "FileStorage:Bucket is required when object storage is selected.");

        var config = new AmazonS3Config
        {
            ForcePathStyle = options.ForcePathStyle
        };

        if (!string.IsNullOrWhiteSpace(options.ServiceUrl))
        {
            config.ServiceURL = options.ServiceUrl;
        }
        else if (!string.IsNullOrWhiteSpace(options.Region))
        {
            config.RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(options.Region);
        }

        // Explicit keys where they are configured, otherwise the ambient chain
        // — environment, instance role, mounted credentials. A deployment that
        // can use a role should not have to put a secret in configuration.
        _client = string.IsNullOrWhiteSpace(options.AccessKey)
            ? new AmazonS3Client(config)
            : new AmazonS3Client(
                new BasicAWSCredentials(options.AccessKey, options.SecretKey), config);
    }

    public async Task SaveAsync(
        string storageKey,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        await _client.PutObjectAsync(
            new PutObjectRequest
            {
                BucketName = _bucket,
                Key = storageKey,
                InputStream = content,
                ContentType = contentType,
                // The API checks authorization on every read; a bucket that
                // also answers anonymously would make that check decorative.
                DisablePayloadSigning = false
            },
            cancellationToken);
    }

    public async Task<Stream?> OpenReadAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _client.GetObjectAsync(
                _bucket, storageKey, cancellationToken);

            return response.ResponseStream;
        }
        catch (AmazonS3Exception exception)
            when (exception.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task DeleteAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        // S3 treats deleting an absent key as success, which is what a retried
        // delete needs.
        await _client.DeleteObjectAsync(_bucket, storageKey, cancellationToken);
    }
}
