namespace SoupAndSoup.Data.Models;

public class RecipeComponent
{
    public int RecipeId { get; set; }
    public Recipe Recipe { get; set; }

    public int ComponentId { get; set; }
    public Component Component { get; set; }

    public decimal Amount { get; set; }
}