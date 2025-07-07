using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using SoupAndSoup.Data.Models;
using SoupAndSoup.Data.Services;
using SoupAndSoupApp.Helpers;
using SoupAndSoupApp.Models;
using ComponentType = SoupAndSoupApp.Models.ComponentType;
using CosmeticType = SoupAndSoupApp.Models.CosmeticType;

namespace SoupAndSoupApp.ViewModels;

public class SoapDesignerViewModel : ViewModelBase, IAutoSaveCandidate
{
    public const string NoImage_Receipt = "Assets/No_Receipt_Photo.png";
    public const string NoImage_Component_Image = "Assets/No_Component_Photo.png";

    public ICommand NewReceiptCommand { get; private set; }
    public ICommand DeleteReceiptCommand { get; private set; }

    public ReactiveCommand<ComponentType, Unit> NewComponentCommand { get; private set; }
    public ReactiveCommand<ComponentModel, Unit> EditComponentCommand { get; private set; }
    public ReactiveCommand<ComponentModel, Unit> DeleteComponentCommand { get; private set; }


    public bool IsRecipeAddMode
    {
        get => _isRecipeAddMode;
        set => this.RaiseAndSetIfChanged(ref _isRecipeAddMode, value);
    }

    public string NewImagePath
    {
        get => _newImagePath;
        set
        {
            if (_newImagePath == value) return;
            if (SelectedRecipe != null) 
                SelectedRecipe.ImagePath = ImageHelper.LoadFromResource(value);

            this.RaiseAndSetIfChanged(ref _newImagePath, value);
        }
    }

    public ObservableCollection<RecipeModel> Recipes { get; } = new();

    public RecipeModel? SelectedRecipe
    {
        get => _selectedRecipe;
        set
        {
            if (_selectedRecipe == value || value is null)  return;

            _previousSelectedRecipe = _selectedRecipe; // Track previous value
            this.RaiseAndSetIfChanged(ref _selectedRecipe, value);
        }
    }

    public ObservableCollection<ComponentGroup> ComponentGroups { get; set; } = new();

    
    private readonly SourceCache<ComponentModel, int> _cachedComponents = new(component => component.Id);


    private readonly ReadOnlyObservableCollection<ComponentModel> _componentsByRecipe = ReadOnlyObservableCollection<ComponentModel>.Empty;
    public ReadOnlyObservableCollection<ComponentModel> ComponentsByRecipe => _componentsByRecipe;

    public bool IsDirty { get; set; }

    public Task Initialization { get; }


    public SoapDesignerViewModel()
    {
        InitView();
    }

    public SoapDesignerViewModel(
        RecipeService recipeService,
        ComponentService componentService,
        ComponentTypeService componentTypeService,
        IDialogService dialogService, IUnitCostCalculator unitCostCalc, MeasureTypeCache measureTypeCache,
        INotificationService notificationService)
    {
        _recipeService = recipeService;
        _componentService = componentService;
        _componentTypeService = componentTypeService;

        _dialogService = dialogService;
        _unitCostCalc = unitCostCalc;
        _measureTypeCache = measureTypeCache;
        _notificationService = notificationService;

        try
        {
            Initialization = InitializeAsync();

            _cachedComponents.Connect()
                .AutoRefresh(x => x.IsSelected)
                .AutoRefresh(x => x.AmountInRecipe)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ =>
                {
                    if (_suppressIsDirty) return;
                    IsDirty = true;
                })
                .DisposeWith(Disposables);

            _cachedComponents.Connect()
                .AutoRefresh(x => x.IsSelected)
                .Filter(x => x.IsSelected)
                .Sort(SortExpressionComparer<ComponentModel>
                    .Ascending(x => x.Type)
                    .ThenByAscending(x => IsLatin(x.Name))
                    .ThenByAscending(x => x.Name))
                .Bind(out _componentsByRecipe)
                .DisposeMany()
                .Subscribe()
                .DisposeWith(Disposables);


            this.WhenAnyValue(x => x.SelectedRecipe)
                .Where(x => x != null) // Optional: skip nulls
                .SelectMany(async newValue =>
                {
                    _suppressIsDirty = true;
                    try
                    {
                        // Use _previousSelectedRecipe for the previous value
                        await HandleChangeSelectedRecipeAsync(newValue, _previousSelectedRecipe, _componentsByRecipe);
                    }
                    finally
                    {
                        _suppressIsDirty = false;
                    }
                   
                    return Unit.Default;
                })
                .Subscribe()
                .DisposeWith(Disposables);

        }
        catch (Exception ex)
        {
            Debug.WriteLine($"‼️ Exception: {ex.Message}");
            Debug.WriteLine(ex.StackTrace);
        }

        InitView();
    }


    private void InitView()
    {
        NewReceiptCommand = ReactiveCommand.Create(NewReceipt);
        DeleteReceiptCommand = ReactiveCommand.CreateFromTask<RecipeModel>(DeleteReceiptAsync);

        NewComponentCommand = ReactiveCommand.CreateFromTask<ComponentType>(AddComponentAsync);
        EditComponentCommand = ReactiveCommand.CreateFromTask<ComponentModel>(EditComponentAsync);
        DeleteComponentCommand = ReactiveCommand.CreateFromTask<ComponentModel>(DeleteComponentAsync, Observable.Return(true));
    }

    private async Task InitializeAsync()
    {
        const int cosmeticType = (int)CosmeticType.Soap;
        try
        {
            _suppressIsDirty = true;

            var componentTypes = await _componentTypeService.GetAllAsync(cosmeticType);
            _cachedComponentTypes = componentTypes.ToDictionary(type => type.Id, type => new ComponentTypeModel(type));

            var components = await _componentService.GetAllAsync(cosmeticType);
            ;
            foreach (var mappedComponent in await Task.WhenAll(components.Select(MapComponentModelAsync)))
            {
                _cachedComponents.AddOrUpdate(mappedComponent);
            }

            ComponentGroups.AddRange(
                _cachedComponentTypes
                    .OrderBy(_ => _.Value.Order)
                    .Select(componentType =>
                        MapComponentGroup(componentType.Value,
                            _cachedComponents.Items.Where(_ => _.Type == componentType.Value.Type)))
            );

            var recipes = await _recipeService.GetAllAsync();
            var recipeModels = recipes.Select(MapRecipe);

            Recipes.AddRange(recipeModels);
            SelectedRecipe = Recipes.FirstOrDefault();
        }
        catch (Exception e)
        {
            Debug.WriteLine($"‼️ Exception during initialization: {e.Message}");
        }
        finally
        {
            _suppressIsDirty = false;
        }
    }


    private void NewReceipt()
    {
        var newRecipe = new RecipeModel
        {
            Name = "Нова Рецептура",
            Description = string.Empty,
            ImagePath = ImageHelper.LoadFromResource(NoImage_Receipt),
        };
        Recipes.Add(newRecipe);
        IsRecipeAddMode = true;

        SelectedRecipe = newRecipe;
    }
    private async Task<RecipeModel> DeleteReceiptAsync(RecipeModel recipeModel)
    {
        var deleteResult = await _recipeService.SoftDelete(recipeModel.Id);
        if (!deleteResult)
        {
            Debug.WriteLine($"Failed to delete recipe with ID {recipeModel.Id}.");
            return null;
        }

        _notificationService.Notify(DomainNotificationType.RecipeDeleted);

        Recipes.Remove(recipeModel);
        SelectedRecipe = Recipes.Count > 0 ? Recipes.FirstOrDefault() : null;
        return recipeModel;
    }

    
    private async Task AddComponentAsync(ComponentType parameter)
    {
        var group = ComponentGroups.First(_ => _.ComponentType.Type == parameter);
        
        var newComponentDto = await _dialogService.ShowAddEditComponentDialogAsync(false, group.ComponentType);
        if (newComponentDto is null) return;

        var component = MapComponent(newComponentDto, group.ComponentType.Type);

        var saveResult = await _componentService.CreateAsync(component);
        
        if (saveResult is null)
        {
            Debug.WriteLine($"Failed to save new component of type {group.ComponentType.Type}.");
            return;
        }

        _notificationService.Notify(DomainNotificationType.ComponentCreated);

        var tmpList = new List<ComponentModel>();
        tmpList.AddRange(group.Components);
        tmpList.Add(await MapComponentModelAsync(saveResult));

        group.Components.Clear();
        group.Components.AddRange(
            tmpList
                .OrderBy(x => IsLatin(x.Name))
                .ThenBy(x => x.Name));
    }

    private async Task EditComponentAsync(ComponentModel componentModel)
    {
        var group = ComponentGroups.First(_ => _.ComponentType.Type == componentModel.Type);

        var editedComponentDto =
            await _dialogService.ShowAddEditComponentDialogAsync(true, group.ComponentType, componentModel);
        if (editedComponentDto is null) return;

        var component = MapComponent(editedComponentDto, componentModel.Type, componentModel.Id);

        var saveResult = await _componentService.UpdateAsync(component);
        if (!saveResult)
        {
            Debug.WriteLine($"Failed to update component with ID {componentModel.Id}");
            return;
        }

        _notificationService.Notify(DomainNotificationType.ComponentUpdated);

        var existingComponent = GetCachedComponentById(component.Id);
        if (existingComponent is null) return;

        //TODO Change to Copy pattern
        existingComponent.Name = component.Name;
        existingComponent.BuyAmount = component.BuyAmount;
        existingComponent.Cost = component.Cost; 
        existingComponent.BuyPrice = component.BuyPrice;
        existingComponent.UseMeasureTypeId = component.UseMeasureTypeId;
        existingComponent.BuyMeasureTypeId = component.BuyMeasureTypeId;
        existingComponent.SuggestedAmount = component.SuggestedAmount;
        existingComponent.Type = (ComponentType)component.ComponentTypeId;

        existingComponent.ImagePath = component.Images.FirstOrDefault()?.ImageUrl is null
            ? componentModel.ImagePath
            : ImageHelper.LoadFromResource(component.Images.First().ImageUrl);

        Debug.WriteLine($"Success for save component with ID {componentModel.Id}");
    }

    private async Task DeleteComponentAsync(ComponentModel component)
    {
        var result = await _componentService.SoftDeleteAsync(component.Id);
        if (!result)
        {
            Debug.WriteLine($"Failed to delete component with ID {component.Id}.");
            return;
        }

        _notificationService.Notify(DomainNotificationType.ComponentDeleted);

        ComponentGroups.FirstOrDefault(_ => _.ComponentType.Type == component.Type)?.Components.Remove(component);
        _cachedComponents.Remove(component.Id);
    }



    private async Task HandleChangeSelectedRecipeAsync(RecipeModel? newValue, RecipeModel? oldValue, IEnumerable<ComponentModel> componentsByRecipe)
    {
        var isSaveSuccess = await SavePreviouslySelectedRecipeAsync(oldValue, componentsByRecipe);
        if (!isSaveSuccess)
        {
            LogError("Failed to save previously selected recipe, aborting selection change.");
            SelectedRecipe = oldValue; 
            return;
        }

        var tmpList = new List<ComponentModel>(componentsByRecipe);
        foreach (var componentByRecipe in tmpList)
        {
            var component = GetCachedComponentById(componentByRecipe.Id);
            if (component == null) continue;

            component.IsSelected = false;
        }

        if (newValue == null)
        {
            LogError("New value is null, cannot update components.");
            return;
        }

        foreach (var componentByRecipeModel in newValue.RecipeComponents)
        {
            var component = GetCachedComponentById(componentByRecipeModel.ComponentId);
            if (component == null) continue;

            component.IsSelected = true;
            component.AmountInRecipe = componentByRecipeModel.Amount;
        }

        ReCalculateUnitCost(componentsByRecipe);
    }
  
    private void HandleSelectedComponentChanged(ComponentModel componentModel)
    {
        try
        {
            _suppressSelectionChange = true;

            UpdateComponentInGroupAccordingToRules(componentModel);

            if (SelectedRecipe != null && !_suppressIsDirty)
                ReCalculateUnitCost(ComponentsByRecipe);
        }
        finally
        {
            _suppressSelectionChange = false;
        }
    }

    private void HandleAmountComponentChanged(ComponentModel componentModel)
    {
        if (componentModel.IsSelected && SelectedRecipe != null && !_suppressIsDirty)
            ReCalculateUnitCost(_componentsByRecipe);
    }

    private async Task<bool> SavePreviouslySelectedRecipeAsync(RecipeModel? oldRecipe, IEnumerable<ComponentModel> componentsByRecipe)
    {
        if (!ShouldSaveRecipe(oldRecipe)) return true;

        var isSuccess = await AddOrUpdateRecipeAsync(oldRecipe);
        if (!isSuccess) return false;

        ResetDirtyFlags(oldRecipe);

        UpdateRecipeComponents(oldRecipe);

        return true;

       

        void ResetDirtyFlags(RecipeModel recipe)
        {
            recipe.IsDirty = false;
            IsDirty = false;
        }

        void UpdateRecipeComponents(RecipeModel recipe)
        {
            recipe.RecipeComponents = componentsByRecipe.Select(MapRecipeComponentModel).ToList();
        }
    }
    private bool ShouldSaveRecipe(RecipeModel? recipe)
    {
        return recipe is not null && (IsDirty || recipe.IsDirty);
    }

    private async Task<bool> AddOrUpdateRecipeAsync(RecipeModel? inputRecipe)
    {
        if (inputRecipe == null) return false;

        var recipe = MapRecipe(inputRecipe);

        if (inputRecipe.IsNewRecipe)
        {
            var saveResult = await SaveCreatedRecipe(recipe);

            NotifyResult(saveResult, DomainNotificationType.RecipeCreated); 
            return saveResult;
        }

        var updateResult = await SaveUpdatedRecipe(recipe);

        NotifyResult(updateResult, DomainNotificationType.RecipeUpdated);
        return updateResult;
    }
    private async Task<bool> SaveCreatedRecipe(Recipe recipe)
    {
        try
        {
            var createdRecipe = await _recipeService.CreateAsync(recipe);
            return createdRecipe is not null && createdRecipe.Id > 0;
        }
        catch (Exception exception)
        {
            LogError("Failed to create new recipe with NAME {recipeName}", recipe.Name, exception);
            return false;
        }
    }
    private async Task<bool> SaveUpdatedRecipe(Recipe recipe)
    {
        try
        {
            return await _recipeService.UpdateAsync(recipe);
        }
        catch (Exception exception)
        {
            LogError("Failed to update recipe with NAME {recipeName} and ID {recipeId}.", recipe.Name, recipe.Id);
            return false;
        }
    }


    private void UpdateComponentInGroupAccordingToRules(ComponentModel componentModel)
    {
        //TODO: Change to dictionary
        switch (componentModel.Type)
        {
            case ComponentType.Form:
                UpdateComponents(ComponentsByRecipe, componentModel.Type);

                var craftingBase = ComponentsByRecipe.FirstOrDefault(_ => _.Type == ComponentType.CraftingBase);
                if (craftingBase != null)
                    craftingBase.AmountInRecipe = componentModel.SuggestedAmount;
                break;

            case ComponentType.EssentialOil:
                UpdateComponents(ComponentsByRecipe, ComponentType.FragranceOil);
                componentModel.AmountInRecipe = componentModel.SuggestedAmount;
                break;

            case ComponentType.FragranceOil:
                UpdateComponents(ComponentsByRecipe, ComponentType.EssentialOil);
                componentModel.AmountInRecipe = componentModel.SuggestedAmount;
                break;

            case ComponentType.CraftingBase:
                var form = ComponentsByRecipe.FirstOrDefault(_ => _.Type == ComponentType.Form);
                if (form != null && componentModel.IsSelected)
                    componentModel.AmountInRecipe = form.SuggestedAmount;
                else
                    componentModel.AmountInRecipe = componentModel.SuggestedAmount;

                break;
            case ComponentType.Pigment:
            case ComponentType.HerbalExtract:
            case ComponentType.Tools:
            case ComponentType.Other:
                componentModel.AmountInRecipe = componentModel.SuggestedAmount;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        void UpdateComponents(IEnumerable<ComponentModel> components, ComponentType typeComponent)
        {
            foreach (var component in components.Where(_ => _.Type == typeComponent && _.Id != componentModel.Id))
            {
                component.IsSelected = false;
            }
        }
    }

    private void ReCalculateUnitCost(IEnumerable<ComponentModel> components)
    {
        if (SelectedRecipe == null)
        {
            Debug.WriteLine("Selected receipt is null, cannot calculate unit cost.");
            return;
        }

        if (!components.Any())
        {
            Debug.WriteLine("No components found to calculate unit cost.");

            SelectedRecipe.UnitCost = 0;
            return;
        }

        SelectedRecipe.UnitCost = _unitCostCalc.CalculateUnitCost(components);
    }


    private RecipeModel MapRecipe(Recipe recipe)
    {
        var result = new RecipeModel();

        result.BeginInit();

        result.Id = recipe.Id;
        result.Name = recipe.Name;
        result.Description = recipe.Description;
        result.Amount = recipe.Amount;
        result.PreparationTime = (decimal)recipe.PreparationTime.TotalMinutes;
        result.DeleteReceiptCommand = DeleteReceiptCommand;

        result.RecipeComponents = recipe.RecipeComponents.Select(MapRecipeComponent);

        result.ImagePath = ImageHelper.LoadFromResource(recipe.Images.FirstOrDefault()?.ImageUrl ?? NoImage_Receipt);
        result.UnitCost = _unitCostCalc.CalculateUnitCost(result.RecipeComponents, _cachedComponents);

        result.EndInit();

        return result;
    }
    private Recipe MapRecipe(RecipeModel recipe)
    {
        var result = new Recipe
        {
            Id = recipe.Id,
            Amount = recipe.Amount,
            Name = recipe.Name,
            PreparationTime = TimeSpan.FromMinutes((int)recipe.PreparationTime),
            Type = CosmeticType.Soap.ToString(),
            Description = recipe.Description,
            RecipeComponents = ComponentsByRecipe.Select(MapRecipeComponent).ToList()
        };

        if (!string.IsNullOrEmpty(NewImagePath) && NewImagePath != NoImage_Receipt)
            result.Images = new List<RecipeImage> { new() { ImageUrl = NewImagePath } };

        return result;
    }

    private ComponentByRecipeModel MapRecipeComponent(RecipeComponent componentByRecipeModel) =>
        new()
        {
            ComponentId = componentByRecipeModel.ComponentId,
            Amount = componentByRecipeModel.Amount
        };
    private ComponentByRecipeModel MapRecipeComponentModel(ComponentModel componentByRecipeModel) =>
        new()
        {
            ComponentId = componentByRecipeModel.Id,
            Amount = componentByRecipeModel.AmountInRecipe
        }; 
    private RecipeComponent MapRecipeComponent(ComponentModel componentModel) =>
        new()
        {
            ComponentId = componentModel.Id,
            Amount = componentModel.AmountInRecipe
        };

    private ComponentGroup MapComponentGroup(ComponentTypeModel componentType, IEnumerable<ComponentModel> components)
    {
        var componentGroup = new ComponentGroup
        {
            NewComponentCommand = NewComponentCommand,
            ComponentType = componentType
        };

        var list = components
            .OrderBy(x => IsLatin(x.Name))
            .ThenBy(x => x.Name).ToList();
        componentGroup.Components.AddRange(list);
        
        return componentGroup;
    }

    private async Task<ComponentModel> MapComponentModelAsync(Component componentModel)
    {
        var componentType = (ComponentType)componentModel.ComponentTypeId;

        var result = new ComponentModel
        {
            Id = componentModel.Id,
            Name = componentModel.Name,
            Cost = componentModel.Cost,

            SuggestedAmount = componentModel.SuggestedAmount,
            BuyPrice = componentModel.BuyPrice,
            BuyAmount = componentModel.BuyAmount,

            DeleteCommand = DeleteComponentCommand,
            EditCommand = EditComponentCommand,
            ShowAmountInButton = componentType != ComponentType.Form,
            Type = componentType,
            
            BuyMeasureTypeId = componentModel.BuyMeasureTypeId,
            UseMeasureTypeId = componentModel.UseMeasureTypeId,
            BuyMeasureTypeShortTitle = (await _measureTypeCache.GetOrAddAsync(componentModel.BuyMeasureTypeId)).ShortTitle,
            UseMeasureTypeShortTitle = (await _measureTypeCache.GetOrAddAsync(componentModel.UseMeasureTypeId)).ShortTitle,

            ImagePath = ImageHelper.LoadFromResource(componentModel.Images.FirstOrDefault()?.ImageUrl ??
                                                     NoImage_Component_Image)
        };


        result
            .WhenAnyValue(x => x.IsSelected)
            .Where(_ => !_suppressSelectionChange)
            .Skip(1)
            .Subscribe(_ => { HandleSelectedComponentChanged(result); })
            .DisposeWith(Disposables);

        result
            .WhenAnyValue(x => x.AmountInRecipe)
            .Skip(1)
            .Subscribe(_ => { HandleAmountComponentChanged(result);})
            .DisposeWith(Disposables);

        return result;
    }

    private Component MapComponent(NewComponentDto componentDto, ComponentType ingredientType, int? ingredientId = null)
    {
        var ingredient = new Component
        {
            Cost = componentDto.Cost,
            Name = componentDto.Name,
            ComponentTypeId = (int)ingredientType,
            UseMeasureTypeId = componentDto.UseMeasureType.Id,
            BuyMeasureTypeId = componentDto.BuyMeasureType.Id,
            SuggestedAmount = (int)componentDto.SuggestedAmount,
            BuyAmount = (int)componentDto.BuyAmount,
            BuyPrice = componentDto.BuyPrice
        };

        if (ingredientId.HasValue)
            ingredient.Id = ingredientId.Value;

        if (!string.IsNullOrEmpty(componentDto.ImagePath))
            ingredient.Images.Add(new ComponentImage { ImageUrl = componentDto.ImagePath });

        return ingredient;
    }


    private bool IsLatin(string name)
    {
        if (string.IsNullOrEmpty(name))
            return false;

        char firstChar = name[0];
        return firstChar >= 'A' && firstChar <= 'z';
    }
    
    private static void LogError(string message, params object[] data)
    {
        Debug.WriteLine(message);
    }

    private ComponentModel? GetCachedComponentById(int id)
    {
        if (_cachedComponents.Lookup(id).HasValue)
            return _cachedComponents.Lookup(id).Value;

        Debug.WriteLine($"Component with ID {id} not found in cache.");
        return null;
    }

    private void NotifyResult(bool success, DomainNotificationType successNotification)
    {
        var type = success
            ? successNotification
            : DomainNotificationType.ErrorWhileSaving;

        _notificationService.Notify(type);
    }


    /// IAutoSaveCandidate implementation 
    public Task<bool> SaveIfNeededAsync(CancellationToken cancellationToken = default)
    {
        return SavePreviouslySelectedRecipeAsync(SelectedRecipe, ComponentsByRecipe);
    }

    public bool ShouldSave(CancellationToken cancellationToken = default)
    {
        return ShouldSaveRecipe(SelectedRecipe);
    }
    /// IAutoSaveCandidate implementation END

    private readonly RecipeService _recipeService;
    private readonly ComponentService _componentService;
    private readonly ComponentTypeService _componentTypeService;

    private readonly IDialogService _dialogService;

    private bool _isRecipeAddMode;
    private RecipeModel? _selectedRecipe;
    private string _newImagePath;
    private readonly IUnitCostCalculator _unitCostCalc;
    private bool _suppressSelectionChange;
    private Dictionary<int, ComponentTypeModel> _cachedComponentTypes;
    private readonly MeasureTypeCache _measureTypeCache;
    private readonly INotificationService _notificationService;
    private bool _suppressIsDirty;
    private RecipeModel? _previousSelectedRecipe;
}