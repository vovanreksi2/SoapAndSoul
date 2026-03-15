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

public class ComponentCoordinator(
    IComponentService componentService,
    IComponentMapper componentMapper,
    IDialogService dialogService,
    IImageService imageService,
    IAzureBlobStorageService blobStorageService,
    IUnitCostCalculator unitCostCalc,
    INotificationService notificationService,
    ILogger<ComponentCoordinator> logger)
{
    public async Task AddComponentAsync(
        ComponentGroup? group,
        SourceCache<ComponentModel, int> cachedComponents,
        ICommand editCommand, ICommand deleteCommand, ICommand toggleCommand,
        CosmeticType cosmeticType, string noImageUrl)
    {
        if (group is null)
        {
            DesignerActivityHelper.NotifyResult(false, DomainNotificationType.ErrorWhileSaving, notificationService);
            logger.LogWarning("No group found for component type");
            return;
        }

        var dto = await dialogService.ShowAddEditComponentDialogAsync(false, group.ComponentType);
        if (dto is null) return;

        await ExecuteComponentSaveAsync(dto, async () =>
        {
            var dbComponent = componentMapper.MapToEntity(dto, group.ComponentType.Type, cosmeticType);
            var saved = await SaveNewComponentAsync(dbComponent, group.ComponentType.Type);
            if (saved is null) return false;

            var model = await componentMapper.MapToModelAsync(saved, editCommand, deleteCommand, toggleCommand, noImageUrl);
            cachedComponents.AddOrUpdate(model);
            logger.LogInformation("Success for save component with ID {componentId}, Name: {componentName}", model.Id, model.Name);
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
            logger.LogWarning("Attempted to edit a null component model.");
            return;
        }

        if (group is null)
        {
            DesignerActivityHelper.NotifyResult(false, DomainNotificationType.ErrorWhileSaving, notificationService);
            logger.LogWarning("No group found for type {componentType}", model.Type);
            return;
        }

        var dto = await dialogService.ShowAddEditComponentDialogAsync(true, group.ComponentType, model);
        if (dto is null)
        {
            logger.LogInformation("Component edit dialog cancelled for component ID {componentId}", model.Id);
            return;
        }

        await ExecuteComponentSaveAsync(dto, async () =>
        {
            var updatedComponent = componentMapper.MapToEntity(dto, group.ComponentType.Type, cosmeticType, model.Id);
            if (!await SaveExistingComponentAsync(updatedComponent, group.ComponentType.Type)) return false;

            await UpdateCachedComponentAsync(updatedComponent, model, cachedComponents, noImageUrl);
            logger.LogInformation("Success for save component with ID {componentId}", model.Id);
            return true;
        });
    }

    private Task ExecuteComponentSaveAsync(NewComponentDto dto, Func<Task<bool>> persistAndUpdate)
    {
        return DesignerActivityHelper.ExecuteCompensatingTransaction(
            async () =>
            {
                await imageService.UpdateComponentImageAsync(dto);
                return await persistAndUpdate();
            },
            async () => await blobStorageService.DeleteBlobAsync(dto.ImagePath),
            logger);
    }

    public async Task DeleteComponentAsync(
        ComponentModel component,
        SourceCache<ComponentModel, int> cachedComponents,
        RecipeModel? selectedRecipe,
        ComponentGroup? group)
    {
        var result = await componentService.SoftDeleteAsync(component.Id);
        DesignerActivityHelper.NotifyResult(result, DomainNotificationType.ComponentDeleted, notificationService);

        if (!result)
        {
            logger.LogError("Failed to delete component with ID {componentId}, Name: {componentName}", component.Id, component.Name);
            return;
        }

        selectedRecipe?.SelectedComponents.Remove(component.Id);
        group?.Components.Remove(component);
        cachedComponents.Remove(component.Id);

        logger.LogInformation("Success for delete component with ID {componentId}, Name: {componentName}", component.Id, component.Name);

        await imageService.DeleteImageAsync(component);
    }

    public decimal CalculateUnitCost(SourceCache<ComponentModel, int> cachedComponents)
    {
        var selectedComponents = cachedComponents.Items
            .Where(c => c.IsInCurrentRecipe)
            .ToList();

        return selectedComponents.Any()
            ? unitCostCalc.CalculateUnitCost(selectedComponents)
            : 0;
    }

    private async Task<Component?> SaveNewComponentAsync(Component component, ComponentType type)
    {
        var result = await componentService.CreateAsync(component);
        var isSuccess = result is not null && result.Id > 0;
        DesignerActivityHelper.NotifyResult(isSuccess, DomainNotificationType.ComponentCreated, notificationService);

        if (isSuccess) return result;

        logger.LogError("Failed to save new component {componentName} of type {componentType}", component.Name, type);
        return result;
    }

    private async Task<bool> SaveExistingComponentAsync(Component component, ComponentType type)
    {
        var result = await componentService.UpdateAsync(component);
        DesignerActivityHelper.NotifyResult(result, DomainNotificationType.ComponentUpdated, notificationService);
        if (result) return true;

        logger.LogError("Failed to save component: componentId {componentId}, {componentName} of type {componentType}", component.Id, component.Name, type);
        return false;
    }

    public Task<ComponentModel> MapComponentModelAsync(
        Component component, ICommand editCommand, ICommand deleteCommand, ICommand toggleCommand, string noImageUrl)
    {
        return componentMapper.MapToModelAsync(component, editCommand, deleteCommand, toggleCommand, noImageUrl);
    }

    private async Task UpdateCachedComponentAsync(
        Component updatedComponent,
        ComponentModel existingModel,
        SourceCache<ComponentModel, int> cachedComponents,
        string noImageUrl)
    {
        if (!cachedComponents.Lookup(updatedComponent.Id).HasValue) return;

        var remapped = await componentMapper.MapToModelAsync(
            updatedComponent,
            existingModel.EditCommand,
            existingModel.DeleteCommand,
            existingModel.ToggleInRecipeCommand,
            noImageUrl);

        cachedComponents.AddOrUpdate(remapped);
    }
}
