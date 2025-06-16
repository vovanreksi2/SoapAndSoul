using System.Collections.Generic;
using SoupAndSoup.Data.Models;
using SoupAndSoupApp.Models;

namespace SoupAndSoupApp.Helpers
{
    public  interface IUnitCostCalculator
    {
        decimal CalculateUnitCost(IEnumerable<IngredientByReceiptModel> recipeComponents, Dictionary<int, IngredientModel> cachedComponents);
    }
}
