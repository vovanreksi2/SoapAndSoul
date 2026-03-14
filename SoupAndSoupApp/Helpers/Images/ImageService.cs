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

public class ImageService : IImageService
{
    private static readonly string[] ValidExtensions = [".jpg", ".jpeg", ".png"];

    private readonly IAzureBlobStorageService _blobStorageService;
    private readonly ILogger<ImageService> _logger;

    public ImageService(IAzureBlobStorageService blobStorageService, ILogger<ImageService> logger)
    {
        _blobStorageService = blobStorageService;
        _logger = logger;
    }

    public Task<string?> UploadImageAsync(string localImagePath)
    {
        if (string.IsNullOrEmpty(localImagePath) || !File.Exists(localImagePath))
        {
            _logger.LogWarning("File not found on local machine. FilePath: {LocalFilePath}", localImagePath);
            return Task.FromResult<string?>(null);
        }

        var ext = Path.GetExtension(localImagePath);
        if (!IsValidExtension(ext))
            throw new InvalidOperationException("Unsupported image extension.");

        var photoName = $"{Guid.NewGuid()}{ext}";
        return UploadImageToBlobAsync(photoName, localImagePath);
    }

    public async Task<Bitmap?> DownloadImageAsync(string blobName)
    {
        if (string.IsNullOrEmpty(blobName)) return null;

        var stream = await _blobStorageService.DownloadBlobAsync(blobName);
        return stream == null ? null : GetBitmapFromStream(stream);
    }

    public async Task DeleteImageAsync(BaseModel model)
    {
        if (string.IsNullOrEmpty(model.ImagePathString))
        {
            _logger.LogInformation("Entity with ID {EntityId} has no image to delete. Name: {EntityName}", model.Id, model.Name);
            return;
        }

        var deleteResult = await _blobStorageService.DeleteBlobAsync(model.ImagePathString);
        if (deleteResult)
            _logger.LogInformation("Image deleted successfully for entity with ID {EntityId}, Name: {EntityName}", model.Id, model.Name);
        else
            _logger.LogError("Failed to delete image for entity with ID {EntityId}, Name: {EntityName}", model.Id, model.Name);
    }

    public async Task UpdateComponentImageAsync(NewComponentDto dto)
    {
        if (!dto.IsPhotoChanged) return;

        var uploadResult = await UploadImageAsync(dto.ImagePath);
        if (uploadResult is null)
            _logger.LogError("Failed to upload component image by {ImagePath} for component {ComponentName}",
                dto.ImagePath, dto.Name);
        else
            dto.ImagePath = uploadResult;
    }

    public async Task UpdateRecipeImageAsync(RecipeModel recipe, string newImagePath)
    {
        var uploadResult = await UploadImageAsync(newImagePath);
        if (uploadResult is null)
            _logger.LogError("Failed to upload recipe image by {ImagePath} for recipe {RecipeName}",
                newImagePath, recipe.Name);
        else
            recipe.ImagePathString = uploadResult;
    }

    public async Task<Bitmap> LoadImageOrDefaultAsync(string? imageUrl, int entityId, string defaultImageUrl)
    {
        if (string.IsNullOrEmpty(imageUrl))
        {
            _logger.LogWarning("Image URL is null or empty for entity ID {EntityId}. Using default image.", entityId);
            return ImageHelper.LoadFromResource(defaultImageUrl);
        }

        var imageBitmap = await DownloadImageAsync(imageUrl);
        if (imageBitmap is not null) return imageBitmap;

        _logger.LogWarning("Could not find or download image for entity ID {EntityId}. Using default image.", entityId);
        return ImageHelper.LoadFromResource(defaultImageUrl);
    }

    public async Task<Bitmap> LoadImageOrDefaultAsync(Component component, string defaultImageUrl)
    {
        var imageUrl = component.Images.FirstOrDefault()?.ImageUrl;
        return await LoadImageOrDefaultAsync(imageUrl, component.Id, defaultImageUrl);
    }

    public Bitmap GetBitmapFromStream(Stream stream)
    {
        if (stream == null || !stream.CanRead)
            throw new ArgumentException("Stream is not valid or readable.");

        if (stream.CanSeek)
            stream.Position = 0;

        var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        memoryStream.Position = 0;

        return new Bitmap(memoryStream);
    }

    private async Task<string?> UploadImageToBlobAsync(string blobName, string localImagePath)
    {
        if (string.IsNullOrEmpty(localImagePath) || !File.Exists(localImagePath))
        {
            _logger.LogWarning("File not found on local machine. FilePath: {LocalFilePath}", localImagePath);
            return null;
        }

        await using var stream = File.OpenRead(localImagePath);
        var success = await _blobStorageService.UploadBlobAsync(blobName, stream);

        return success ? blobName : null;
    }

    private static bool IsValidExtension(string ext) =>
        ValidExtensions.Contains(ext.ToLower());
}
