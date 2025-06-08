using Avalonia.Media.Imaging;

namespace SoupAndSoupApp.Models;

public class IngredientByReceiptModel
{
    public int IngredientId { get; set; }
    public string Name { get; set; }
    public Bitmap ImagePath { get; set; }
    public decimal Cost { get; set; }
    public decimal Amount { get; set; }
    public string CountTitle { get; set; }
    public SoapTypeComponent Type { get; set; }

}