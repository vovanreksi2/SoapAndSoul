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
                .Include(i => i.AmountType)

            .FirstOrDefaultAsync(i => i.Id == id);
    }

    public override  Task<List<Ingredient>> GetAllAsync()
    {
        return _dbSet
            .Where(i => i.IsActive) // Only get active ingredients
                .Include(i => i.IngredientType)
                .Include(i => i.AmountType)
                .Include(i => i.Images)
            .AsNoTracking() // Use AsNoTracking for read-only operations
            .ToListAsync();
    }

    public override async Task<bool> UpdateAsync(Ingredient entity)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));

        var existing = await GetByIdAsync(entity.Id);

        existing.Name = entity.Name;
        existing.Cost = entity.Cost;
        existing.BuyAmount = entity.BuyAmount;
        existing.BuyPrice = entity.BuyPrice;
        existing.TypicalAmountInRecipe = entity.TypicalAmountInRecipe;

        if (entity.Images.Any())
            existing.Images = entity.Images; 

        _dbSet.Update(existing);
        return await _context.SaveChangesAsync() > 0;
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