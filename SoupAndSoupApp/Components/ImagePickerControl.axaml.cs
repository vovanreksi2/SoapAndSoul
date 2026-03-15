using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Microsoft.Extensions.DependencyInjection;
using SoupAndSoupApp.Helpers.Navigation;

namespace SoupAndSoupApp;

public partial class ImagePickerControl : UserControl
{
    public static readonly StyledProperty<IImage> ImageSourceProperty =
        AvaloniaProperty.Register<ImagePickerControl, IImage>(nameof(ImageSource));
    public IImage ImageSource
    {
        get => GetValue(ImageSourceProperty);
        set => SetValue(ImageSourceProperty, value);
    }

    public static readonly StyledProperty<string> NewImagePathProperty =
        AvaloniaProperty.Register<ImagePickerControl, string>(nameof(NewImagePath));
    public string NewImagePath
    {
        get => GetValue(NewImagePathProperty);
        set => SetValue(NewImagePathProperty, value);
    }

    private readonly IFilePickerService _filePickerService;

    public ImagePickerControl()
    {
        InitializeComponent();

        _filePickerService = App.Services?.GetRequiredService<IFilePickerService>()
            ?? new FilePickerService();

        var button = this.FindControl<Button>("MainButton");
        if (button is not null)
            button.Click += async (_, _) => await OnPickImageAsync();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private async Task OnPickImageAsync()
    {
        var path = await _filePickerService.PickImageAsync();
        if (path is not null)
            NewImagePath = path;
    }
}
