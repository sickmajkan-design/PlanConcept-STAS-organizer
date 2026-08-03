namespace Construction.Application.Common.Interfaces;

/// <summary>Where an uploaded file's bytes live.</summary>
/// <remarks>
/// Deliberately narrow. Everything the product needs of storage is "put these
/// bytes somewhere and give them back later", and keeping the interface at
/// that size is what lets the same code run against object storage in
/// deployment and the local disk in development and tests.
///
/// The API streams both directions rather than handing out pre-signed URLs.
/// A signed URL is faster and offloads bandwidth, but it also escapes the
/// authorization that produced it — anyone holding the link has the file until
/// it expires. Streaming keeps one answer to "may this caller see this file",
/// and the files here are documents and site photos rather than video.
/// Pre-signing stays available later as an optimisation behind this same
/// interface.
/// </remarks>
public interface IFileStorage
{
    /// <summary>
    /// Stores <paramref name="content"/> under <paramref name="storageKey"/>,
    /// replacing anything already there.
    /// </summary>
    Task SaveAsync(
        string storageKey,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens the stored bytes for reading, or null when the key is not there.
    /// </summary>
    /// <remarks>
    /// Null rather than an exception: a row whose bytes have gone missing is a
    /// real state after a restore from an out-of-step backup, and the caller
    /// should be able to report that rather than fail as if the request were
    /// malformed.
    /// </remarks>
    Task<Stream?> OpenReadAsync(
        string storageKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes the stored bytes. Succeeds when the key is already gone, so a
    /// retried delete is not an error.
    /// </summary>
    Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default);
}
