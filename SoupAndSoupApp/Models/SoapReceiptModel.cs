using System;
using System.Collections.ObjectModel;
using Avalonia.Media.Imaging;

namespace SoupAndSoupApp.Models;

public class SoapReceiptModel
{
    public string Name { get; set; }
    public ObservableCollection<IngredientByReceiptModel> Ingredients { get; set; }

    public decimal WeightNet { get; set; }
    public TimeSpan MakingTime{ get; set; }
    public string Description { get; set; }

    public decimal TotalCost { get; set; }
    public Bitmap? Photo { get; set; }
}


