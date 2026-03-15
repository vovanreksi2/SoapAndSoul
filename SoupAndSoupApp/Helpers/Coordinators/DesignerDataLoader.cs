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

public class DesignerDataLoader
{
    private readonly IRecipeService _recipeService;
    private readonly IComponentService _componentService;
    private readonly IComponentTypeService _componentTypeService;
    private readonly RecipeCoordinator _recipeCoordinator;
    private readonly ComponentCoordinator _componentCoordinator;
    private readonly ILogger<DesignerDataLoader> _logger;

    public DesignerDataLoader(
        IRecipeService recipeService,
        IComponentService componentService,
        IComponentTypeService componentTypeService,
        RecipeCoordinator recipeCoordinator,
        ComponentCoordinator componentCoordinator,
        ILogger<DesignerDataLoader> logger)
    {
        _recipeService = recipeService;
        _componentService = componentService;
        _componentTypeService = componentTypeService;
        _recipeCoordinator = recipeCoordinator;
        _componentCoordinator = componentCoordinator;
        _logger = logger;
    }

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
                var componentTypes = await _componentTypeService.GetAllAsync((int)cosmeticType);

                cachedComponentTypes.Clear();
                foreach (var type in componentTypes)
                    cachedComponentTypes[(ComponentType)type.Id] = new ComponentTypeModel(type);

                foreach (var type in cachedComponentTypes)
                    cachedComponents.AddOrUpdate(CreateButtonComponent(type.Key));
            },
            _logger,
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
                var components = await _componentService.GetAllAsync((int)cosmeticType);

                var mapped = await Task.WhenAll(components.Select(async c =>
                {
                    try
                    {
                        return await _componentCoordinator.MapComponentModelAsync(
                            c, editCommand, deleteCommand, toggleCommand, noImageUrl);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to map component {ComponentId} during bulk load", c.Id);
                        return null;
                    }
                }));

                foreach (var model in mapped.OfType<ComponentModel>())
                    cachedComponents.AddOrUpdate(model);
            },
            _logger,
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
                var recipes = await _recipeService.GetAllAsync((int)cosmeticType);

                var mapped = await Task.WhenAll(recipes.Select(async r =>
                {
                    try
                    {
                        return await _recipeCoordinator.MapFromEntityAsync(r, deleteRecipeCommand, cachedComponents, noImageUrl);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to map recipe {RecipeId} during bulk load", r.Id);
                        return null;
                    }
                }));

                foreach (var recipeModel in mapped.OfType<RecipeModel>())
                    cachedRecipes.AddOrUpdate(recipeModel);
            },
            _logger,
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
