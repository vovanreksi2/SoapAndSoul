using SoupAndSoup.Data.Models;

namespace SoupAndSoup.Data.Services;

public interface IRecipeService
{
    Task<Recipe?> GetByIdAsync(int id);
    Task<List<Recipe>> GetAllAsync(int cosmeticType, bool noTracking = false);
    Task<Recipe?> CreateAsync(Recipe recipe);
    Task<bool> UpdateAsync(Recipe recipe);
    Task<bool> SoftDelete(int id);
    Task<bool> DeleteAsync(int id);
}