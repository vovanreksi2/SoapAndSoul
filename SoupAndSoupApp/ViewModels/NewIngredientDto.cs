using SoupAndSoupApp.Models;

namespace SoupAndSoupApp.ViewModels;

public class NewIngredientDto
{
    public string Name { get; set; } = string.Empty;
    public decimal Cost { get; set; }
    public string ImagePath { get; set; }

    public MeasureTypeModel MeasureType { get; set; }

    public decimal DefaultAmount { get; set; }

    public decimal Amount { get; set; }
    public decimal Price { get; set; }
}