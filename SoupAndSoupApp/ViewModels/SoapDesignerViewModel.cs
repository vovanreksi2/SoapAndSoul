using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using DynamicData;
using ReactiveUI;
using SoapAndSoul.Infrastructure;
using SoupAndSoupApp.Helpers;
using SoupAndSoupApp.Helpers.Autosave;
using SoupAndSoupApp.Helpers.Coordinators;
using SoupAndSoupApp.Helpers.Navigation;
using SoupAndSoupApp.Helpers.Notifications;
using SoupAndSoupApp.Helpers.Rules;
using SoupAndSoupApp.Models;
using ComponentType = SoupAndSoupApp.Models.ComponentType;
using CosmeticType = SoupAndSoupApp.Models.CosmeticType;

namespace SoupAndSoupApp.ViewModels;

public class SoapDesignerViewModel : ViewModelBase, IAutoSaveCandidate, IInitializableVM
{
    // Shared state — owned here, passed by reference to sub-VMs
    private readonly SourceCache<ComponentModel, int> _cachedComponents = new(c => c.Id);
    private readonly Dictionary<ComponentType, ComponentTypeModel> _cachedComponentTypes = [];

    public RecipeListViewModel RecipeList { get; }
    public RecipeEditorViewModel RecipeEditor { get; }
    public ComponentLibraryViewModel ComponentLibrary { get; }

    private readonly ILogger<SoapDesignerViewModel> _logger;
    private readonly ActivitySource _activitySource;
    private readonly INotificationService _notificationService;
    private readonly DesignerDataLoader _dataLoader;

    private CosmeticType _currentCosmeticType;
    private bool _isInitialized;

    // Design-time constructor
    public SoapDesignerViewModel()
    {
        RecipeList = null!;
        RecipeEditor = null!;
        ComponentLibrary = null!;
    }

    public SoapDesignerViewModel(
        ILogger<SoapDesignerViewModel> logger,
        ILoggerFactory loggerFactory,
        InstrumentationOpenTelemetry instrumentation,
        INotificationService notificationService,
        IComponentSelectionRuleEngine ruleEngine,
        RecipeCoordinator recipeCoordinator,
        ComponentCoordinator componentCoordinator,
        DesignerDataLoader dataLoader)
    {
        _logger = logger;
        _activitySource = instrumentation.ActivitySource;
        _notificationService = notificationService;
        _dataLoader = dataLoader;

        RecipeList = new RecipeListViewModel(recipeCoordinator, loggerFactory.CreateLogger<RecipeListViewModel>());
        RecipeEditor = new RecipeEditorViewModel(
            _cachedComponents, recipeCoordinator, componentCoordinator,
            loggerFactory.CreateLogger<RecipeEditorViewModel>());
        ComponentLibrary = new ComponentLibraryViewModel(
            _cachedComponents, _cachedComponentTypes,
            componentCoordinator, dataLoader, ruleEngine, _activitySource,
            () => RecipeList.SelectedRecipe,
            OnComponentToggled,
            loggerFactory.CreateLogger<ComponentLibraryViewModel>());

        RecipeList.WhenAnyValue(x => x.SelectedRecipe)
            .Where(x => x is not null)
            .SelectMany(async newValue =>
            {
                var previousValue = RecipeList.PreviousSelectedRecipe;
                var success = await RecipeEditor.HandleRecipeChangedAsync(newValue, previousValue);
                if (!success && previousValue is not null)
                    RecipeList.SelectedRecipe = previousValue;
                else
                    RecipeEditor.SelectedRecipe = newValue;
                return Unit.Default;
            })
            .Subscribe()
            .DisposeWith(Disposables);
    }

    private void OnComponentToggled()
    {
        RecipeEditor.IsDirty = true;
        RecipeEditor.ReCalculateUnitCost();
    }

    public void SetType(CosmeticType mode)
    {
        _currentCosmeticType = mode;
        RecipeEditor.SetCosmeticType(mode);
        ComponentLibrary.SetCosmeticType(mode);
    }

    public async Task InitializeAsync()
    {
        _logger.LogInformation("InitializeAsync invoked for {CosmeticType}", _currentCosmeticType);
        if (_isInitialized)
        {
            _logger.LogInformation("InitializeAsync skipped: already initialized");
            return;
        }

        using var activity = _activitySource.StartActivity("InitializeAsync", ActivityKind.Client);
        activity?.SetTag("cosmetic.type", _currentCosmeticType.ToString());

        try
        {
            RecipeEditor.BeginLoading();

            await Task.WhenAll(
                ComponentLibrary.LoadComponentTypesAsync(),
                ComponentLibrary.LoadComponentsAsync(),
                LoadRecipesAsync());

            if (RecipeList.Recipes.Any())
                RecipeList.SelectedRecipe = RecipeList.Recipes.FirstOrDefault();
            else
                RecipeList.CreateNewRecipePlaceholder();
        }
        catch (Exception e)
        {
            ActivityHelper.RecordInitException(e, activity, _logger);
            _notificationService.Notify(DomainNotificationType.ErrorDuringInit);
        }
        finally
        {
            RecipeEditor.EndLoading();
            _isInitialized = true;
            _logger.LogInformation("InitializeAsync: Completed for {CosmeticType}", _currentCosmeticType);
        }
    }

    private Task LoadRecipesAsync() =>
        _dataLoader.LoadRecipesAsync(
            _activitySource, _currentCosmeticType, RecipeList.CachedRecipes,
            _cachedComponents, RecipeList.DeleteRecipeCommand, RecipeListViewModel.NoImageRecipe);

    public Task<bool> SaveIfNeededAsync(CancellationToken cancellationToken = default) =>
        RecipeEditor.SaveIfNeededAsync(cancellationToken);

    public bool ShouldSave(CancellationToken cancellationToken = default) =>
        RecipeEditor.ShouldSave(cancellationToken);
}
