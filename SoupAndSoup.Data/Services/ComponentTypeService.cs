using Microsoft.EntityFrameworkCore;
using SoupAndSoup.Data.Models;

namespace SoupAndSoup.Data.Services;

public class ComponentTypeService : RepositoryBase<ComponentType>, IComponentTypeService
{
    public ComponentTypeService(IDbContextFactory<SoapAndSoulContext> contextFactory) : base(contextFactory) { }

    public Task<List<ComponentType>> GetAllAsync(int cosmeticType)
    {
        return UseContextAsync((context, dbSet) =>
            dbSet.Where(i => i.CosmeticTypes.Any(c => c.Id == cosmeticType))
                .Include(i => i.UseMeasureTypes)
                .Include(i => i.BuyMeasureTypes)
                .ToListAsync()
        );
    }

}