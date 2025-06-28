using System.Collections.Generic;
using System.Diagnostics;
using SoupAndSoupApp.Models;

namespace SoupAndSoupApp.Helpers;

public class UnitCostCalculator : IUnitCostCalculator
{
    private const decimal ArrangeAmountOfUseForm = 100m;
    private const decimal DropsInMilliliters = 20;

    public decimal CalculateUnitCost(IEnumerable<ComponentByRecipeModel> recipeComponents, Dictionary<int, ComponentModel> cachedComponents)
    {
        var result = 0m;

        foreach (var recipeComponent in recipeComponents)
        {
            if (!cachedComponents.TryGetValue(recipeComponent.ComponentId, out var ingredientModel))
            {
                Debug.WriteLine($"Component with ID {recipeComponent.ComponentId} not found in cached components.");
                continue; 
            }

            if (ingredientModel.Type == ComponentType.Form)
            {
                result += ingredientModel.BuyPrice / ArrangeAmountOfUseForm;
            }
            else if ((MeasureType)ingredientModel.UseMeasureTypeId == MeasureType.Milliliter)
            {
                result += ingredientModel.Cost * ConvertMilliliterInDrop(recipeComponent.Amount);
            }
            else
                result += ingredientModel.Cost * recipeComponent.Amount;
        }

        return result;
    }

    public decimal CalculateUnitCost(IEnumerable<ComponentModel> components)
    {
        var result = 0m;

        foreach (var recipeComponent in components)
        {
            if (recipeComponent.Type == ComponentType.Form)
            {
                result += recipeComponent.BuyPrice / ArrangeAmountOfUseForm;
            }
            else if ((MeasureType)recipeComponent.UseMeasureTypeId == MeasureType.Milliliter)
            {
                result += recipeComponent.Cost * ConvertMilliliterInDrop(recipeComponent.BuyAmount);
            }
            else
                result += recipeComponent.Cost * recipeComponent.BuyAmount;
        }

        return result;
    }

    private decimal ConvertMilliliterInDrop(decimal amount)
    {
        return amount / DropsInMilliliters;
    }

    public decimal CalculateComponentCostForOneMeasure(ComponentType componentType, MeasureType? measureType, decimal buyPrice, decimal buyAmount)
    {
        if (buyPrice <= 0 || buyAmount <= 0 || measureType is null)
        {
            return 0;
        }

        if (componentType == ComponentType.Form)
            return buyPrice;

        //If the measure type is Drop, convert the buy amount from milliliters to drops 
        if (measureType == MeasureType.Drop)
            return buyPrice / (buyAmount * DropsInMilliliters);
     
        return buyPrice / buyAmount;
    }
}