using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DynamicData;
using DynamicData.Binding;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using SoupAndSoupApp.Helpers.Coordinators;
using SoupAndSoupApp.Helpers.Rules;
using SoupAndSoupApp.Models;
using ComponentType = SoupAndSoupApp.Models.ComponentType;
using CosmeticType = SoupAndSoupApp.Models.CosmeticType;

namespace SoupAndSoupApp.ViewModels;

public class ComponentLibraryViewModel : ViewModelBase
{
    public const string NoImage_Component_Image = "Assets/No_Component_Photo.png";

    private readonly SourceCache<ComponentModel, int> _cachedComponents;
    private readonly Dictionary<ComponentType, ComponentTypeModel> _cachedComponentTypes;
    private readonly ComponentCoordinator _componentCoordinator;
    private readonly DesignerDataLoader _dataLoader;
    private readonly IComponentSelectionRuleEngine _ruleEngine;
    private readonly ActivitySource _activitySource;
    private readonly Func<RecipeModel?> _getSelectedRecipe;
    private readonly Action _onComponentToggled;
    private readonly ILogger<ComponentLibraryViewModel> _logger;

    private CosmeticType _currentCosmeticType;

    private ReadOnlyObservableCollection<ComponentGroup> _componentGroups =
        ReadOnlyObservableCollection<ComponentGroup>.Empty;

    public ReadOnlyObservableCollection<ComponentGroup> ComponentGroups => _componentGroups;

    public ReactiveCommand<ComponentType, Unit> NewComponentCommand { get; }
    public ReactiveCommand<ComponentModel, Unit> EditComponentCommand { get; }
    public ReactiveCommand<ComponentModel, Unit> DeleteComponentCommand { get; }
    public ReactiveCommand<ComponentModel, Unit> ToggleInRecipeCommand { get; }

    public ComponentLibraryViewModel(
        SourceCache<ComponentModel, int> cachedComponents,
        Dictionary<ComponentType, ComponentTypeModel> cachedComponentTypes,
        ComponentCoordinator componentCoordinator,
        DesignerDataLoader dataLoader,
        IComponentSelectionRuleEngine ruleEngine,
        ActivitySource activitySource,
        Func<RecipeModel?> getSelectedRecipe,
        Action onComponentToggled,
        ILogger<ComponentLibraryViewModel> logger)
    {
        _cachedComponents = cachedComponents;
        _cachedComponentTypes = cachedComponentTypes;
        _componentCoordinator = componentCoordinator;
        _dataLoader = dataLoader;
        _ruleEngine = ruleEngine;
        _activitySource = activitySource;
        _getSelectedRecipe = getSelectedRecipe;
        _onComponentToggled = onComponentToggled;
        _logger = logger;

        NewComponentCommand = ReactiveCommand.CreateFromTask<ComponentType>(AddComponentAsync);
        EditComponentCommand = ReactiveCommand.CreateFromTask<ComponentModel>(EditComponentAsync);
        DeleteComponentCommand = ReactiveCommand.CreateFromTask<ComponentModel>(DeleteComponentAsync, Observable.Return(true));
        ToggleInRecipeCommand = ReactiveCommand.Create<ComponentModel>(ToggleComponentInRecipe);

        _cachedComponents.Connect()
            .Group(c => c.Type)
            .Transform(group =>
            {
                _cachedComponentTypes.TryGetValue(group.Key, out var componentType);

                var groupVm = new ComponentGroup
                {
                    ComponentType = componentType,
                    NewComponentCommand = NewComponentCommand,
                    Components = new ObservableCollectionExtended<ComponentModel>()
                };

                group.Cache.Connect()
                    .AutoRefreshOnObservable(_ => _.WhenAnyPropertyChanged())
                    .Sort(SortExpressionComparer<ComponentModel>
                        .Ascending(c => IsLatin(c.Name))
                        .ThenByAscending(c => c.Name))
                    .Bind(groupVm.Components)
                    .Subscribe();

                return groupVm;
            })
            .Sort(SortExpressionComparer<ComponentGroup>.Ascending(g => g.ComponentType.Order))
            .Bind(out _componentGroups)
            .Subscribe()
            .DisposeWith(Disposables);
    }

    public void SetCosmeticType(CosmeticType cosmeticType) => _currentCosmeticType = cosmeticType;

    public Task LoadComponentTypesAsync() =>
        _dataLoader.LoadComponentTypesAsync(_activitySource, _currentCosmeticType, _cachedComponents, _cachedComponentTypes);

    public Task LoadComponentsAsync() =>
        _dataLoader.LoadComponentsAsync(
            _activitySource, _currentCosmeticType, _cachedComponents,
            EditComponentCommand, DeleteComponentCommand, ToggleInRecipeCommand,
            NoImage_Component_Image);

    private void ToggleComponentInRecipe(ComponentModel component)
    {
        var selectedRecipe = _getSelectedRecipe();
        if (selectedRecipe == null || component.IsButton) return;

        var selections = selectedRecipe.SelectedComponents;
        var existing = selections.Lookup(component.Id);

        if (existing.HasValue)
        {
            selections.Remove(component.Id);
            component.IsInCurrentRecipe = false;
            component.AmountInRecipe = 0;
        }
        else
        {
            var recipeComponent = new ComponentByRecipeModel
            {
                ComponentId = component.Id,
                Amount = component.SuggestedAmount,
                Component = component
            };
            selections.AddOrUpdate(recipeComponent);
            component.IsInCurrentRecipe = true;
            component.AmountInRecipe = component.SuggestedAmount;

            _ruleEngine.ApplySelectionRules(recipeComponent, selections, _cachedComponents);
            SyncIsInCurrentRecipeFromSelections(selections);
        }

        _onComponentToggled();
    }

    private void SyncIsInCurrentRecipeFromSelections(SourceCache<ComponentByRecipeModel, int> selections)
    {
        var selectedIds = selections.Items.Select(s => s.ComponentId).ToHashSet();
        foreach (var comp in _cachedComponents.Items.Where(c => !c.IsButton))
        {
            var shouldBeSelected = selectedIds.Contains(comp.Id);
            if (comp.IsInCurrentRecipe != shouldBeSelected)
            {
                comp.IsInCurrentRecipe = shouldBeSelected;
                if (!shouldBeSelected) comp.AmountInRecipe = 0;
            }
        }
    }

    private async Task AddComponentAsync(ComponentType parameter)
    {
        var group = ComponentGroups.FirstOrDefault(_ => _.ComponentType.Type == parameter);
        await _componentCoordinator.AddComponentAsync(
            group, _cachedComponents,
            EditComponentCommand, DeleteComponentCommand, ToggleInRecipeCommand,
            _currentCosmeticType, NoImage_Component_Image);
    }

    private async Task EditComponentAsync(ComponentModel? model)
    {
        var group = model != null
            ? ComponentGroups.FirstOrDefault(g => g.ComponentType.Type == model.Type)
            : null;
        await _componentCoordinator.EditComponentAsync(
            model, group, _cachedComponents, _currentCosmeticType, NoImage_Component_Image);
    }

    private async Task DeleteComponentAsync(ComponentModel component)
    {
        var group = ComponentGroups.FirstOrDefault(_ => _.ComponentType.Type == component.Type);
        await _componentCoordinator.DeleteComponentAsync(
            component, _cachedComponents, _getSelectedRecipe(), group);
    }

    private static bool IsLatin(string name)
    {
        if (string.IsNullOrEmpty(name)) return false;
        char c = name[0];
        return c >= 'A' && c <= 'z';
    }
}
