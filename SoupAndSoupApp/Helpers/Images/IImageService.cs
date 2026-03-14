using System.IO;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using SoupAndSoupApp.Models;
using SoupAndSoup.Data.Models;

namespace SoupAndSoupApp.Helpers.Images;

public interface IImageService
{
    Task<string?> UploadImageAsync(string localImagePath);
    Task<Bitmap?> DownloadImageAsync(string blobName);
    Task DeleteImageAsync(BaseModel model);
    Task UpdateComponentImageAsync(NewComponentDto dto);
    Task UpdateRecipeImageAsync(RecipeModel recipe, string newImagePath);
    Task<Bitmap> LoadImageOrDefaultAsync(string? imageUrl, int entityId, string defaultImageUrl);
    Task<Bitmap> LoadImageOrDefaultAsync(Component component, string defaultImageUrl);
    Bitmap GetBitmapFromStream(Stream stream);
}
