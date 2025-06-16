using System.Collections.Generic;
using System.Diagnostics;
using SoupAndSoupApp.Models;

namespace SoupAndSoupApp.Helpers;

public class UnitCostCalculator : IUnitCostCalculator
{
    private const decimal ArrangeAmountOfUseForm = 100m;
    private const decimal DropsInMilliliters = 20;

    public decimal CalculateUnitCost(IEnumerable<IngredientByReceiptModel> recipeComponents, Dictionary<int, IngredientModel> cachedComponents)
    {
        var result = 0m;

        foreach (var recipeComponent in recipeComponents)
        {
            if (!cachedComponents.TryGetValue(recipeComponent.IngredientId, out var ingredientModel))
            {
                Debug.WriteLine($"Ingredient with ID {recipeComponent.IngredientId} not found in cached components.");
                continue; 
            }

            if (ingredientModel.Type == SoapTypeComponent.Form)
            {
                result += ingredientModel.BuyPrice / ArrangeAmountOfUseForm;
            }
            else if ((MeasureType)ingredientModel.MeasureType.Id == MeasureType.Milliliter)
            {
                result += ingredientModel.Cost * ConvertMilliliterInDrop(recipeComponent.Amount);
            }
            else
                result += ingredientModel.Cost * recipeComponent.Amount;
        }

        return result;
    }

    private decimal ConvertMilliliterInDrop(decimal amount)
    {
        return amount / DropsInMilliliters;
    }
}