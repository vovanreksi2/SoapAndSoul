using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using DynamicData;
using SoupAndSoup.Data.Services;
using SoupAndSoupApp.Models;
using ComponentType = SoupAndSoupApp.Models.ComponentType;
using CosmeticType = SoupAndSoupApp.Models.CosmeticType;

namespace SoupAndSoupApp.Helpers.Coordinators;

public class DesignerDataLoader(
    IRecipeService recipeService,
    IComponentService componentService,
    IComponentTypeService componentTypeService,
    RecipeCoordinator recipeCoordinator,
    ComponentCoordinator componentCoordinator,
    ILogger<DesignerDataLoader> logger)
{
    public Task LoadComponentTypesAsync(
        ActivitySource activitySource,
        CosmeticType cosmeticType,
        SourceCache<ComponentModel, int> cachedComponents,
        Dictionary<ComponentType, ComponentTypeModel> cachedComponentTypes)
    {
        return ActivityHelper.RunWithActivity(
            activitySource,
            nameof(LoadComponentTypesAsync),
            async () =>
            {
                var componentTypes = await componentTypeService.GetAllAsync((int)cosmeticType);

                cachedComponentTypes.Clear();
                foreach (var type in componentTypes)
                    cachedComponentTypes[(ComponentType)type.Id] = new ComponentTypeModel(type);

                foreach (var type in cachedComponentTypes)
                    cachedComponents.AddOrUpdate(CreateButtonComponent(type.Key));
            },
            logger,
            ("componentTypes.count", cachedComponents.Count), ("cosmetic.type", cosmeticType));
    }

    public Task LoadComponentsAsync(
        ActivitySource activitySource,
        CosmeticType cosmeticType,
        SourceCache<ComponentModel, int> cachedComponents,
        ICommand editCommand, ICommand deleteCommand, ICommand toggleCommand,
        string noImageUrl)
    {
        return ActivityHelper.RunWithActivity(
            activitySource,
            nameof(LoadComponentsAsync),
            async () =>
            {
                var components = await componentService.GetAllAsync((int)cosmeticType);

                var mapped = await Task.WhenAll(components.Select(async c =>
                {
                    try
                    {
                        return await componentCoordinator.MapComponentModelAsync(
                            c, editCommand, deleteCommand, toggleCommand, noImageUrl);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to map component {ComponentId} during bulk load", c.Id);
                        return null;
                    }
                }));

                foreach (var model in mapped.OfType<ComponentModel>())
                    cachedComponents.AddOrUpdate(model);
            },
            logger,
            ("components.count", cachedComponents.Count),
            ("cosmetic.type", cosmeticType));
    }

    public Task LoadRecipesAsync(
        ActivitySource activitySource,
        CosmeticType cosmeticType,
        SourceCache<RecipeModel, int> cachedRecipes,
        SourceCache<ComponentModel, int> cachedComponents,
        ICommand deleteRecipeCommand,
        string noImageUrl)
    {
        return ActivityHelper.RunWithActivity(
            activitySource,
            nameof(LoadRecipesAsync),
            async () =>
            {
                var recipes = await recipeService.GetAllAsync((int)cosmeticType);

                var mapped = await Task.WhenAll(recipes.Select(async r =>
                {
                    try
                    {
                        return await recipeCoordinator.MapFromEntityAsync(r, deleteRecipeCommand, cachedComponents, noImageUrl);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to map recipe {RecipeId} during bulk load", r.Id);
                        return null;
                    }
                }));

                foreach (var recipeModel in mapped.OfType<RecipeModel>())
                    cachedRecipes.AddOrUpdate(recipeModel);
            },
            logger,
            ("cosmetic.type", cosmeticType));
    }

    private static ComponentModel CreateButtonComponent(ComponentType type)
    {
        return new ComponentModel
        {
            Id = -(int)type,
            IsInCurrentRecipe = false,
            IsButton = true,
            Type = type,
        };
    }
}
