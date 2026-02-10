namespace Cadence.Core.Features.Photos.Services;

/// <summary>
/// Abstraction for photo blob storage operations.
/// Provides upload, delete, and read access to blob storage without Azure-specific dependencies.
/// </summary>
public interface IBlobStorageService
{
    /// <summary>
    /// Uploads a photo to blob storage.
    /// </summary>
    /// <param name="stream">The photo file stream</param>
    /// <param name="blobPath">Full blob path (e.g., "{orgId}/{exerciseId}/photos/{photoId}.jpg")</param>
    /// <param name="contentType">MIME content type (e.g., "image/jpeg")</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The blob URI</returns>
    Task<string> UploadAsync(Stream stream, string blobPath, string contentType, CancellationToken ct = default);

    /// <summary>
    /// Deletes a blob by its URI.
    /// </summary>
    /// <param name="blobUri">The full blob URI to delete</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeleteAsync(string blobUri, CancellationToken ct = default);

    /// <summary>
    /// Gets a read-access URI for a blob (SAS token for production, direct URI for Azurite).
    /// </summary>
    /// <param name="blobUri">The stored blob URI</param>
    /// <param name="expiresIn">How long the read URI should be valid</param>
    /// <returns>A URI with read access</returns>
    string GetReadUri(string blobUri, TimeSpan expiresIn);
}
