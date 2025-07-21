using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Input;
using ReactiveUI;
using SoupAndSoupApp.Helpers;
using SoupAndSoupApp.Models;

namespace SoupAndSoupApp.ViewModels;

public class MainViewModel : ViewModelBase
{
    public SoapDesignerViewModel CurrentContentViewModel  
    {
        get => _currentContentViewModel;
        set => this.RaiseAndSetIfChanged(ref _currentContentViewModel, value);
    }

    public UiNotification? CurrentNotification
    {
        get => _currentNotification;
        set => this.RaiseAndSetIfChanged(ref _currentNotification, value);
    }

    public ObservableCollection<NavigationItem> NavigationItems { get; } = new();
 
    public NavigationItem SelectedNavigationItem
    {
        get => _selectedNavigationItem;
        set
        {
            if (_selectedNavigationItem == value) return;

            // Save changes in the current content view model before switching
            SaveChangesInCurrentContentAsync(CurrentContentViewModel);

            this.RaiseAndSetIfChanged(ref _selectedNavigationItem, value);
            
            LoadContentForSelectedItemAsync(_selectedNavigationItem.Mode);
        }
    }

    public bool IsNavigationExpanded
    {
        get => _isNavigationExpanded;
        set => this.RaiseAndSetIfChanged(ref _isNavigationExpanded, value);
    }

    public ICommand ToggleNavigationCommand { get; }


    public MainViewModel(IDesignerViewModelFactory designerViewModelFactory, INotificationService notificationService)
    {
        _designerViewModelFactory = designerViewModelFactory;

        NavigationItems.Add(new NavigationItem
        {
            Title = "Мило",
            IconPath = "M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm0 18c-4.41 0-8-3.59-8-8s3.59-8 8-8 8 3.59 8 8-3.59 8-8 8zM9.5 11.5L7 14l2.5 2.5L12 14l-2.5-2.5zm5 0L17 14l-2.5 2.5L12 14l2.5-2.5zM12 7c-1.38 0-2.5 1.12-2.5 2.5S10.62 12 12 12s2.5-1.12 2.5-2.5S13.38 7 12 7z", 
            Mode = CosmeticType.Soap
        });
        NavigationItems.Add(new NavigationItem
        {
            Title = "Парфуми",
            IconPath = "M16 9c0 1.1-.9 2-2 2s-2-.9-2-2 .9-2 2-2 2 .9 2 2zm-4 7c-1.1 0-2 .9-2 2s.9 2 2 2 2-.9 2-2-.9-2-2-2zM20 7h-2V5c0-1.1-.9-2-2-2H8c-1.1 0-2 .9-2 2v2H4c-1.1 0-2 .9-2 2v8h20v-8c0-1.1-.9-2-2-2zm-6 2c-.55 0-1-.45-1-1s.45-1 1-1 1 .45 1 1-.45 1-1 1zm0 4c-.55 0-1-.45-1-1s.45-1 1-1 1 .45 1 1-.45 1-1 1zM8 5h8v2H8V5zm12 10H4V9h16v6z", 
            Mode = CosmeticType.Parfum
        });

        if (NavigationItems.Any())
        {
            SelectedNavigationItem = NavigationItems.First();
        }

        notificationService.Notifications
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(notification =>
            {
                CurrentNotification = notification.ToUiNotification();
            });
        ToggleNavigationCommand = ReactiveCommand.Create(() => IsNavigationExpanded = !IsNavigationExpanded);
    }

    public MainViewModel()
    {
        NavigationItems.Add(new NavigationItem
        {
            Title = "Мило",
            IconPath = "M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm0 18c-4.41 0-8-3.59-8-8s3.59-8 8-8 8 3.59 8 8-3.59 8-8 8zM9.5 11.5L7 14l2.5 2.5L12 14l-2.5-2.5zm5 0L17 14l-2.5 2.5L12 14l2.5-2.5zM12 7c-1.38 0-2.5 1.12-2.5 2.5S10.62 12 12 12s2.5-1.12 2.5-2.5S13.38 7 12 7z",
            Mode = CosmeticType.Soap
        });
        NavigationItems.Add(new NavigationItem
        {
            Title = "Парфуми",
            IconPath = "M16 9c0 1.1-.9 2-2 2s-2-.9-2-2 .9-2 2-2 2 .9 2 2zm-4 7c-1.1 0-2 .9-2 2s.9 2 2 2 2-.9 2-2-.9-2-2-2zM20 7h-2V5c0-1.1-.9-2-2-2H8c-1.1 0-2 .9-2 2v2H4c-1.1 0-2 .9-2 2v8h20v-8c0-1.1-.9-2-2-2zm-6 2c-.55 0-1-.45-1-1s.45-1 1-1 1 .45 1 1-.45 1-1 1zm0 4c-.55 0-1-.45-1-1s.45-1 1-1 1 .45 1 1-.45 1-1 1zM8 5h8v2H8V5zm12 10H4V9h16v6z",
            Mode = CosmeticType.Parfum
        });


        this.RaiseAndSetIfChanged(ref _selectedNavigationItem, NavigationItems.First());
        
    }

    private async void SaveChangesInCurrentContentAsync(SoapDesignerViewModel currentViewModel)
    {
        if (currentViewModel is not IAutoSaveCandidate viewModel) return;

        await viewModel.SaveIfNeededAsync();
    }

    private async void LoadContentForSelectedItemAsync(CosmeticType mode)
    {
        if (!_cachedDesignerViewModels.TryGetValue(mode, out var vm))
        {
            vm = _designerViewModelFactory.CreateDesignerViewModel(mode);
            _cachedDesignerViewModels.Add(mode, vm);
        }

        CurrentContentViewModel = vm;

        if (CurrentContentViewModel is IInitializableVM initializableVm)
        {
            await initializableVm.InitializeAsync();
        }
    }
    
    private readonly IDesignerViewModelFactory _designerViewModelFactory;
    private readonly Dictionary<CosmeticType, SoapDesignerViewModel> _cachedDesignerViewModels = new();

    private SoapDesignerViewModel _currentContentViewModel;
    private NavigationItem _selectedNavigationItem;
    private UiNotification? _currentNotification;

    private bool _isNavigationExpanded = true;
}