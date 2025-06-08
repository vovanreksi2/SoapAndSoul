using Avalonia.Media.Imaging;

namespace SoupAndSoupApp.ViewModels;

public class NewIngredientDto
{
    public string Name { get; set; } = string.Empty;
    public decimal Cost { get; set; }
    public string ImagePath { get; set; }
}