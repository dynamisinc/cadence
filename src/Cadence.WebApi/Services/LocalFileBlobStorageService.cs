using Cadence.Core.Features.Photos.Services;

namespace Cadence.WebApi.Services;

/// <summary>
/// Local file system blob storage for development.
/// Saves files to wwwroot/uploads/ so they're served by Kestrel's static files middleware.
/// Eliminates the need for Azurite during local development.
/// </summary>
public class LocalFileBlobStorageService : IBlobStorageService
{
    private readonly string _basePath;
    private readonly string _baseUrl;
    private readonly ILogger<LocalFileBlobStorageService> _logger;

    public LocalFileBlobStorageService(
        IWebHostEnvironment env,
        IConfiguration configuration,
        ILogger<LocalFileBlobStorageService> logger)
    {
        _basePath = Path.Combine(env.ContentRootPath, "wwwroot", "uploads");
        Directory.CreateDirectory(_basePath);

        // Use the app's base URL for serving files
        var urls = configuration["Urls"] ?? "http://localhost:5071";
        _baseUrl = urls.Split(';').First().TrimEnd('/');

        _logger = logger;
        _logger.LogInformation("[BlobStorage] Using local file storage at {BasePath}", _basePath);
    }

    /// <inheritdoc />
    public async Task<string> UploadAsync(Stream stream, string blobPath, string contentType, CancellationToken ct = default)
    {
        var filePath = Path.Combine(_basePath, blobPath.Replace('/', Path.DirectorySeparatorChar));
        var directory = Path.GetDirectoryName(filePath)!;
        Directory.CreateDirectory(directory);

        if (stream.CanSeek)
            stream.Position = 0;

        await using var fileStream = File.Create(filePath);
        await stream.CopyToAsync(fileStream, ct);

        var uri = $"{_baseUrl}/uploads/{blobPath}";

        _logger.LogDebug("[BlobStorage] Local upload - Path: {FilePath}, URI: {Uri}", filePath, uri);

        return uri;
    }

    /// <inheritdoc />
    public Task<bool> DeleteAsync(string blobUri, CancellationToken ct = default)
    {
        // Extract relative path from URI
        var uploadsIndex = blobUri.IndexOf("/uploads/", StringComparison.OrdinalIgnoreCase);
        if (uploadsIndex < 0)
            return Task.FromResult(false);

        var relativePath = blobUri[(uploadsIndex + "/uploads/".Length)..];
        var filePath = Path.Combine(_basePath, relativePath.Replace('/', Path.DirectorySeparatorChar));

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            _logger.LogDebug("[BlobStorage] Local delete - Path: {FilePath}", filePath);
            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }

    /// <inheritdoc />
    public string GetReadUri(string blobUri, TimeSpan expiresIn)
    {
        // Local files are served directly - no SAS needed
        return blobUri;
    }
}
