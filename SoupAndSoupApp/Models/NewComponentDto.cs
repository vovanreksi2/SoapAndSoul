namespace SoupAndSoupApp.Models;

public class NewComponentDto
{
    public bool IsPhotoChanged => !string.IsNullOrEmpty(ImagePath);
    public string ImagePath { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal SuggestedAmount { get; set; }

    public decimal BuyAmount { get; set; }
    public decimal BuyPrice { get; set; }

    public MeasureTypeModel UseMeasureType { get; set; }
    public MeasureTypeModel BuyMeasureType { get; set; }

    public decimal Cost { get; set; }
}