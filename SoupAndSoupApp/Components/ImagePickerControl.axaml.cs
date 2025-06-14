using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using ReactiveUI;

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

    public ImagePickerControl()
    {
        InitializeComponent();

        var button = this.FindControl<Button>("MainButton");
        if (button != null)
            button.Click += async (_, e) => await Button_OnClick(e);

    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private async Task Button_OnClick(RoutedEventArgs e)
    {
        var ownerWindow = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        if (ownerWindow == null)
            return;

        var storageProvider = ownerWindow.StorageProvider;
        var options = new FilePickerOpenOptions
        {
            Title = "Select an Image",
            FileTypeFilter = new List<FilePickerFileType>
            {
                new ("Images")
                {
                    Patterns = new[] { "*.png", "*.jpg", "*.jpeg" }
                }
            }
        };

        var result = await storageProvider.OpenFilePickerAsync(options);
        if (result.FirstOrDefault() is { } file)
        {
            NewImagePath = file.Path.LocalPath;
        }
    }

}
