using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Microsoft.Extensions.Logging;
using SoupAndSoup.Data.Models;
using SoupAndSoupApp.ExternalServices;
using SoupAndSoupApp.Models;

namespace SoupAndSoupApp.Helpers.Images;

public class ImageService(IAzureBlobStorageService blobStorageService, ILogger<ImageService> logger) : IImageService
{
    private static readonly string[] ValidExtensions = [".jpg", ".jpeg", ".png"];
    private static readonly byte[] PngMagicBytes = [0x89, 0x50, 0x4E, 0x47];
    private static readonly byte[] JpegMagicBytes = [0xFF, 0xD8, 0xFF];
    private const long MaxImageSizeBytes = 10 * 1024 * 1024; // 10 MB

    public Task<string?> UploadImageAsync(string localImagePath)
    {
        if (string.IsNullOrEmpty(localImagePath) || !File.Exists(localImagePath))
        {
            logger.LogWarning("File not found on local machine. FilePath: {LocalFilePath}", localImagePath);
            return Task.FromResult<string?>(null);
        }

        var fileInfo = new FileInfo(localImagePath);
        if (fileInfo.Length > MaxImageSizeBytes)
            throw new InvalidOperationException($"Image file exceeds the maximum allowed size of {MaxImageSizeBytes / 1024 / 1024} MB.");

        var ext = Path.GetExtension(localImagePath);
        if (!IsValidExtension(ext))
            throw new InvalidOperationException("Unsupported image extension.");

        if (!HasValidMagicBytes(localImagePath))
            throw new InvalidOperationException("Image file content does not match a supported format (JPEG or PNG).");

        var photoName = $"{Guid.NewGuid()}{ext}";
        return UploadImageToBlobAsync(photoName, localImagePath);
    }

    public async Task<Bitmap?> DownloadImageAsync(string blobName)
    {
        if (string.IsNullOrEmpty(blobName)) return null;

        var stream = await blobStorageService.DownloadBlobAsync(blobName);
        return stream is null ? null : GetBitmapFromStream(stream);
    }

    public async Task DeleteImageAsync(BaseModel model)
    {
        if (string.IsNullOrEmpty(model.ImagePathString))
        {
            logger.LogInformation("Entity with ID {EntityId} has no image to delete. Name: {EntityName}", model.Id, model.Name);
            return;
        }

        var deleteResult = await blobStorageService.DeleteBlobAsync(model.ImagePathString);
        if (deleteResult)
            logger.LogInformation("Image deleted successfully for entity with ID {EntityId}, Name: {EntityName}", model.Id, model.Name);
        else
            logger.LogError("Failed to delete image for entity with ID {EntityId}, Name: {EntityName}", model.Id, model.Name);
    }

    public async Task UpdateComponentImageAsync(NewComponentDto dto)
    {
        if (!dto.IsPhotoChanged) return;

        var uploadResult = await UploadImageAsync(dto.ImagePath);
        if (uploadResult is null)
            logger.LogError("Failed to upload component image by {ImagePath} for component {ComponentName}",
                dto.ImagePath, dto.Name);
        else
            dto.ImagePath = uploadResult;
    }

    public async Task UpdateRecipeImageAsync(RecipeModel recipe, string newImagePath)
    {
        var uploadResult = await UploadImageAsync(newImagePath);
        if (uploadResult is null)
            logger.LogError("Failed to upload recipe image by {ImagePath} for recipe {RecipeName}",
                newImagePath, recipe.Name);
        else
            recipe.ImagePathString = uploadResult;
    }

    public async Task<Bitmap> LoadImageOrDefaultAsync(string? imageUrl, int entityId, string defaultImageUrl)
    {
        if (string.IsNullOrEmpty(imageUrl))
        {
            logger.LogWarning("Image URL is null or empty for entity ID {EntityId}. Using default image.", entityId);
            return ImageHelper.LoadFromResource(defaultImageUrl);
        }

        var imageBitmap = await DownloadImageAsync(imageUrl);
        if (imageBitmap is not null) return imageBitmap;

        logger.LogWarning("Could not find or download image for entity ID {EntityId}. Using default image.", entityId);
        return ImageHelper.LoadFromResource(defaultImageUrl);
    }

    public async Task<Bitmap> LoadImageOrDefaultAsync(Component component, string defaultImageUrl)
    {
        var imageUrl = component.Images.FirstOrDefault()?.ImageUrl;
        return await LoadImageOrDefaultAsync(imageUrl, component.Id, defaultImageUrl);
    }

    public Bitmap GetBitmapFromStream(Stream stream)
    {
        if (stream is null || !stream.CanRead)
            throw new ArgumentException("Stream is not valid or readable.");

        if (stream.CanSeek)
            stream.Position = 0;

        using var memoryStream = new MemoryStream();
        using (stream)
            stream.CopyTo(memoryStream);
        memoryStream.Position = 0;

        return new Bitmap(memoryStream);
    }

    private async Task<string?> UploadImageToBlobAsync(string blobName, string localImagePath)
    {
        if (string.IsNullOrEmpty(localImagePath) || !File.Exists(localImagePath))
        {
            logger.LogWarning("File not found on local machine. FilePath: {LocalFilePath}", localImagePath);
            return null;
        }

        await using var stream = File.OpenRead(localImagePath);
        var success = await blobStorageService.UploadBlobAsync(blobName, stream);

        return success ? blobName : null;
    }

    private static bool IsValidExtension(string ext) =>
        ValidExtensions.Contains(ext.ToLower());

    private static bool HasValidMagicBytes(string filePath)
    {
        using var fs = File.OpenRead(filePath);
        Span<byte> buffer = stackalloc byte[4];
        var read = fs.Read(buffer);
        if (read < 3) return false;

        if (buffer[0] == JpegMagicBytes[0] && buffer[1] == JpegMagicBytes[1] && buffer[2] == JpegMagicBytes[2])
            return true;

        if (read >= 4 && buffer[0] == PngMagicBytes[0] && buffer[1] == PngMagicBytes[1]
                      && buffer[2] == PngMagicBytes[2] && buffer[3] == PngMagicBytes[3])
            return true;

        return false;
    }
}
