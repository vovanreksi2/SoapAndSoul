using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using DynamicData;
using SoupAndSoup.Data.Models;
using SoupAndSoupApp.Helpers.Calculators;
using SoupAndSoupApp.Helpers.Images;
using SoupAndSoupApp.Models;
using CosmeticType = SoupAndSoupApp.Models.CosmeticType;

namespace SoupAndSoupApp.Helpers.Mappers;

public class RecipeMapper(IUnitCostCalculator unitCostCalc, IImageService imageService) : IRecipeMapper
{
    public async Task<RecipeModel> MapToModelAsync(Recipe recipe, ICommand deleteRecipeCommand,
        SourceCache<ComponentModel, int> cachedComponents, string noImageUrl)
    {
        var result = new RecipeModel();

        result.BeginInit();

        result.Id = recipe.Id;
        result.Name = recipe.Name;
        result.Description = recipe.Description;
        result.Amount = recipe.Amount;
        result.PreparationTime = (decimal)recipe.PreparationTime.TotalMinutes;
        result.DeleteRecipeCommand = deleteRecipeCommand;

        result.RecipeComponents = recipe.RecipeComponents.Select(MapToComponentByRecipeModel);

        // Populate SelectedComponents SourceCache from DB data
        foreach (var rc in recipe.RecipeComponents)
        {
            var componentByRecipe = MapToComponentByRecipeModel(rc);
            var masterLookup = cachedComponents.Lookup(rc.ComponentId);
            if (masterLookup.HasValue)
                componentByRecipe.Component = masterLookup.Value;
            result.SelectedComponents.AddOrUpdate(componentByRecipe);
        }

        result.ImagePathString = recipe.Images.FirstOrDefault()?.ImageUrl ?? string.Empty;
        result.ImagePath = await imageService.LoadImageOrDefaultAsync(result.ImagePathString, result.Id, noImageUrl);

        result.UnitCost = unitCostCalc.CalculateUnitCost(result.RecipeComponents, cachedComponents);

        result.EndInit();

        return result;
    }

    public Recipe MapToEntity(RecipeModel recipe, CosmeticType cosmeticType, string noImageUrl)
    {
        var result = new Recipe
        {
            Id = recipe.Id,
            Amount = recipe.Amount,
            Name = recipe.Name,
            PreparationTime = TimeSpan.FromMinutes((int)recipe.PreparationTime),
            CosmeticTypeId = (int)cosmeticType,
            Description = recipe.Description,
            RecipeComponents = recipe.SelectedComponents.Items
                .Select(MapToRecipeComponent).ToList(),
        };

        if (!string.IsNullOrEmpty(recipe.ImagePathString) && recipe.ImagePathString != noImageUrl)
            result.Images = new List<RecipeImage> { new() { ImageUrl = recipe.ImagePathString } };

        return result;
    }

    public ComponentByRecipeModel MapToComponentByRecipeModel(RecipeComponent recipeComponent) =>
        new()
        {
            ComponentId = recipeComponent.ComponentId,
            Amount = recipeComponent.Amount
        };

    public ComponentByRecipeModel MapToComponentByRecipeModel(ComponentByRecipeModel selection) =>
        new()
        {
            ComponentId = selection.ComponentId,
            Amount = selection.Amount,
            Component = selection.Component
        };

    public RecipeComponent MapToRecipeComponent(ComponentByRecipeModel selection) =>
        new()
        {
            ComponentId = selection.ComponentId,
            Amount = selection.Component?.AmountInRecipe ?? selection.Amount
        };
}
