using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;

namespace SoupAndSoupApp.ExternalServices;

public class AzureBlobStorageService : IAzureBlobStorageService
{
    private readonly BlobContainerClient _containerClient;
    private readonly string _sasUri;

    private const string ContainerName = "photo";

    public AzureBlobStorageService()
    {
        _sasUri = "https://soapandsoulphotosacc.blob.core.windows.net/photo?sp=racwdl&st=2025-07-08T08:34:34Z&se=2026-07-08T16:34:34Z&spr=https&sv=2024-11-04&sr=c&sig=3ro5VMfmglkw64d3o%2BL7Oo7eE6Swfah9vzDEGbLOehE%3D";
        _containerClient = GetContainerClient("photo");
    }

    public async Task<bool> UploadBlobAsync(string blobName, Stream content,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var blobClient = _containerClient.GetBlobClient(blobName);
            await blobClient.UploadAsync(content, true, cancellationToken);
            Debug.WriteLine($"[AzureBlobStorageService] Uploaded blob '{blobName}' to container '{ContainerName}'.");
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AzureBlobStorageService] Failed to upload blob '{blobName}': {ex.Message}");
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
            Debug.WriteLine($"[AzureBlobStorageService] Downloaded blob '{blobName}' from container '{ContainerName}'.");
            return response.Value.Content;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AzureBlobStorageService] Failed to download blob '{blobName}': {ex.Message}");
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
            Debug.WriteLine($"[AzureBlobStorageService] Deleted blob '{blobName}' from container '{ContainerName}': {result.Value}");
            return result.Value;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AzureBlobStorageService] Failed to delete blob '{blobName}': {ex.Message}");
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
            Debug.WriteLine($"[AzureBlobStorageService] Blob '{blobName}' exists in container '{ContainerName}': {exists.Value}");
            return exists.Value;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AzureBlobStorageService] Failed to check existence of blob '{blobName}': {ex.Message}");
            return false;
        }
    }

    private BlobContainerClient GetContainerClient(string containerName)
    {
        var serviceClient = new BlobServiceClient(new Uri(_sasUri));
        return serviceClient.GetBlobContainerClient(containerName);
    }
}