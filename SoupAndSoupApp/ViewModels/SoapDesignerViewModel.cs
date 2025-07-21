using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Media.Imaging;
using DynamicData;
using DynamicData.Binding;
using FuzzySharp;
using ReactiveUI;
using SoupAndSoup.Data.Models;
using SoupAndSoup.Data.Services;
using SoupAndSoupApp.Helpers;
using SoupAndSoupApp.Models;
using SoupAndSoupApp.Services;
using ComponentType = SoupAndSoupApp.Models.ComponentType;
using CosmeticType = SoupAndSoupApp.Models.CosmeticType;


namespace SoupAndSoupApp.ViewModels;

public class SoapDesignerViewModel : ViewModelBase, IAutoSaveCandidate, IInitializableVM
{
    public const string NoImage_Receipt = "Assets/No_Receipt_Photo.png";
    public const string NoImage_Component_Image = "Assets/No_Component_Photo.png";

    public ICommand NewReceiptCommand { get; private set; }
    public ReactiveCommand<RecipeModel, Unit> DeleteReceiptCommand { get; private set; }

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

            _cachedRecipes.Connect()
                .AutoRefreshOnObservable(_ => Observable.Return(Unit.Default)); 

        }
    }
    
    public SoapDesignerViewModel( )
    {
        InitView();
    }

    public SoapDesignerViewModel(
        RecipeService recipeService,
        ComponentService componentService,
        ComponentTypeService componentTypeService,
        IDialogService dialogService, IUnitCostCalculator unitCostCalc, MeasureTypeCache measureTypeCache,
        INotificationService notificationService, IAzureBlobStorageService blobStorageService)
    {
        _recipeService = recipeService;
        _componentService = componentService;
        _componentTypeService = componentTypeService;

        _dialogService = dialogService;
        _unitCostCalc = unitCostCalc;
        _measureTypeCache = measureTypeCache;
        _notificationService = notificationService;
        _blobStorageService = blobStorageService;

        InitView();
    }


    private void InitView()
    {
        NewReceiptCommand = ReactiveCommand.Create(NewReceipt);
        DeleteReceiptCommand = ReactiveCommand.CreateFromTask<RecipeModel>(DeleteReceiptAsync);

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
                .Throttle(TimeSpan.FromMilliseconds(300)) // debounce optional
                .DistinctUntilChanged()
                .Subscribe(_ => _cachedRecipes.Refresh())
                .DisposeWith(Disposables);

        } 

    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        try
        {
            _suppressIsDirty = true;

            var componentTypes = await _componentTypeService.GetAllAsync((int)_currentCosmeticType);
            _cachedComponentTypes =
                componentTypes.ToDictionary(type => (ComponentType)type.Id, type => new ComponentTypeModel(type));

            foreach (var type in _cachedComponentTypes)
            {
                _cachedComponents.AddOrUpdate(new ComponentModel
                {
                    Id = -(int)type.Key,
                    IsSelected = false,
                    IsButton = true,
                    Type = type.Key,
                });
            }

            var components = await _componentService.GetAllAsync((int)_currentCosmeticType);
            foreach (var mappedComponent in await Task.WhenAll(components.Select(MapComponentModelAsync)))
            {
                _cachedComponents.AddOrUpdate(mappedComponent);
            }

            var recipes = await _recipeService.GetAllAsync((int)_currentCosmeticType);
            var recipeModels = await Task.WhenAll(recipes.Select(MapRecipe));

            if (recipeModels.Any())
            {
                foreach (var recipeModel in recipeModels)
                {
                    _cachedRecipes.AddOrUpdate(recipeModel);
                }
                SelectedRecipe = Recipes.FirstOrDefault();
            }
            else
                NewReceipt();
  
        }
        catch (Exception e)
        {
            _notificationService.Notify(DomainNotificationType.ErrorDuringInit);
            LogError("!‼️ Exception during initialization", e);
        }
        finally
        {
            _suppressIsDirty = false;
            _isInitialized = true;
        }
    }

    private void NewReceipt()
    {
        var newRecipe = new RecipeModel();

        newRecipe.BeginInit();

        newRecipe.Name = "Нова Рецептура";
        newRecipe.Description = string.Empty;
        newRecipe.ImagePath = ImageHelper.LoadFromResource(NoImage_Receipt);
        newRecipe.DeleteReceiptCommand = DeleteReceiptCommand;

        newRecipe.EndInit();

        _cachedRecipes.AddOrUpdate(newRecipe);
        IsRecipeAddMode = true;

        SelectedRecipe = newRecipe;
    }

    private async Task DeleteReceiptAsync(RecipeModel recipeModel)
    {
        if (!recipeModel.IsNew)
        {
            var isDeleteSuccess = await _recipeService.SoftDelete(recipeModel.Id);
            NotifyResult(isDeleteSuccess, DomainNotificationType.RecipeDeleted);

            if (!isDeleteSuccess)
            {
                LogError("Failed to delete recipe with ID {recipeId}, Name: {recipeName}", recipeModel.Id, recipeModel.Name);
                return;
            }

            await DeleteImageAsync(recipeModel);
            LogError("Success for delete recipe with ID {recipeId}, Name: {recipeName}", recipeModel.Id, recipeModel.Name);
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
            LogError($"No group found for type {parameter}");
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

            LogError("Success for save component with ID {componentId}, Name: {componentName}", component.Id, component.Name);

            return true;
        },
        async () => await _blobStorageService.DeleteBlobAsync(newComponentDto.ImagePath));
    }
    private async Task EditComponentAsync(ComponentModel? model)
    {
        if (model is null)
        {
            LogError("Attempted to edit a null component model.");
            return;
        }

        var group = ComponentGroups.FirstOrDefault(g => g.ComponentType.Type == model.Type);
        if (group is null)
        {
            NotifyResult(false, DomainNotificationType.ErrorWhileSaving);
            LogError($"No group found for type {model.Type}");
            return;
        }

        var editedComponentDto =
            await _dialogService.ShowAddEditComponentDialogAsync(true, group.ComponentType, model);
        if (editedComponentDto is null)
        {
            LogError("Component edit dialog cancelled for component ID {componentId}", model.Id);
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

                LogError($"Success for save component with ID {model.Id}");
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
            LogError("Failed to delete component with ID {componentId}, Name: {componentName}", component.Id, component.Name);
            return;
        }

        ComponentGroups.FirstOrDefault(_ => _.ComponentType.Type == component.Type)?.Components.Remove(component);
        _cachedComponents.Remove(component.Id);
        
        LogError("Success for delete component with ID {componentId}, Name: {componentName}", component.Id, component.Name);

        await DeleteImageAsync(component);
    }


    private async Task<Component?> SaveNewComponentAsync(Component component, ComponentType type)
    {
        var result = await _componentService.CreateAsync(component);
        var isSuccess = result is not null && result.Id > 0;
        NotifyResult(isSuccess, DomainNotificationType.ComponentCreated);

        if (isSuccess) return result;

        LogError("Failed to save new component{componentName} of type {componentType}.", component.Name, type);
        return result;
    }    

    private async Task<bool> SaveExistingComponentAsync(Component component, ComponentType type)
    {
        var result = await _componentService.UpdateAsync(component);
        NotifyResult(result, DomainNotificationType.ComponentUpdated);
        if (result) return true;

        LogError("Failed to save component: componentId {componentId}, {componentName} of type {componentType}.", component.Id, component.Name, type);
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

        cachedComponent.ImagePath = await LoadFromResourceAsync(updatedComponent);
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
        if (oldRecipe == null)
        {
            LogError("Old recipe is null, nothing to save.");
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
            recipe.RecipeComponents = componentsByRecipe.Select(MapRecipeComponentModel).ToList();
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
            LogError("Input recipe is null, cannot save.");
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
        LogError($"Success for save recipe with ID {inputRecipe.Id}");

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
            var tmpList = new List<ComponentModel>(components);
            foreach (var component in tmpList.Where(_ => _.Type == typeComponent && _.Id != componentModel.Id))
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


    private async Task<bool> ExecuteCompensatingTransaction(Func<Task<bool>> executeTask, Func<Task<bool>> rollbackTask)
    {
        try
        {
             return await executeTask();
        }
        catch (Exception ex)
        {
            LogError("Failed: {message}", ex.Message);

            try
            {
                var result = await rollbackTask();
                if (!result)
                    LogError("Rollback transaction is failed: {message}", ex.Message);
            }
            catch (Exception e)
            {
                LogError("Rollback transaction is failed: {message}", ex.Message);
            }

            return false;
        }
    }
    private async Task UpdateComponentImageAsync(NewComponentDto newComponentDto)
    {
        if (!newComponentDto.IsPhotoChanged) return;

        var uploadResult = await UploadComponentImageAsync(newComponentDto.ImagePath);
        if (uploadResult is null)
            LogError("Failed to upload component image by {imagePath} for component {componentName}",
                newComponentDto.ImagePath, newComponentDto.Name);
        else
            newComponentDto.ImagePath = uploadResult;
    } 
    private async Task UpdateRecipeImageAsync(RecipeModel recipe)
    {
        var uploadResult = await UploadComponentImageAsync(NewImagePath);
        if (uploadResult is null)
            LogError("Failed to upload component image by {imagePath} for component {componentName}",
                NewImagePath, recipe.Name);
        else
            recipe.ImagePathString = uploadResult;
    }


    private bool IsValidExtension(string ext) =>
        new[] { ".jpg", ".jpeg", ".png" }.Contains(ext.ToLower());
    private Task<string?> UploadComponentImageAsync(string imagePath)
    {
        if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
        {
            LogError("File not found on local machine. FilePath: {localFilePath}", imagePath);
            return Task.FromResult<string?>(null);
        }
        var ext = Path.GetExtension(imagePath);
        if (!IsValidExtension(ext))
            throw new InvalidOperationException("Unsupported image extension.");

        var photoName = $"{Guid.NewGuid()}{ext}";
        return UploadImageToBlobAsync(photoName, imagePath);
    }
    private async Task<string?> UploadImageToBlobAsync(string blobName, string localImagePath)
    {
        if (string.IsNullOrEmpty(localImagePath) || !File.Exists(localImagePath))
        {
            LogError("File not found on local machine. FilePath: {localFilePath}", localImagePath);
            return null;
        }

        await using var stream = File.OpenRead(localImagePath);
        var success = await _blobStorageService.UploadBlobAsync(blobName, stream);

        return success ? blobName : null;
    }
    private async Task<Bitmap?> DownloadImageFromBlobAsync(string blobName)
    {
        if (string.IsNullOrEmpty(blobName)) return null;
        var stream = await _blobStorageService.DownloadBlobAsync(blobName);
        
        return stream == null ? null : GetBitmapFromStream(stream);
    }
    private async Task DeleteImageAsync(BaseModel baseModel)
    {
        if (string.IsNullOrEmpty(baseModel.ImagePathString))
        {
            LogError("Component with ID {componentId} has no image to delete. Name: {componentName}", baseModel.Id, baseModel.Name);
            return;
        }

        var deleteResult = await _blobStorageService.DeleteBlobAsync(baseModel.ImagePathString);
        LogError(deleteResult
                ? "Image deleted successfully for component with ID {componentId}, Name: {componentName}"
                : "Failed to delete image for component with ID {componentId}, Name: {componentName}",
            baseModel.Id, baseModel.Name);
    }


    private async Task<RecipeModel> MapRecipe(Recipe recipe)
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

        result.ImagePathString = recipe.Images.FirstOrDefault()?.ImageUrl ?? string.Empty;
        result.ImagePath = await LoadFromResourceAsync(result, NoImage_Receipt);

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
            CosmeticTypeId = (int)_currentCosmeticType,
            Description = recipe.Description,
            RecipeComponents = ComponentsByRecipe.Select(MapRecipeComponent).ToList(),
        };

        if (!string.IsNullOrEmpty(recipe.ImagePathString) && recipe.ImagePathString != NoImage_Receipt)
            result.Images = new List<RecipeImage> { new() { ImageUrl = recipe.ImagePathString } };

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


    private async Task<ComponentModel> MapComponentModelAsync(Component component)
    {
        var componentType = (ComponentType)component.ComponentTypeId;

        var result = new ComponentModel
        {
            Id = component.Id,
            Name = component.Name,
            Cost = component.Cost,

            SuggestedAmount = component.SuggestedAmount,
            BuyPrice = component.BuyPrice,
            BuyAmount = component.BuyAmount,

            DeleteCommand = DeleteComponentCommand,
            EditCommand = EditComponentCommand,
            ShowAmountInButton = componentType != ComponentType.Form,
            Type = componentType,
            
            BuyMeasureTypeId = component.BuyMeasureTypeId,
            UseMeasureTypeId = component.UseMeasureTypeId,
            BuyMeasureTypeShortTitle = (await _measureTypeCache.GetOrAddAsync(component.BuyMeasureTypeId)).ShortTitle,
            UseMeasureTypeShortTitle = (await _measureTypeCache.GetOrAddAsync(component.UseMeasureTypeId)).ShortTitle,
            
            ImagePathString = component.Images.FirstOrDefault()?.ImageUrl,
        };

        result.ImagePath = await LoadFromResourceAsync(result, NoImage_Component_Image);

        SubscribeOnComponentChanges(result);
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
            BuyPrice = componentDto.BuyPrice,
            CosmeticTypeId = (int)_currentCosmeticType
        };

        if (ingredientId.HasValue)
            ingredient.Id = ingredientId.Value;

        if (!string.IsNullOrEmpty(componentDto.ImagePath))
            ingredient.Images.Add(new ComponentImage { ImageUrl = componentDto.ImagePath });

        return ingredient;
    }


    private Task<Bitmap> LoadFromResourceAsync(Component component)
    {
        var imageIsExisting = component.Images.Any();
        if (!imageIsExisting)
            return Task.FromResult(ImageHelper.LoadFromResource(NoImage_Component_Image));

        var imageUrl = component.Images.FirstOrDefault()?.ImageUrl;
        return LoadFromResourceAsync(new BaseModel
        {
            Id = component.Id,
            Name = component.Name,
            ImagePathString = imageUrl ?? string.Empty
        }, NoImage_Component_Image);
    }
    private async Task<Bitmap> LoadFromResourceAsync(BaseModel baseModel, string defaultImageUrl)
    {
        var imageUrl = baseModel.ImagePathString;
        if (string.IsNullOrEmpty(imageUrl))
        {
            LogError("Image URL is null or empty for component ID {componentId}. Using default image.", baseModel.Id);
            return ImageHelper.LoadFromResource(defaultImageUrl);
        }

        var imageBitmap = await DownloadImageFromBlobAsync(imageUrl);
        if (imageBitmap is not null) return imageBitmap;

        LogError("Could not found or download component ID {componentId}. Using default image.", baseModel.Id);
        return ImageHelper.LoadFromResource(defaultImageUrl);
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

    public Bitmap GetBitmapFromStream(Stream stream)
    {
        // The stream must be readable and positioned at the beginning.
        if (stream == null || !stream.CanRead)
        {
            throw new ArgumentException("Stream is not valid or readable.");
        }

        // Ensure the stream is at the beginning.
        // This is crucial if the stream was read from previously.
        if (stream.CanSeek)
        {
            stream.Position = 0;
        }

        // The most common "gotcha":
        // The Bitmap object keeps a lock on the stream for its entire lifetime.
        // If you dispose of the stream before you are done with the bitmap,
        // you will get a "GDI+ generic error".
        // Therefore, you should NOT wrap the stream in a `using` statement here
        // if you intend to return the Bitmap.
        // The caller who receives the Bitmap is responsible for its disposal,
        // which in turn will release the stream.

        // A safer way is to copy the stream to a MemoryStream, which you can keep alive.
        var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        memoryStream.Position = 0; // Rewind the memory stream.

        // Now, create the bitmap from the memory stream.
        // The original stream can now be safely closed if needed.
        return new Bitmap(memoryStream);
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

        var fuzzyScore = Fuzz.PartialRatio(searchString, recipe.Name.ToLowerInvariant()) > 60 ||
                         Fuzz.PartialRatio(searchString, recipe.Description.ToLowerInvariant()) > 60;

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

    private readonly RecipeService _recipeService;
    private readonly ComponentService _componentService;
    private readonly ComponentTypeService _componentTypeService;

    private readonly IDialogService _dialogService;

    private bool _isRecipeAddMode;
    private RecipeModel? _selectedRecipe;
    private string _newImagePath;
    private readonly IUnitCostCalculator _unitCostCalc;
    private bool _suppressSelectionChange;
    private Dictionary<ComponentType, ComponentTypeModel> _cachedComponentTypes;
    private readonly MeasureTypeCache _measureTypeCache;
    private readonly INotificationService _notificationService;
    private bool _suppressIsDirty;
    private RecipeModel? _previousSelectedRecipe;
    private readonly IAzureBlobStorageService _blobStorageService;
    private CosmeticType _currentCosmeticType;
    private bool _isInitialized;
    private string _searchString;
}