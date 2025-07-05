using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using ReactiveUI;
using SoupAndSoupApp.Helpers;
using SoupAndSoupApp.Models;

namespace SoupAndSoupApp.ViewModels;

public class MainViewModel : ViewModelBase
{
    private object? _isSoupDesignerVisible = false;
    public object? IsSoupDesignerVisible
    {
        get => _isSoupDesignerVisible;
        set => this.RaiseAndSetIfChanged(ref _isSoupDesignerVisible, value);
    }

    private object _currentViewModel;

    public object CurrentViewModel
    {
        get => _currentViewModel;
        set => this.RaiseAndSetIfChanged(ref _currentViewModel, value);
    }

    public ReactiveCommand<Unit, Unit> ShowReceipt{ get; }
    public ReactiveCommand<Unit, Unit> ShowDesigner { get; }

    private object _soapDesignerDataContext;
    public object SoapDesignerDataContext
    {
        get => _soapDesignerDataContext;
        set => this.RaiseAndSetIfChanged(ref _soapDesignerDataContext, value);
    }


    private UiNotification? _currentNotification;
    public UiNotification? CurrentNotification
    {
        get => _currentNotification;
        set => this.RaiseAndSetIfChanged(ref _currentNotification, value);
    }

    public MainViewModel(SoapDesignerViewModel soapDesignerDataContext, INotificationService notificationService)
    {
        _soapDesignerDataContext = soapDesignerDataContext;

        IsSoupDesignerVisible = null;

        ShowReceipt = ReactiveCommand.Create(ShowSoupReceipt);
        ShowDesigner = ReactiveCommand.Create(ShowSoupDesigner);

        notificationService.Notifications
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(notification =>
            {
                CurrentNotification = notification.ToUiNotification();
            });
    }

    public MainViewModel()
    {
        IsSoupDesignerVisible = null;

        ShowReceipt = ReactiveCommand.Create(ShowSoupReceipt);
        ShowDesigner = ReactiveCommand.Create(ShowSoupDesigner);
    }

    public void ShowSoupDesigner()
    {
        IsSoupDesignerVisible = new object();

    }

    public void ShowSoupReceipt()
    {
        IsSoupDesignerVisible = null;
    }
}