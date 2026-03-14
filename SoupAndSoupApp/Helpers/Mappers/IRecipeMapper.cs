using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;
using DynamicData;
using SoupAndSoup.Data.Models;
using SoupAndSoupApp.Models;
using CosmeticType = SoupAndSoupApp.Models.CosmeticType;

namespace SoupAndSoupApp.Helpers.Mappers;

public interface IRecipeMapper
{
    Task<RecipeModel> MapToModelAsync(Recipe recipe, ICommand deleteRecipeCommand,
        SourceCache<ComponentModel, int> cachedComponents, string noImageUrl);

    Recipe MapToEntity(RecipeModel recipe, IEnumerable<ComponentModel> componentsByRecipe,
        CosmeticType cosmeticType, string noImageUrl);

    ComponentByRecipeModel MapToComponentByRecipeModel(RecipeComponent recipeComponent);
    ComponentByRecipeModel MapToComponentByRecipeModel(ComponentModel component);
    RecipeComponent MapToRecipeComponent(ComponentModel component);
}
