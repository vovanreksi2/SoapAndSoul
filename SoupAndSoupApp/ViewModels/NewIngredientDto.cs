using SoupAndSoupApp.Models;

namespace SoupAndSoupApp.ViewModels;

public class NewIngredientDto
{
    public string Name { get; set; } = string.Empty;
    public decimal Cost { get; set; }
    public string ImagePath { get; set; }

    
    public MeasureTypeModel MeasureType { get; set; }

    public decimal TypicalAmountInRecipe { get; set; }

    public decimal BuyAmount { get; set; }
    public decimal BuyPrice { get; set; }
}