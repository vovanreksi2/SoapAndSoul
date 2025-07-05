using Microsoft.EntityFrameworkCore;
using SoupAndSoup.Data.Models;

namespace SoupAndSoup.Data.Services;
public class RecipeService : RepositoryBase<Recipe>
{
    public RecipeService(SoapAndSoulContext context) : base(context) { }

    public override async Task<Recipe> GetByIdAsync(int id)
    {
        return await _dbSet
            .Include(r => r.RecipeComponents)
            .ThenInclude(ri => ri.Component)
            .Include(r => r.Images)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public override async Task<List<Recipe>> GetAllAsync(bool noTracking = false)
    {
        return await _dbSet
            .Where(r=> r.IsActive)
            .Include(r => r.RecipeComponents)
                .ThenInclude(ri => ri.Component)
                    .ThenInclude(i=> i.Images)
            .Include(r => r.Images)
            .ToListAsync();
    }

    public override async Task<Recipe?> CreateAsync(Recipe recipe)
    {
        recipe.DateOfCreate = DateTime.UtcNow;
        return await base.CreateAsync(recipe);
    }

    public override async Task<bool> UpdateAsync(Recipe recipe)
    {
        var entity = await GetByIdAsync(recipe.Id);
        if (entity is null)
            return false;

        _context.Entry(entity).CurrentValues.SetValues(recipe);

        entity.RecipeComponents = recipe.RecipeComponents;
        entity.Images = recipe.Images;

        entity.Version += 1;

        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> SoftDelete(int id)
    {
        var entity = await GetByIdAsync(id);
        if (entity == null)
            return false;

        entity.IsActive = false;
        await _context.SaveChangesAsync();
        return true;
    }
}

