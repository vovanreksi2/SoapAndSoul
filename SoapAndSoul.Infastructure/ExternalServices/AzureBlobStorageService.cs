using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SoupAndSoupApp.ExternalServices;

public class AzureBlobStorageService : IAzureBlobStorageService
{
    private readonly BlobContainerClient _containerClient;
    private readonly ILogger<AzureBlobStorageService> _logger;

    private const string ContainerName = "photo";

    public AzureBlobStorageService(IOptions<AzureBlobStorageSettings> options, ILogger<AzureBlobStorageService> logger)
    {
        _logger = logger;
        var sasUri = options.Value.SasUri;
        _containerClient = new BlobContainerClient(new Uri(sasUri));
    }

    public async Task<bool> UploadBlobAsync(string blobName, Stream content,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var blobClient = _containerClient.GetBlobClient(blobName);
            await blobClient.UploadAsync(content, true, cancellationToken);
            _logger.LogInformation("Uploaded blob '{BlobName}' to container '{ContainerName}'", blobName, ContainerName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload blob '{BlobName}'", blobName);
            return false;
        }
    }

    public async Task<Stream?> DownloadBlobAsync(string blobName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var blobClient = _containerClient.GetBlobClient(blobName);
            var response = await blobClient.DownloadAsync(cancellationToken);
            _logger.LogInformation("Downloaded blob '{BlobName}' from container '{ContainerName}'", blobName, ContainerName);
            return response.Value.Content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download blob '{BlobName}'", blobName);
            return null;
        }
    }

    public async Task<bool> DeleteBlobAsync(string blobName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var blobClient = _containerClient.GetBlobClient(blobName);
            var result = await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
            _logger.LogInformation("Deleted blob '{BlobName}' from container '{ContainerName}': {Result}", blobName, ContainerName, result.Value);
            return result.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete blob '{BlobName}'", blobName);
            return false;
        }
    }

    public async Task<bool> BlobExistsAsync(string blobName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var blobClient = _containerClient.GetBlobClient(blobName);
            var exists = await blobClient.ExistsAsync(cancellationToken);
            _logger.LogInformation("Blob '{BlobName}' exists in container '{ContainerName}': {Exists}", blobName, ContainerName, exists.Value);
            return exists.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check existence of blob '{BlobName}'", blobName);
            return false;
        }
    }
}