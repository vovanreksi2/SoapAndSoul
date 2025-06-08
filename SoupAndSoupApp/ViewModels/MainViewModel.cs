using System.Reactive;
using ReactiveUI;
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

    public MainViewModel(SoapDesignerViewModel soapDesignerDataContext)
    {
        _soapDesignerDataContext = soapDesignerDataContext;
        
        IsSoupDesignerVisible = null;

        ShowReceipt = ReactiveCommand.Create(ShowSoupReceipt);
        ShowDesigner = ReactiveCommand.Create(ShowSoupDesigner);
        
    }

    public MainViewModel()
    {
        IsSoupDesignerVisible = null;

        ShowReceipt = ReactiveCommand.Create(ShowSoupReceipt);
        ShowDesigner = ReactiveCommand.Create(ShowSoupDesigner);
    }


    // Метод для переключення ViewModel
    public void ShowSoupDesigner()
    {
        IsSoupDesignerVisible = new object();

    }

    public void ShowSoupReceipt()
    {
        IsSoupDesignerVisible = null;
    }
}
