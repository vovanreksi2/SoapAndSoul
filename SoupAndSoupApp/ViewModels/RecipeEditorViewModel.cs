using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using DynamicData;
using DynamicData.Binding;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using SoupAndSoupApp.Helpers;
using SoupAndSoupApp.Helpers.Autosave;
using SoupAndSoupApp.Helpers.Coordinators;
using SoupAndSoupApp.Models;
using CosmeticType = SoupAndSoupApp.Models.CosmeticType;

namespace SoupAndSoupApp.ViewModels;

public class RecipeEditorViewModel : ViewModelBase, IAutoSaveCandidate
{
    private readonly SourceCache<ComponentModel, int> _cachedComponents;
    private readonly RecipeCoordinator _recipeCoordinator;
    private readonly ComponentCoordinator _componentCoordinator;
    private readonly ILogger<RecipeEditorViewModel> _logger;

    private CosmeticType _currentCosmeticType;
    private bool _isLoadingRecipe;

    private ReadOnlyObservableCollection<ComponentModel> _componentsByRecipe =
        ReadOnlyObservableCollection<ComponentModel>.Empty;

    public ReadOnlyObservableCollection<ComponentModel> ComponentsByRecipe => _componentsByRecipe;

    private bool _isDirty;
    public bool IsDirty
    {
        get => _isDirty;
        set => this.RaiseAndSetIfChanged(ref _isDirty, value);
    }

    public RecipeModel? SelectedRecipe { get; set; }

    public string NewImagePath
    {
        get => _newImagePath;
        set
        {
            if (_newImagePath == value) return;
            if (SelectedRecipe is not null)
                SelectedRecipe.ImagePath = ImageHelper.LoadFromResource(value);
            this.RaiseAndSetIfChanged(ref _newImagePath, value);
        }
    }

    private string _newImagePath = string.Empty;

    public RecipeEditorViewModel(
        SourceCache<ComponentModel, int> cachedComponents,
        RecipeCoordinator recipeCoordinator,
        ComponentCoordinator componentCoordinator,
        ILogger<RecipeEditorViewModel> logger)
    {
        _cachedComponents = cachedComponents;
        _recipeCoordinator = recipeCoordinator;
        _componentCoordinator = componentCoordinator;
        _logger = logger;

        _cachedComponents.Connect()
            .AutoRefresh(x => x.IsInCurrentRecipe)
            .Filter(x => x.IsInCurrentRecipe)
            .Sort(SortExpressionComparer<ComponentModel>
                .Ascending(x => x.Type)
                .ThenByAscending(x => IsLatin(x.Name))
                .ThenByAscending(x => x.Name))
            .Bind(out _componentsByRecipe)
            .DisposeMany()
            .Subscribe()
            .DisposeWith(Disposables);

        _cachedComponents.Connect()
            .AutoRefresh(x => x.AmountInRecipe)
            .Where(_ => !_isLoadingRecipe)
            .Subscribe(_ => IsDirty = true)
            .DisposeWith(Disposables);
    }

    public void SetCosmeticType(CosmeticType cosmeticType) => _currentCosmeticType = cosmeticType;

    public void BeginLoading() => _isLoadingRecipe = true;
    public void EndLoading() => _isLoadingRecipe = false;

    public async Task<bool> HandleRecipeChangedAsync(RecipeModel? newValue, RecipeModel? oldValue)
    {
        var isSaveSuccess = await _recipeCoordinator.SavePreviouslySelectedRecipeAsync(
            oldValue, IsDirty, NewImagePath, _currentCosmeticType, RecipeListViewModel.NoImageRecipe);

        if (isSaveSuccess) IsDirty = false;

        if (!isSaveSuccess)
        {
            _logger.LogError("Failed to save previously selected recipe, aborting selection change.");
            return false;
        }

        _isLoadingRecipe = true;
        try
        {
            foreach (var comp in _cachedComponents.Items.Where(c => c.IsInCurrentRecipe))
            {
                comp.IsInCurrentRecipe = false;
                comp.AmountInRecipe = 0;
            }

            if (newValue is null)
            {
                _logger.LogWarning("New value is null, cannot update components.");
                return true;
            }

            foreach (var selection in newValue.SelectedComponents.Items)
            {
                var component = GetCachedComponentById(selection.ComponentId);
                if (component is null) continue;

                component.IsInCurrentRecipe = true;
                component.AmountInRecipe = selection.Amount;
                selection.Component = component;
            }

            newValue.UnitCost = _componentCoordinator.CalculateUnitCost(_cachedComponents);
        }
        finally
        {
            _isLoadingRecipe = false;
        }

        return true;
    }

    public void ReCalculateUnitCost()
    {
        if (SelectedRecipe is null)
        {
            _logger.LogWarning("Selected recipe is null, cannot calculate unit cost.");
            return;
        }

        SelectedRecipe.UnitCost = _componentCoordinator.CalculateUnitCost(_cachedComponents);
    }

    public Task<bool> SaveIfNeededAsync(CancellationToken cancellationToken = default) =>
        _recipeCoordinator.SavePreviouslySelectedRecipeAsync(
            SelectedRecipe, IsDirty, NewImagePath, _currentCosmeticType, RecipeListViewModel.NoImageRecipe);

    public bool ShouldSave(CancellationToken cancellationToken = default) =>
        _recipeCoordinator.ShouldSaveRecipe(SelectedRecipe, IsDirty);

    private ComponentModel? GetCachedComponentById(int id)
    {
        var lookup = _cachedComponents.Lookup(id);
        if (lookup.HasValue) return lookup.Value;

        _logger.LogWarning("Component with ID {componentId} not found in cache", id);
        return null;
    }

    private static bool IsLatin(string name)
    {
        if (string.IsNullOrEmpty(name)) return false;
        char c = name[0];
        return c >= 'A' && c <= 'z';
    }
}
