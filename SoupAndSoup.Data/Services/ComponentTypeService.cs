using Microsoft.EntityFrameworkCore;
using SoupAndSoup.Data;
using SoupAndSoup.Data.Models;

public class ComponentTypeService : RepositoryBase<ComponentType>
{
    public ComponentTypeService(SoapAndSoulContext context) : base(context)
    {

    }

    public   async Task<List<ComponentType>> GetAllAsync(int cosmeticType)
    {
        return await _dbSet
            .Where(i => i.CosmeticTypes.Any(c => c.Id == cosmeticType))
            .Include(i => i.UseMeasureTypes)
            .Include(i => i.BuyMeasureTypes)
            .ToListAsync();
    }

}