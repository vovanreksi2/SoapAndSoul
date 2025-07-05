using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DynamicData;
using SoupAndSoupApp.Models;

namespace SoupAndSoupApp.Helpers;

public class UnitCostCalculator : IUnitCostCalculator
{
    private const decimal ArrangeAmountOfUseForm = 100m;
    private const decimal DropsInMilliliters = 20;

    public decimal CalculateUnitCost(IEnumerable<ComponentByRecipeModel> recipeComponents, SourceCache<ComponentModel, int> cachedComponents)
    {
        var result = 0m;

        foreach (var recipeComponent in recipeComponents)
        {
            var component =  GetCachedComponentById(cachedComponents, recipeComponent.ComponentId);
            if (component is null) continue;

            if (component.Type == ComponentType.Form)
            {
                result += component.BuyPrice / ArrangeAmountOfUseForm;
            }
            else if ((MeasureType)component.UseMeasureTypeId == MeasureType.Milliliter)
            {
                result += component.Cost * ConvertMilliliterInDrop(recipeComponent.Amount);
            }
            else
                result += component.Cost * recipeComponent.Amount;
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
                result += recipeComponent.Cost * recipeComponent.AmountInRecipe;
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

    private ComponentModel? GetCachedComponentById(SourceCache<ComponentModel, int>  cachedComponents, int id)
    {
        if (cachedComponents.Lookup(id).HasValue)
            return cachedComponents.Lookup(id).Value;

        Debug.WriteLine($"Component with ID {id} not found in cache.");
        return null;
    }

}