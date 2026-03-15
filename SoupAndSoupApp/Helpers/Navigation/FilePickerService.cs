using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;

namespace SoupAndSoupApp.Helpers.Navigation;

public class FilePickerService : IFilePickerService
{
    private static readonly FilePickerOpenOptions PickerOptions = new()
    {
        Title = "Select an Image",
        FileTypeFilter = new List<FilePickerFileType>
        {
            new("Images") { Patterns = ["*.png", "*.jpg", "*.jpeg"] }
        }
    };

    public async Task<string?> PickImageAsync()
    {
        var storageProvider = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)
            ?.MainWindow?.StorageProvider;

        if (storageProvider is null) return null;

        var result = await storageProvider.OpenFilePickerAsync(PickerOptions);
        return result.FirstOrDefault()?.Path.LocalPath;
    }
}
