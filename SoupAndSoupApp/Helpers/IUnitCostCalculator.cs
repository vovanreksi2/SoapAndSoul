using System.Collections.Generic;
using SoupAndSoupApp.Models;

namespace SoupAndSoupApp.Helpers;

public  interface IUnitCostCalculator
{
    decimal CalculateUnitCost(IEnumerable<IngredientByReceiptModel> recipeComponents, Dictionary<int, IngredientModel> cachedComponents);

    decimal CalculateUnitCost(IEnumerable<IngredientModel> components);
}