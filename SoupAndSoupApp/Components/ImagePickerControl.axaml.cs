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
    public static readonly StyledProperty<bool> IsHoveredProperty =
        AvaloniaProperty.Register<ImagePickerControl, bool>(nameof(IsHovered));
    public bool IsHovered
    {
        get => GetValue(IsHoveredProperty);
        private set => SetValue(IsHoveredProperty, value);
    }

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

        var myButton = this.FindControl<Button>("MainButton");

        Observable.FromEventPattern<PointerEventArgs>(myButton, nameof(PointerEntered))
            .Subscribe(new AnonymousObserver<EventPattern<PointerEventArgs>>(
                onNext: _ => IsHovered = true
            ));

        Observable.FromEventPattern<PointerEventArgs>(myButton, nameof(PointerExited))
            .Subscribe(new AnonymousObserver<EventPattern<PointerEventArgs>>(
                onNext: _ => IsHovered = false
            ));
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}