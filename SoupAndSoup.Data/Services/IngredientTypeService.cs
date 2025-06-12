using Microsoft.EntityFrameworkCore;
using SoupAndSoup.Data;
using SoupAndSoup.Data.Models;

public class IngredientTypeService : RepositoryBase<IngredientType>
{
    public IngredientTypeService(SoapAndSoulContext context) : base(context)
    {

    }

    public override async Task<List<IngredientType>> GetAllAsync()
    {
        return await _dbSet
            .Include(i => i.AmountTypes)
            .ToListAsync();
    }

}