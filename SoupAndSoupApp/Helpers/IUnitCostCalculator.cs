using System.Collections.Generic;
using SoupAndSoupApp.Models;

namespace SoupAndSoupApp.Helpers;

public  interface IUnitCostCalculator
{
    decimal CalculateUnitCost(IEnumerable<ComponentByRecipeModel> recipeComponents, Dictionary<int, ComponentModel> cachedComponents);

    decimal CalculateUnitCost(IEnumerable<ComponentModel> components);
    decimal CalculateComponentCostForOneMeasure(ComponentType componentType, MeasureType? measureType, decimal buyPrice, decimal buyAmount);
}