using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using SoupAndSoup.Data.Models;
using SoupAndSoup.Data.Services;
using SoupAndSoupApp.ExternalServices;
using SoupAndSoupApp.Helpers.Calculators;
using SoupAndSoupApp.Helpers.Images;
using SoupAndSoupApp.Helpers.Navigation;
using SoupAndSoupApp.Helpers.Mappers;
using SoupAndSoupApp.Helpers.Notifications;
using SoupAndSoupApp.Models;
using DynamicData;
using ComponentType = SoupAndSoupApp.Models.ComponentType;
using CosmeticType = SoupAndSoupApp.Models.CosmeticType;

namespace SoupAndSoupApp.Helpers.Coordinators;

public class ComponentCoordinator
{
    private readonly IComponentService _componentService;
    private readonly IComponentMapper _componentMapper;
    private readonly IDialogService _dialogService;
    private readonly IImageService _imageService;
    private readonly IAzureBlobStorageService _blobStorageService;
    private readonly IUnitCostCalculator _unitCostCalc;
    private readonly INotificationService _notificationService;
    private readonly ILogger<ComponentCoordinator> _logger;

    public ComponentCoordinator(
        IComponentService componentService,
        IComponentMapper componentMapper,
        IDialogService dialogService,
        IImageService imageService,
        IAzureBlobStorageService blobStorageService,
        IUnitCostCalculator unitCostCalc,
        INotificationService notificationService,
        ILogger<ComponentCoordinator> logger)
    {
        _componentService = componentService;
        _componentMapper = componentMapper;
        _dialogService = dialogService;
        _imageService = imageService;
        _blobStorageService = blobStorageService;
        _unitCostCalc = unitCostCalc;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task AddComponentAsync(
        ComponentGroup? group,
        SourceCache<ComponentModel, int> cachedComponents,
        ICommand editCommand, ICommand deleteCommand, ICommand toggleCommand,
        CosmeticType cosmeticType, string noImageUrl)
    {
        if (group is null)
        {
            DesignerActivityHelper.NotifyResult(false, DomainNotificationType.ErrorWhileSaving, _notificationService);
            _logger.LogWarning("No group found for component type");
            return;
        }

        var dto = await _dialogService.ShowAddEditComponentDialogAsync(false, group.ComponentType);
        if (dto is null) return;

        await ExecuteComponentSaveAsync(dto, async () =>
        {
            var dbComponent = _componentMapper.MapToEntity(dto, group.ComponentType.Type, cosmeticType);
            var saved = await SaveNewComponentAsync(dbComponent, group.ComponentType.Type);
            if (saved is null) return false;

            var model = await _componentMapper.MapToModelAsync(saved, editCommand, deleteCommand, toggleCommand, noImageUrl);
            cachedComponents.AddOrUpdate(model);
            _logger.LogInformation("Success for save component with ID {componentId}, Name: {componentName}", model.Id, model.Name);
            return true;
        });
    }

    public async Task EditComponentAsync(
        ComponentModel? model,
        ComponentGroup? group,
        SourceCache<ComponentModel, int> cachedComponents,
        CosmeticType cosmeticType,
        string noImageUrl)
    {
        if (model is null)
        {
            _logger.LogWarning("Attempted to edit a null component model.");
            return;
        }

        if (group is null)
        {
            DesignerActivityHelper.NotifyResult(false, DomainNotificationType.ErrorWhileSaving, _notificationService);
            _logger.LogWarning("No group found for type {componentType}", model.Type);
            return;
        }

        var dto = await _dialogService.ShowAddEditComponentDialogAsync(true, group.ComponentType, model);
        if (dto is null)
        {
            _logger.LogInformation("Component edit dialog cancelled for component ID {componentId}", model.Id);
            return;
        }

        await ExecuteComponentSaveAsync(dto, async () =>
        {
            var updatedComponent = _componentMapper.MapToEntity(dto, group.ComponentType.Type, cosmeticType, model.Id);
            if (!await SaveExistingComponentAsync(updatedComponent, group.ComponentType.Type)) return false;

            await UpdateCachedComponentAsync(updatedComponent, model, cachedComponents, noImageUrl);
            _logger.LogInformation("Success for save component with ID {componentId}", model.Id);
            return true;
        });
    }

    private Task ExecuteComponentSaveAsync(NewComponentDto dto, Func<Task<bool>> persistAndUpdate)
    {
        return DesignerActivityHelper.ExecuteCompensatingTransaction(
            async () =>
            {
                await _imageService.UpdateComponentImageAsync(dto);
                return await persistAndUpdate();
            },
            async () => await _blobStorageService.DeleteBlobAsync(dto.ImagePath),
            _logger);
    }

    public async Task DeleteComponentAsync(
        ComponentModel component,
        SourceCache<ComponentModel, int> cachedComponents,
        RecipeModel? selectedRecipe,
        ComponentGroup? group)
    {
        var result = await _componentService.SoftDeleteAsync(component.Id);
        DesignerActivityHelper.NotifyResult(result, DomainNotificationType.ComponentDeleted, _notificationService);

        if (!result)
        {
            _logger.LogError("Failed to delete component with ID {componentId}, Name: {componentName}", component.Id, component.Name);
            return;
        }

        selectedRecipe?.SelectedComponents.Remove(component.Id);
        group?.Components.Remove(component);
        cachedComponents.Remove(component.Id);

        _logger.LogInformation("Success for delete component with ID {componentId}, Name: {componentName}", component.Id, component.Name);

        await _imageService.DeleteImageAsync(component);
    }

    public decimal CalculateUnitCost(SourceCache<ComponentModel, int> cachedComponents)
    {
        var selectedComponents = cachedComponents.Items
            .Where(c => c.IsInCurrentRecipe)
            .ToList();

        return selectedComponents.Any()
            ? _unitCostCalc.CalculateUnitCost(selectedComponents)
            : 0;
    }

    private async Task<Component?> SaveNewComponentAsync(Component component, ComponentType type)
    {
        var result = await _componentService.CreateAsync(component);
        var isSuccess = result is not null && result.Id > 0;
        DesignerActivityHelper.NotifyResult(isSuccess, DomainNotificationType.ComponentCreated, _notificationService);

        if (isSuccess) return result;

        _logger.LogError("Failed to save new component {componentName} of type {componentType}", component.Name, type);
        return result;
    }

    private async Task<bool> SaveExistingComponentAsync(Component component, ComponentType type)
    {
        var result = await _componentService.UpdateAsync(component);
        DesignerActivityHelper.NotifyResult(result, DomainNotificationType.ComponentUpdated, _notificationService);
        if (result) return true;

        _logger.LogError("Failed to save component: componentId {componentId}, {componentName} of type {componentType}", component.Id, component.Name, type);
        return false;
    }

    public Task<ComponentModel> MapComponentModelAsync(
        Component component, ICommand editCommand, ICommand deleteCommand, ICommand toggleCommand, string noImageUrl)
    {
        return _componentMapper.MapToModelAsync(component, editCommand, deleteCommand, toggleCommand, noImageUrl);
    }

    private async Task UpdateCachedComponentAsync(
        Component updatedComponent,
        ComponentModel existingModel,
        SourceCache<ComponentModel, int> cachedComponents,
        string noImageUrl)
    {
        if (!cachedComponents.Lookup(updatedComponent.Id).HasValue) return;

        var remapped = await _componentMapper.MapToModelAsync(
            updatedComponent,
            existingModel.EditCommand,
            existingModel.DeleteCommand,
            existingModel.ToggleInRecipeCommand,
            noImageUrl);

        cachedComponents.AddOrUpdate(remapped);
    }
}
