using Microsoft.EntityFrameworkCore;
using SoupAndSoup.Data;
using SoupAndSoup.Data.Models;

public class IngredientService : RepositoryBase<Ingredient>
{
    public IngredientService(SoapAndSoulContext context) : base(context) { }

    public override async Task<Ingredient> GetByIdAsync(int id)
    {
        return await _dbSet
            .Include(i => i.IngredientType)
            .Include(i => i.Images)
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    public override async Task<List<Ingredient>> GetAllAsync()
    {
        return await _dbSet
            .Where(i => i.IsActive) // Only get active ingredients
            .Include(i => i.IngredientType)
            .Include(i => i.AmountTypes)
            .Include(i => i.Images)
            .ToListAsync();
    }

    public async Task<bool> SoftDeleteAsync(int id)
    {
        var entity = await GetByIdAsync(id);
        if (entity == null)
            return false;

        entity.IsActive = false;
        await _context.SaveChangesAsync();
        return true;
    }
}