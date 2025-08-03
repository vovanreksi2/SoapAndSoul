using System.Collections.Generic;
using DynamicData;
using SoupAndSoupApp.Models;

namespace SoupAndSoupApp.Helpers.Calculators;

public  interface IUnitCostCalculator
{
    decimal CalculateUnitCost(IEnumerable<ComponentByRecipeModel> recipeComponents, SourceCache<ComponentModel, int> cachedComponents);

    decimal CalculateUnitCost(IEnumerable<ComponentModel> components);
    decimal CalculateComponentCostForOneMeasure(ComponentType componentType, MeasureType? measureType, decimal buyPrice, decimal buyAmount);
}