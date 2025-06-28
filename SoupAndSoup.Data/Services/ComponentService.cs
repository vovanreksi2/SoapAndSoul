using Microsoft.EntityFrameworkCore;
using SoupAndSoup.Data;
using SoupAndSoup.Data.Models;

public class ComponentService : RepositoryBase<Component>
{
    public ComponentService(SoapAndSoulContext context) : base(context) { }

    public override async Task<Component> GetByIdAsync(int id)
    {
        return await _dbSet
                .Include(i => i.ComponentType)
                .Include(i => i.Images)

            .FirstOrDefaultAsync(i => i.Id == id);
    }

    public Task<List<Component>> GetAllAsync(int cosmeticType)
    {
        return _dbSet
            .Where(i => i.ComponentType.CosmeticTypes.Any(c => c.Id == cosmeticType))
                .Include(i => i.ComponentType)
                .Include(i => i.Images)
            .AsNoTracking() // Use AsNoTracking for read-only operations
            .ToListAsync();
    }

    public override async Task<bool> UpdateAsync(Component entity)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));

        var existing = await GetByIdAsync(entity.Id);

        existing.Name = entity.Name;
        existing.Cost = entity.Cost;
        existing.BuyAmount = entity.BuyAmount;
        existing.BuyPrice = entity.BuyPrice;
        existing.SuggestedAmount = entity.SuggestedAmount;
        existing.UseMeasureTypeId = entity.UseMeasureTypeId;

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