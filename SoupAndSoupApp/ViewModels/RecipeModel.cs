using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Avalonia.Media.Imaging;
using ReactiveUI;
using SoupAndSoupApp.Models;

namespace SoupAndSoupApp.ViewModels;

public class RecipeModel: ReactiveObject
{
    public int Id { get; set; }
    public string Name { get; set; }

    public Bitmap? ImagePath
    {
        get => _imagePath;
        set => this.RaiseAndSetIfChanged(ref _imagePath, value);
    }

    public string Description { get; set; }
    public decimal Amount { get; set; }

    public decimal UnitCost
    {
        get => _unitCost;
        set => this.RaiseAndSetIfChanged(ref _unitCost, value);
    }

    public int PreparationTime { get; set; }

    public ICommand? DeleteReceiptCommand { get; set; }

    public ObservableCollection<IngredientByReceiptModel> RecipeIngredients { get; set; } = new();
    
    private decimal _unitCost;
    private Bitmap? _imagePath;
}