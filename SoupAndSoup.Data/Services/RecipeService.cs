using Microsoft.EntityFrameworkCore;
using SoupAndSoup.Data.Models;

namespace SoupAndSoup.Data.Services;

public class RecipeService(IDbContextFactory<SoapAndSoulContext> contextFactory)
    : RepositoryBase<Recipe>(contextFactory), IRecipeService
{
    public override Task<Recipe?> GetByIdAsync(int id) =>
        UseContextAsync((context, dbSet) => dbSet
                .Include(r => r.RecipeComponents)
                    .ThenInclude(ri => ri.Component)
                .Include(r => r.Images)
                .FirstOrDefaultAsync(r => r.Id == id));

    public Task<List<Recipe>> GetAllAsync(int cosmeticType, bool noTracking = false) =>
        UseContextAsync((context, dbSet) =>
        {
            var query = dbSet
                .Where(r => r.IsActive)
                .Where(r => r.CosmeticTypeId == cosmeticType)
                .Include(r => r.RecipeComponents)
                    .ThenInclude(ri => ri.Component)
                .Include(r => r.Images)
                .AsSplitQuery()
                .AsQueryable();

            if (noTracking)
                query = query.AsNoTracking();

            return query.ToListAsync();
        });

    public override Task<Recipe?> CreateAsync(Recipe recipe) =>
        UseContextAsync(async (context, dbSet) =>
        {
            recipe.DateOfCreate = DateTime.UtcNow;
            dbSet.Add(recipe);

            var result = await context.SaveChangesAsync();
            return result > 0 ? recipe : null;
        });

    public override Task<bool> UpdateAsync(Recipe recipe) =>
        UseContextAsync(async (context, dbSet) =>
        {
            var entity = await dbSet
                .Include(r => r.RecipeComponents)
                .Include(r => r.Images)
                .FirstOrDefaultAsync(r => r.Id == recipe.Id);

            if (entity is null)
                return false;

            context.Entry(entity).CurrentValues.SetValues(recipe);

            entity.RecipeComponents = recipe.RecipeComponents;
            entity.Images = recipe.Images;

            entity.Version += 1;

            return await context.SaveChangesAsync() > 0;
        });

    public Task<bool> SoftDelete(int id) =>
        UseContextAsync(async (context, dbSet) =>
        {
            var entity = await dbSet.FindAsync(id);
            if (entity is null)
                return false;

            entity.IsActive = false;
            var result = await context.SaveChangesAsync();
            return result > 0;
        });
}
