using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SoupAndSoupApp.ExternalServices;

public interface IAzureBlobStorageService
{
    Task<bool> UploadBlobAsync(string blobName, Stream content, CancellationToken cancellationToken = default);
    Task<Stream?> DownloadBlobAsync(string blobName, CancellationToken cancellationToken = default);
    Task<bool> DeleteBlobAsync(string blobName, CancellationToken cancellationToken = default);
    Task<bool> BlobExistsAsync(string blobName, CancellationToken cancellationToken = default);
}