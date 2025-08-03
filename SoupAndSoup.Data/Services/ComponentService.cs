using Microsoft.EntityFrameworkCore;
using SoupAndSoup.Data.Models;

namespace SoupAndSoup.Data.Services;

public class ComponentService : RepositoryBase<Component>, IComponentService
{
    public ComponentService(IDbContextFactory<SoapAndSoulContext> contextFactory) : base(contextFactory) { }

    public override Task<Component?> GetByIdAsync(int id) =>
        UseContextAsync((context, dbSet) =>
            dbSet
                .Include(i => i.ComponentType)
                .Include(i => i.Images)
                .FirstOrDefaultAsync(i => i.Id == id)
        );

    public Task<List<Component>> GetAllAsync(int cosmeticType) =>
        UseContextAsync((context, dbSet) => dbSet
            .Where(i => i.ComponentType.CosmeticTypes.Any(c => c.Id == cosmeticType))
            .Include(i => i.ComponentType)
            .Include(i => i.Images)
            .AsNoTracking()
            .ToListAsync()
        );

    public override Task<bool> UpdateAsync(Component entity)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));

        return UseContextAsync(async (context, dbSet) =>
        {
            var existing = await dbSet
                .Include(i => i.ComponentType)
                .Include(i => i.Images)
                .FirstOrDefaultAsync(i => i.Id == entity.Id);

            if (existing is null)
                return false;

            context.Entry(existing).CurrentValues.SetValues(entity);

            if (entity.Images.Any())
                existing.Images = entity.Images;

            context.Entry(existing).State = EntityState.Modified;
            return await context.SaveChangesAsync() > 0;
        });
    }

    public Task<bool> SoftDeleteAsync(int id) =>
        UseContextAsync(async (context, dbSet) =>
        {
            var entity = await dbSet.FindAsync(id);
            if (entity == null)
                return false;

            entity.IsActive = false;
            context.Entry(entity).State = EntityState.Modified;

            return await context.SaveChangesAsync() > 0;
        });
}