using System.Reactive;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

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

    public ImagePickerControl()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}