using System;
using System.Collections.Generic;
using System.Linq;
using DynamicData;
using Microsoft.Extensions.Logging;
using SoupAndSoupApp.Models;

namespace SoupAndSoupApp.Helpers.Calculators;

public class UnitCostCalculator : IUnitCostCalculator
{
    private readonly ILogger<UnitCostCalculator> _logger;

    public UnitCostCalculator(ILogger<UnitCostCalculator> logger)
    {
        _logger = logger;
    }

    private const decimal ArrangeAmountOfUseForm = 100m;
    private const decimal DropsInMilliliters = 20;

    /// <summary>
    /// Calculates the unit cost of a recipe based on its components.
    /// </summary>
    /// <param name="recipeComponents"></param>
    /// <param name="cachedComponents"></param>
    /// <returns></returns>
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

        return Math.Round(result);
    }

    /// <summary>
    /// Calculates the unit cost of a collection of components.
    /// </summary>
    /// <param name="components"></param>
    /// <returns></returns>
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

        return Math.Round(result);
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
        var lookup = cachedComponents.Lookup(id);
        if (lookup.HasValue)
            return lookup.Value;

        _logger.LogWarning("Component with ID {componentId} not found in cache", id);
        return null;
    }

}