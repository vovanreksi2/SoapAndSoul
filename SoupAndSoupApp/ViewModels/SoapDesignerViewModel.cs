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
using Microsoft.Extensions.Logging;
using DynamicData;
using DynamicData.Binding;
using FuzzySharp;
using ReactiveUI;
using SoapAndSoul.Infrastructure;
using SoupAndSoup.Data.Models;
using SoupAndSoup.Data.Services;
using SoupAndSoupApp.ExternalServices;
using SoupAndSoupApp.Helpers;
using SoupAndSoupApp.Helpers.Autosave;
using SoupAndSoupApp.Helpers.Calculators;
using SoupAndSoupApp.Helpers.Images;
using SoupAndSoupApp.Helpers.Mappers;
using SoupAndSoupApp.Helpers.Navigation;
using SoupAndSoupApp.Helpers.Notifications;
using SoupAndSoupApp.Helpers.Rules;
using SoupAndSoupApp.Models;
using ComponentType = SoupAndSoupApp.Models.ComponentType;
using CosmeticType = SoupAndSoupApp.Models.CosmeticType;


namespace SoupAndSoupApp.ViewModels;

public class SoapDesignerViewModel : ViewModelBase, IAutoSaveCandidate, IInitializableVM
{
    public const string NoImageRecipe = "Assets/No_Receipt_Photo.png";
    public const string NoImage_Component_Image = "Assets/No_Component_Photo.png";

    private static readonly TimeSpan SearchDebounceDelay = TimeSpan.FromMilliseconds(300);
    private const int FuzzyMatchThreshold = 60;

    public ICommand NewRecipeCommand { get; private set; }
    public ReactiveCommand<RecipeModel, Unit> DeleteRecipeCommand { get; private set; }

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

    private readonly SourceCache<RecipeModel, int> _cachedRecipes = new(recipe => recipe.Id);

    private ReadOnlyObservableCollection<RecipeModel> _recipes = ReadOnlyObservableCollection<RecipeModel>.Empty;
    public ReadOnlyObservableCollection<RecipeModel> Recipes => _recipes;

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

    
    private readonly SourceCache<ComponentModel, int> _cachedComponents = new(component => component.Id);


    private ReadOnlyObservableCollection<ComponentModel> _componentsByRecipe = ReadOnlyObservableCollection<ComponentModel>.Empty;
    public ReadOnlyObservableCollection<ComponentModel> ComponentsByRecipe => _componentsByRecipe;


    private ReadOnlyObservableCollection<ComponentGroup> _componentGroups = ReadOnlyObservableCollection<ComponentGroup>.Empty;
    public ReadOnlyObservableCollection<ComponentGroup> ComponentGroups => _componentGroups;


    public bool IsDirty { get; set; }

    public string SearchString
    {
        get => _searchString;
        set
        {
            if (_searchString == value) return;
            this.RaiseAndSetIfChanged(ref _searchString, value.Trim());
        }
    }
    
    public SoapDesignerViewModel( )
    {
        InitView();
    }

    public SoapDesignerViewModel(
        IRecipeService recipeService,
        IComponentService componentService,
        IComponentTypeService componentTypeService,
        IDialogService dialogService, IUnitCostCalculator unitCostCalc,
        INotificationService notificationService, IAzureBlobStorageService blobStorageService,
        ILogger<SoapDesignerViewModel> logger,
        InstrumentationOpenTelemetry instrumentation,
        IImageService imageService,
        IRecipeMapper recipeMapper,
        IComponentMapper componentMapper,
        IComponentSelectionRuleEngine ruleEngine)
    {
        _recipeService = recipeService;
        _componentService = componentService;
        _componentTypeService = componentTypeService;

        _dialogService = dialogService;
        _unitCostCalc = unitCostCalc;
        _notificationService = notificationService;
        _blobStorageService = blobStorageService;
        _logger = logger;
        _activitySource = instrumentation.ActivitySource;
        _imageService = imageService;
        _recipeMapper = recipeMapper;
        _componentMapper = componentMapper;
        _ruleEngine = ruleEngine;

        InitView();
    }


    private void InitView()
    {
        NewRecipeCommand = ReactiveCommand.Create(CreateNewRecipePlaceholder);
        DeleteRecipeCommand = ReactiveCommand.CreateFromTask<RecipeModel>(DeleteRecipeAsync);

        NewComponentCommand = ReactiveCommand.CreateFromTask<ComponentType>(AddComponentAsync);
        EditComponentCommand = ReactiveCommand.CreateFromTask<ComponentModel>(EditComponentAsync);
        DeleteComponentCommand = ReactiveCommand.CreateFromTask<ComponentModel>(DeleteComponentAsync, Observable.Return(true));

        SetupRx();
    }

    private void SetupRx()
    {
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

            _cachedRecipes.Connect()
                .Filter(FilterRecipes)
                .Sort(SortExpressionComparer<RecipeModel>.Ascending(r => r.Name))
                .Bind(out _recipes)
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

            this.WhenAnyValue(x => x.SearchString)
                .Throttle(SearchDebounceDelay)
                .DistinctUntilChanged()
                .Subscribe(_ => _cachedRecipes.Refresh())
                .DisposeWith(Disposables);

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
            _suppressIsDirty = true;

            await Task.WhenAll(
                LoadComponentTypesAsync(),
                LoadComponentsAsync(),
                LoadRecipesAsync());

            if (Recipes.Any())
                SelectedRecipe = Recipes.FirstOrDefault();
            else
                CreateNewRecipePlaceholder();

        }
        catch (Exception e)
        {
            HandleInitException(e, activity);
            _notificationService.Notify(DomainNotificationType.ErrorDuringInit);
        }
        finally
        {
            _suppressIsDirty = false;
            _isInitialized = true;
            _logger.LogInformation("InitializeAsync: Completed for {CosmeticType}", _currentCosmeticType);
        }
    }

    private Task LoadComponentTypesAsync()
    {
         return RunWithActivity(
            nameof(LoadComponentTypesAsync),
            async () =>
            {
                var componentTypes = await _componentTypeService.GetAllAsync((int)_currentCosmeticType);

                _cachedComponentTypes = componentTypes.ToDictionary(
                    type => (ComponentType)type.Id,
                    type => new ComponentTypeModel(type));

                foreach (var type in _cachedComponentTypes)
                {
                    _cachedComponents.AddOrUpdate(CreateButtonComponent(type.Key));
                }
            },
            ("componentTypes.count", _cachedComponents.Count), ("cosmetic.type", _currentCosmeticType));
    }
    private Task LoadComponentsAsync( )
    {
        return RunWithActivity(
            nameof(LoadComponentsAsync),
            async () =>
            {
                var components = await _componentService.GetAllAsync((int)_currentCosmeticType);

                foreach (var mappedComponent in await Task.WhenAll(components.Select(MapComponentModelAsync)))
                {
                    _cachedComponents.AddOrUpdate(mappedComponent);
                }
            },
            ("components.count", _cachedComponents.Count),
            ("cosmetic.type", _currentCosmeticType));
    }
    private Task LoadRecipesAsync()
    {
        return RunWithActivity(
            nameof(LoadRecipesAsync),
            async () =>
            {
                var recipes = await _recipeService.GetAllAsync((int)_currentCosmeticType);
                var recipeModels = await Task.WhenAll(recipes.Select(MapRecipe));

                foreach (var recipeModel in recipeModels)
                    _cachedRecipes.AddOrUpdate(recipeModel);

            },
            ("cosmetic.type", _currentCosmeticType));
    }

    private async Task RunWithActivity(
        string activityName,
        Func<Task> action,
        params (string Key, object Value)[] tags)
    {
        using var activity = _activitySource.StartActivity(activityName);

        try
        {
            await action();

            _logger.LogInformation("Loaded  items for {CosmeticType}", _currentCosmeticType);

            foreach (var (key, value) in tags)
            {
                activity?.SetTag(key, value);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in {ActivityName}", activityName);

            activity.SetTag("error", true);

            var exceptionEvent = new ActivityEvent(
                "exception",
                tags: new ActivityTagsCollection
                {
                    { "exception.type", ex.GetType().FullName },
                    { "exception.message", ex.Message },
                    { "exception.stacktrace", ex.StackTrace }
                });

            activity.AddEvent(exceptionEvent);
            throw;
        }
    }
     
    private ComponentModel CreateButtonComponent(ComponentType type)
    {
        return new ComponentModel
        {
            Id = -(int)type, // Negative ID indicates button component
            IsSelected = false,
            IsButton = true,
            Type = type,
        };
    }
    private void CreateNewRecipePlaceholder()
    {
        var newRecipe = new RecipeModel();

        newRecipe.BeginInit();

        newRecipe.Name = "Нова Рецептура";
        newRecipe.Description = string.Empty;
        newRecipe.ImagePath = ImageHelper.LoadFromResource(NoImageRecipe);
        newRecipe.DeleteRecipeCommand = DeleteRecipeCommand;

        newRecipe.EndInit();

        _cachedRecipes.AddOrUpdate(newRecipe);
        IsRecipeAddMode = true;

        SelectedRecipe = newRecipe;
    }
        
    private void HandleInitException(Exception e, Activity? activity)
    {
        _logger.LogError(e, "Exception during initialization");
        if (activity is null) return;

        activity.SetStatus(ActivityStatusCode.Error);

        activity.SetTag("error", true);

        var exceptionEvent = new ActivityEvent(
            "exception",
            tags: new ActivityTagsCollection
            {
                { "exception.type", e.GetType().FullName },
                { "exception.message", e.Message },
                { "exception.stacktrace", e.StackTrace }
            });

        activity.AddEvent(exceptionEvent);
    }

    private async Task DeleteRecipeAsync(RecipeModel recipeModel)
    {
        if (!recipeModel.IsNew)
        {
            var isDeleteSuccess = await _recipeService.SoftDelete(recipeModel.Id);
            NotifyResult(isDeleteSuccess, DomainNotificationType.RecipeDeleted);

            if (!isDeleteSuccess)
            {
                _logger.LogError("Failed to delete recipe with ID {recipeId}, Name: {recipeName}", recipeModel.Id, recipeModel.Name);
                return;
            }

            await DeleteImageAsync(recipeModel);
            _logger.LogInformation("Success for delete recipe with ID {recipeId}, Name: {recipeName}", recipeModel.Id, recipeModel.Name);
        }

        _cachedRecipes.Remove(recipeModel.Id);
        SelectedRecipe = Recipes.Count > 0 ? Recipes.FirstOrDefault() : null;
    }


    private async Task AddComponentAsync(ComponentType parameter)
    {
        var group = ComponentGroups.FirstOrDefault(_ => _.ComponentType.Type == parameter);
        if (group is null)
        {
            NotifyResult(false, DomainNotificationType.ErrorWhileSaving);
            _logger.LogWarning("No group found for type {componentType}", parameter);
            return;
        }

        var newComponentDto = await _dialogService.ShowAddEditComponentDialogAsync(false, group.ComponentType);
        if (newComponentDto is null) return;

        await ExecuteCompensatingTransaction(async () =>
        {
            await UpdateComponentImageAsync(newComponentDto);
            var dbComponent = MapComponent(newComponentDto, group.ComponentType.Type);

            var newlySavedComponent  = await SaveNewComponentAsync(dbComponent, group.ComponentType.Type);
            if (newlySavedComponent is null) return false;

            var component = await MapComponentModelAsync(newlySavedComponent);
            _cachedComponents.AddOrUpdate(component);

            _logger.LogInformation("Success for save component with ID {componentId}, Name: {componentName}", component.Id, component.Name);

            return true;
        },
        async () => await _blobStorageService.DeleteBlobAsync(newComponentDto.ImagePath));
    }
    private async Task EditComponentAsync(ComponentModel? model)
    {
        if (model is null)
        {
            _logger.LogWarning("Attempted to edit a null component model.");
            return;
        }

        var group = ComponentGroups.FirstOrDefault(g => g.ComponentType.Type == model.Type);
        if (group is null)
        {
            NotifyResult(false, DomainNotificationType.ErrorWhileSaving);
            _logger.LogWarning("No group found for type {componentType}", model.Type);
            return;
        }

        var editedComponentDto =
            await _dialogService.ShowAddEditComponentDialogAsync(true, group.ComponentType, model);
        if (editedComponentDto is null)
        {
            _logger.LogInformation("Component edit dialog cancelled for component ID {componentId}", model.Id);
            return; // User cancelled the dialog
        }

        await ExecuteCompensatingTransaction(async () =>
            {
                await UpdateComponentImageAsync(editedComponentDto);
                var updatedComponent = MapComponent(editedComponentDto, group.ComponentType.Type, model.Id);

                var isSuccessResult = await SaveExistingComponentAsync(updatedComponent, group.ComponentType.Type);
                if (!isSuccessResult) return false;
          
                // Update the cached component with the new values
                await UpdateCachedComponentAsync(updatedComponent);

                _logger.LogInformation("Success for save component with ID {componentId}", model.Id);
                return true;
            },
            async () => await _blobStorageService.DeleteBlobAsync(editedComponentDto.ImagePath));
    }
    private async Task DeleteComponentAsync(ComponentModel component)
    {
        var result = await _componentService.SoftDeleteAsync(component.Id);
        NotifyResult(result, DomainNotificationType.ComponentDeleted);
        
        if (!result)
        {
            _logger.LogError("Failed to delete component with ID {componentId}, Name: {componentName}", component.Id, component.Name);
            return;
        }

        ComponentGroups.FirstOrDefault(_ => _.ComponentType.Type == component.Type)?.Components.Remove(component);
        _cachedComponents.Remove(component.Id);
        
        _logger.LogInformation("Success for delete component with ID {componentId}, Name: {componentName}", component.Id, component.Name);

        await DeleteImageAsync(component);
    }


    private async Task<Component?> SaveNewComponentAsync(Component component, ComponentType type)
    {
        var result = await _componentService.CreateAsync(component);
        var isSuccess = result is not null && result.Id > 0;
        NotifyResult(isSuccess, DomainNotificationType.ComponentCreated);

        if (isSuccess) return result;

        _logger.LogError("Failed to save new component {componentName} of type {componentType}", component.Name, type);
        return result;
    }    

    private async Task<bool> SaveExistingComponentAsync(Component component, ComponentType type)
    {
        var result = await _componentService.UpdateAsync(component);
        NotifyResult(result, DomainNotificationType.ComponentUpdated);
        if (result) return true;

        _logger.LogError("Failed to save component: componentId {componentId}, {componentName} of type {componentType}", component.Id, component.Name, type);
        return false;
    }
    private async Task UpdateCachedComponentAsync(Component updatedComponent)
    {
        var cachedComponent = GetCachedComponentById(updatedComponent.Id);
        if (cachedComponent == null) return;

        // Consider implementing a CopyFrom method on ComponentModel for maintainability
        cachedComponent.Name = updatedComponent.Name;
        cachedComponent.BuyAmount = updatedComponent.BuyAmount;
        cachedComponent.Cost = updatedComponent.Cost;
        cachedComponent.BuyPrice = updatedComponent.BuyPrice;
        cachedComponent.UseMeasureTypeId = updatedComponent.UseMeasureTypeId;
        cachedComponent.BuyMeasureTypeId = updatedComponent.BuyMeasureTypeId;
        cachedComponent.SuggestedAmount = updatedComponent.SuggestedAmount;
        cachedComponent.Type = (ComponentType)updatedComponent.ComponentTypeId;

        cachedComponent.ImagePath = await _imageService.LoadImageOrDefaultAsync(updatedComponent, NoImage_Component_Image);
    }


    private async Task HandleChangeSelectedRecipeAsync(RecipeModel? newValue, RecipeModel? oldValue, IEnumerable<ComponentModel> componentsByRecipe)
    {
        var isSaveSuccess = await SavePreviouslySelectedRecipeAsync(oldValue, componentsByRecipe);
        if (!isSaveSuccess)
        {
            _logger.LogError("Failed to save previously selected recipe, aborting selection change.");
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
            _logger.LogWarning("New value is null, cannot update components.");
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
        if (oldRecipe == null)
        {
            _logger.LogInformation("Old recipe is null, nothing to save.");
            return true; // Nothing to save
        }

        if (!ShouldSaveRecipe(oldRecipe)) return true;

        var isSuccessTransaction = await ExecuteCompensatingTransaction(async () =>
            {
                await UpdateRecipeImageAsync(oldRecipe);

                var isSaveSuccess = await AddOrUpdateRecipeAsync(oldRecipe);
                if (!isSaveSuccess) return false;

                ResetDirtyFlags(oldRecipe);

                UpdateRecipeComponents(oldRecipe);
                return true;
            },
            async () => await _blobStorageService.DeleteBlobAsync(oldRecipe.ImagePathString));
        
        return isSuccessTransaction;


        void ResetDirtyFlags(RecipeModel recipe)
        {
            recipe.IsDirty = false;
            IsDirty = false;
        }

        void UpdateRecipeComponents(RecipeModel recipe)
        {
            recipe.RecipeComponents = componentsByRecipe.Select(_recipeMapper.MapToComponentByRecipeModel).ToList();
        }
    }
    private bool ShouldSaveRecipe(RecipeModel? recipe)
    {
        return recipe is not null && (IsDirty || recipe.IsDirty);
    }

    private async Task<bool> AddOrUpdateRecipeAsync(RecipeModel? inputRecipe)
    {
        if (inputRecipe == null)
        {
            _logger.LogWarning("Input recipe is null, cannot save.");
            return false;
        }

        var recipe = MapRecipe(inputRecipe);
        if (inputRecipe.IsNew)
        {
            var saveResult = await SaveCreatedRecipe(recipe);

            NotifyResult(saveResult, DomainNotificationType.RecipeCreated);
            return saveResult;
        }

        var updateResult = await SaveUpdatedRecipe(recipe);

        NotifyResult(updateResult, DomainNotificationType.RecipeUpdated);
        _logger.LogInformation("Success for save recipe with ID {recipeId}", inputRecipe.Id);

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
            _logger.LogError(exception, "Failed to create new recipe with NAME {recipeName}", recipe.Name);
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
            _logger.LogError(exception, "Failed to update recipe with NAME {recipeName} and ID {recipeId}", recipe.Name, recipe.Id);
            return false;
        }
    }


    private void UpdateComponentInGroupAccordingToRules(ComponentModel componentModel)
    {
        _ruleEngine.ApplySelectionRules(componentModel, [.. ComponentsByRecipe]);
    }

    private void ReCalculateUnitCost(IEnumerable<ComponentModel> components)
    {
        if (SelectedRecipe == null)
        {
            _logger.LogWarning("Selected recipe is null, cannot calculate unit cost.");
            return;
        }

        if (!components.Any())
        {
            _logger.LogWarning("No components found to calculate unit cost.");

            SelectedRecipe.UnitCost = 0;
            return;
        }

        SelectedRecipe.UnitCost = _unitCostCalc.CalculateUnitCost(components);
    }


    private async Task<bool> ExecuteCompensatingTransaction(Func<Task<bool>> executeTask, Func<Task<bool>> rollbackTask)
    {
        try
        {
             return await executeTask();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed: {message}", ex.Message);

            try
            {
                var result = await rollbackTask();
                if (!result)
                    _logger.LogError(ex, "Rollback transaction is failed: {message}", ex.Message);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Rollback transaction is failed: {message}", e.Message);
            }

            return false;
        }
    }
    private Task UpdateComponentImageAsync(NewComponentDto newComponentDto)
    {
        return _imageService.UpdateComponentImageAsync(newComponentDto);
    }

    private Task UpdateRecipeImageAsync(RecipeModel recipe)
    {
        return _imageService.UpdateRecipeImageAsync(recipe, NewImagePath);
    }

    private Task DeleteImageAsync(BaseModel baseModel)
    {
        return _imageService.DeleteImageAsync(baseModel);
    }


    private Task<RecipeModel> MapRecipe(Recipe recipe)
    {
        return _recipeMapper.MapToModelAsync(recipe, DeleteRecipeCommand, _cachedComponents, NoImageRecipe);
    }

    private Recipe MapRecipe(RecipeModel recipe)
    {
        return _recipeMapper.MapToEntity(recipe, ComponentsByRecipe, _currentCosmeticType, NoImageRecipe);
    }


    private async Task<ComponentModel> MapComponentModelAsync(Component component)
    {
        var result = await _componentMapper.MapToModelAsync(
            component, EditComponentCommand, DeleteComponentCommand, NoImage_Component_Image);

        SubscribeOnComponentChanges(result);
        return result;
    }

    private Component MapComponent(NewComponentDto componentDto, ComponentType ingredientType, int? ingredientId = null)
    {
        return _componentMapper.MapToEntity(componentDto, ingredientType, _currentCosmeticType, ingredientId);
    }


    private void SubscribeOnComponentChanges(ComponentModel component)
    {
        component
            .WhenAnyValue(x => x.IsSelected)
            .Where(_ => !_suppressSelectionChange)
            .Skip(1)
            .Subscribe(_ => { HandleSelectedComponentChanged(component); })
            .DisposeWith(Disposables);

        component
            .WhenAnyValue(x => x.AmountInRecipe)
            .Skip(1)
            .Subscribe(_ => { HandleAmountComponentChanged(component); })
            .DisposeWith(Disposables);
    }

    private bool IsLatin(string name)
    {
        if (string.IsNullOrEmpty(name))
            return false;

        char firstChar = name[0];
        return firstChar >= 'A' && firstChar <= 'z';
    }
    
    private ComponentModel? GetCachedComponentById(int id)
    {
        var lookup = _cachedComponents.Lookup(id);
        if (lookup.HasValue)
            return lookup.Value;

        _logger.LogWarning("Component with ID {componentId} not found in cache", id);
        return null;
    }

    private void NotifyResult(bool success, DomainNotificationType successNotification)
    {
        var type = success
            ? successNotification
            : DomainNotificationType.ErrorWhileSaving;

        _notificationService.Notify(type);
    }

    public void SetType(CosmeticType mode)
    {
        _currentCosmeticType = mode;
    }

   
    private bool FilterRecipes(RecipeModel recipe)
    {
        var searchString = SearchString?.Trim()?.ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(searchString))
            return true;

        var contains = recipe.Name.Contains(searchString, StringComparison.OrdinalIgnoreCase) ||
                       recipe.Description.Contains(searchString, StringComparison.OrdinalIgnoreCase);

        var fuzzyScore = Fuzz.PartialRatio(searchString, recipe.Name.ToLowerInvariant()) > FuzzyMatchThreshold ||
                         Fuzz.PartialRatio(searchString, recipe.Description.ToLowerInvariant()) > FuzzyMatchThreshold;

        return contains || fuzzyScore;
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

    private readonly IRecipeService _recipeService;
    private readonly IComponentService _componentService;
    private readonly IComponentTypeService _componentTypeService;

    private readonly IDialogService _dialogService;

    private bool _isRecipeAddMode;
    private RecipeModel? _selectedRecipe;
    private string _newImagePath;
    private readonly IUnitCostCalculator _unitCostCalc;
    private bool _suppressSelectionChange;
    private Dictionary<ComponentType, ComponentTypeModel> _cachedComponentTypes;
    private readonly INotificationService _notificationService;
    private bool _suppressIsDirty;
    private RecipeModel? _previousSelectedRecipe;
    private readonly ILogger<SoapDesignerViewModel> _logger;
    private readonly ActivitySource _activitySource;

    private readonly IAzureBlobStorageService _blobStorageService;
    private readonly IImageService _imageService;
    private readonly IRecipeMapper _recipeMapper;
    private readonly IComponentMapper _componentMapper;
    private readonly IComponentSelectionRuleEngine _ruleEngine;
    private CosmeticType _currentCosmeticType;
    private bool _isInitialized;
    private string _searchString;
}