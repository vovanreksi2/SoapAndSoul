using System;
using Microsoft.Extensions.DependencyInjection;
using SoupAndSoupApp.Models;
using SoupAndSoupApp.ViewModels;

namespace SoupAndSoupApp.Helpers;

public class DesignerViewModelFactory : IDesignerViewModelFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IActiveViewModelRegistry _viewModelRegistry;

    public DesignerViewModelFactory(IServiceProvider serviceProvider, IActiveViewModelRegistry viewModelRegistry)
    {
        _serviceProvider = serviceProvider;
        _viewModelRegistry = viewModelRegistry;
    }

    public SoapDesignerViewModel CreateDesignerViewModel(CosmeticType type)
    {
        var vm = _serviceProvider.GetRequiredService<SoapDesignerViewModel>();
        vm.SetType(type);
        _viewModelRegistry.Register(vm);
        return vm;
    }
}

