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

public class RecipeMapper : IRecipeMapper
{
    private readonly IUnitCostCalculator _unitCostCalc;
    private readonly IImageService _imageService;

    public RecipeMapper(IUnitCostCalculator unitCostCalc, IImageService imageService)
    {
        _unitCostCalc = unitCostCalc;
        _imageService = imageService;
    }

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

        result.ImagePathString = recipe.Images.FirstOrDefault()?.ImageUrl ?? string.Empty;
        result.ImagePath = await _imageService.LoadImageOrDefaultAsync(result.ImagePathString, result.Id, noImageUrl);

        result.UnitCost = _unitCostCalc.CalculateUnitCost(result.RecipeComponents, cachedComponents);

        result.EndInit();

        return result;
    }

    public Recipe MapToEntity(RecipeModel recipe, IEnumerable<ComponentModel> componentsByRecipe,
        CosmeticType cosmeticType, string noImageUrl)
    {
        var result = new Recipe
        {
            Id = recipe.Id,
            Amount = recipe.Amount,
            Name = recipe.Name,
            PreparationTime = TimeSpan.FromMinutes((int)recipe.PreparationTime),
            CosmeticTypeId = (int)cosmeticType,
            Description = recipe.Description,
            RecipeComponents = componentsByRecipe.Select(MapToRecipeComponent).ToList(),
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

    public ComponentByRecipeModel MapToComponentByRecipeModel(ComponentModel component) =>
        new()
        {
            ComponentId = component.Id,
            Amount = component.AmountInRecipe
        };

    public RecipeComponent MapToRecipeComponent(ComponentModel component) =>
        new()
        {
            ComponentId = component.Id,
            Amount = component.AmountInRecipe
        };
}
