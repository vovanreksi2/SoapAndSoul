using Avalonia.Media.Imaging;
using ReactiveUI;

namespace SoupAndSoupApp.Models;

public class BaseModel : ReactiveObject
{
    public int Id { get; set; }
    public bool IsNew => Id == default;

    public string Name
    {
        get => _name;
        set => this.RaiseAndSetIfChanged(ref _name, value);
    }

    public string? ImagePathString { get; set; }
    public Bitmap ImagePath
    {
        get => _imagePath;
        set => this.RaiseAndSetIfChanged(ref _imagePath, value);
    }


    private Bitmap _imagePath;
    private string _name;
}