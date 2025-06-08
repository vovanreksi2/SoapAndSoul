using SoupAndSoup.Data;
using SoupAndSoup.Data.Models;

public class IngredientTypeService : RepositoryBase<IngredientType>
{
    public IngredientTypeService(SoapAndSoulContext context) : base(context)
    {
    }
}