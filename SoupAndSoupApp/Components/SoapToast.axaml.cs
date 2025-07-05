using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using ReactiveUI;
using SoupAndSoupApp.Models;

namespace SoupAndSoupApp.Components;

public partial class SoapToast : UserControl
{
    public static readonly StyledProperty<UiNotification?> NotificationProperty =
        AvaloniaProperty.Register<SoapToast, UiNotification?>(nameof(Notification));

    public UiNotification? Notification
    {
        get => GetValue(NotificationProperty);
        set => SetValue(NotificationProperty, value);
    }

    public static readonly StyledProperty<bool> IsToastVisibleProperty =
        AvaloniaProperty.Register<SoapToast, bool>(
            nameof(IsToastVisible), defaultValue: false);

    public bool IsToastVisible
    {
        get => GetValue(IsToastVisibleProperty);
        set =>
            SetValue(IsToastVisibleProperty, value);
    }

    public SoapToast()
    {
        InitializeComponent();
        Notification = null;
        IsToastVisible = false;

        this.GetObservable(NotificationProperty)
            .Subscribe(OnNotificationChanged);
    }

    private CancellationTokenSource? _cts;

    private void OnNotificationChanged(UiNotification? notification)
    {
        if (notification is null) return;
        IsToastVisible = true;

        ClearClasses(ToastContainer);
        AddClass(ToastContainer, notification.Level);

        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        Dispatcher.UIThread.InvokeAsync(async () =>
        {
            try
            {
                await Task.Delay(2000, token);
                if (!token.IsCancellationRequested)
                {
                    IsToastVisible = false;
                }
            }
            catch (TaskCanceledException)
            {
                // Ignore cancellation
            }
        });
    }

    private void AddClass(StyledElement control, NotificationLevel level)
    {
        switch (level)
        {
            case NotificationLevel.Success:
                control.Classes.Add("Success");
                break;
            case NotificationLevel.Error:
                control.Classes.Add("Error");
                break;
            case NotificationLevel.Warning:
                control.Classes.Add("Warning");
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(level), level, null);
        }
    }

    private void ClearClasses(StyledElement control)
    {
        control.Classes.Remove("Success");
        control.Classes.Remove("Error");
        control.Classes.Remove("Warning");
    }
    private void CloseButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        // Clear the notification when the close button is clicked
        IsToastVisible = false;
        _cts?.Cancel(); // Cancel any ongoing auto-hide timer
    }
}