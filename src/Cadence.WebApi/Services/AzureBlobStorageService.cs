using System.Diagnostics;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Cadence.Core.Features.Photos.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Cadence.WebApi.Services;

/// <summary>
/// Production blob storage service using Azure Blob Storage.
/// Supports both Azurite (local development) and production Azure Storage.
/// Includes structured logging for troubleshooting.
/// </summary>
public class AzureBlobStorageService : IBlobStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly string _containerName;
    private readonly bool _isDevelopment;
    private readonly ILogger<AzureBlobStorageService> _logger;
    private BlobContainerClient? _containerClient;

    public AzureBlobStorageService(
        BlobServiceClient blobServiceClient,
        IConfiguration configuration,
        ILogger<AzureBlobStorageService> logger)
    {
        _blobServiceClient = blobServiceClient ?? throw new ArgumentNullException(nameof(blobServiceClient));
        _containerName = configuration["Azure:BlobStorage:PhotoContainerName"] ?? "exercise-photos";

        var connectionString = configuration["Azure:BlobStorage:ConnectionString"] ?? "";
        _isDevelopment = connectionString.Contains("UseDevelopmentStorage=true", StringComparison.OrdinalIgnoreCase);

        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogInformation(
            "[BlobStorage] Initialized - Container: {ContainerName}, Mode: {Mode}",
            _containerName,
            _isDevelopment ? "Development (Azurite)" : "Production (Azure)");
    }

    /// <summary>
    /// Gets or creates the blob container client.
    /// Container is created on first access if it doesn't exist.
    /// </summary>
    private async Task<BlobContainerClient> GetContainerClientAsync(CancellationToken ct = default)
    {
        if (_containerClient != null)
            return _containerClient;

        _containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);

        // Create container if it doesn't exist (PublicAccessType.None = private by default)
        await _containerClient.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: ct);

        _logger.LogDebug(
            "[BlobStorage] Container '{ContainerName}' ready",
            _containerName);

        return _containerClient;
    }

    /// <inheritdoc />
    public async Task<string> UploadAsync(Stream stream, string blobPath, string contentType, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (string.IsNullOrWhiteSpace(blobPath))
            throw new ArgumentException("Blob path cannot be empty", nameof(blobPath));
        if (string.IsNullOrWhiteSpace(contentType))
            throw new ArgumentException("Content type cannot be empty", nameof(contentType));

        var sw = Stopwatch.StartNew();
        var containerClient = await GetContainerClientAsync(ct);
        var blobClient = containerClient.GetBlobClient(blobPath);

        try
        {
            // Ensure stream is at the beginning
            if (stream.CanSeek)
                stream.Position = 0;

            var options = new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = contentType
                }
            };

            _logger.LogDebug(
                "[BlobStorage] Uploading - Path: {BlobPath}, ContentType: {ContentType}, StreamLength: {StreamLength}",
                blobPath,
                contentType,
                stream.Length);

            await blobClient.UploadAsync(stream, options, ct);
            sw.Stop();

            var blobUri = blobClient.Uri.ToString();

            _logger.LogInformation(
                "[BlobStorage] Upload complete - Path: {BlobPath}, URI: {BlobUri}, ElapsedMs: {ElapsedMs}",
                blobPath,
                blobUri,
                sw.ElapsedMilliseconds);

            return blobUri;
        }
        catch (RequestFailedException ex)
        {
            sw.Stop();
            _logger.LogError(ex,
                "[BlobStorage] Upload FAILED - Path: {BlobPath}, ErrorCode: {ErrorCode}, " +
                "HttpStatus: {HttpStatus}, ElapsedMs: {ElapsedMs}",
                blobPath,
                ex.ErrorCode,
                ex.Status,
                sw.ElapsedMilliseconds);
            throw;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex,
                "[BlobStorage] Upload error - Path: {BlobPath}, ExceptionType: {ExceptionType}, " +
                "ElapsedMs: {ElapsedMs}",
                blobPath,
                ex.GetType().Name,
                sw.ElapsedMilliseconds);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(string blobUri, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(blobUri))
            throw new ArgumentException("Blob URI cannot be empty", nameof(blobUri));

        var sw = Stopwatch.StartNew();

        try
        {
            // Extract blob name from URI
            // URI format: https://{account}.blob.core.windows.net/{container}/{blobPath}
            // or for Azurite: http://127.0.0.1:10000/devstoreaccount1/{container}/{blobPath}
            var uri = new Uri(blobUri);
            var segments = uri.Segments;

            // Skip the first segment (/) and container name, join the rest as blob path
            var blobPath = string.Join("", segments.Skip(2)).TrimEnd('/');

            var containerClient = await GetContainerClientAsync(ct);
            var blobClient = containerClient.GetBlobClient(blobPath);

            _logger.LogDebug(
                "[BlobStorage] Deleting - URI: {BlobUri}, Path: {BlobPath}",
                blobUri,
                blobPath);

            var response = await blobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, cancellationToken: ct);
            sw.Stop();

            var deleted = response.Value;

            if (deleted)
            {
                _logger.LogInformation(
                    "[BlobStorage] Delete complete - Path: {BlobPath}, ElapsedMs: {ElapsedMs}",
                    blobPath,
                    sw.ElapsedMilliseconds);
            }
            else
            {
                _logger.LogWarning(
                    "[BlobStorage] Delete - blob not found - Path: {BlobPath}, ElapsedMs: {ElapsedMs}",
                    blobPath,
                    sw.ElapsedMilliseconds);
            }

            return deleted;
        }
        catch (RequestFailedException ex)
        {
            sw.Stop();
            _logger.LogError(ex,
                "[BlobStorage] Delete FAILED - URI: {BlobUri}, ErrorCode: {ErrorCode}, " +
                "HttpStatus: {HttpStatus}, ElapsedMs: {ElapsedMs}",
                blobUri,
                ex.ErrorCode,
                ex.Status,
                sw.ElapsedMilliseconds);
            throw;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex,
                "[BlobStorage] Delete error - URI: {BlobUri}, ExceptionType: {ExceptionType}, " +
                "ElapsedMs: {ElapsedMs}",
                blobUri,
                ex.GetType().Name,
                sw.ElapsedMilliseconds);
            throw;
        }
    }

    /// <inheritdoc />
    public string GetReadUri(string blobUri, TimeSpan expiresIn)
    {
        if (string.IsNullOrWhiteSpace(blobUri))
            throw new ArgumentException("Blob URI cannot be empty", nameof(blobUri));

        // For Azurite (local development), return direct URI without SAS token
        // Azurite doesn't require authentication for blob access by default
        if (_isDevelopment)
        {
            _logger.LogDebug(
                "[BlobStorage] Read URI (development) - URI: {BlobUri}",
                blobUri);
            return blobUri;
        }

        // For production, generate a SAS token for read access
        try
        {
            var uri = new Uri(blobUri);
            var segments = uri.Segments;
            var blobPath = string.Join("", segments.Skip(2)).TrimEnd('/');

            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobClient(blobPath);

            // Check if we can generate SAS tokens (requires account key, not SAS connection string)
            if (!blobClient.CanGenerateSasUri)
            {
                _logger.LogWarning(
                    "[BlobStorage] Cannot generate SAS URI - using direct URI. " +
                    "This may fail if blob is not public. Path: {BlobPath}",
                    blobPath);
                return blobUri;
            }

            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = _containerName,
                BlobName = blobPath,
                Resource = "b", // "b" for blob
                StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5), // Allow 5 min clock skew
                ExpiresOn = DateTimeOffset.UtcNow.Add(expiresIn)
            };

            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            var sasUri = blobClient.GenerateSasUri(sasBuilder);

            _logger.LogDebug(
                "[BlobStorage] Read URI with SAS - Path: {BlobPath}, ExpiresIn: {ExpiresIn}",
                blobPath,
                expiresIn);

            return sasUri.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[BlobStorage] Error generating SAS URI - URI: {BlobUri}, ExceptionType: {ExceptionType}",
                blobUri,
                ex.GetType().Name);

            // Fallback to direct URI (may fail if not public)
            return blobUri;
        }
    }
}
