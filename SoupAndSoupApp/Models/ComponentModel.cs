using System.Windows.Input;
using Avalonia.Media.Imaging;
using ReactiveUI;

namespace SoupAndSoupApp.Models;

public class ComponentModel : ReactiveObject
{
    public int Id { get; set; }

    public Bitmap ImagePath
    {
        get => _imagePath;
        set => this.RaiseAndSetIfChanged(ref _imagePath, value);
    }

    public string Name
    {
        get => _name;
        set => this.RaiseAndSetIfChanged(ref _name, value);
    }

    public decimal AmountInRecipe
    {
        get => _buyAmount;
        set => this.RaiseAndSetIfChanged(ref _buyAmount, value);
    }

    public decimal BuyAmount
    {
        get => _buyAmount;
        set => this.RaiseAndSetIfChanged(ref _buyAmount, value);
    }
    public decimal BuyPrice { get; set; }

    public int UseMeasureTypeId { get; set; }
    public string UseMeasureTypeShortTitle { get; set; } 

    public int BuyMeasureTypeId { get; set; }
    public string BuyMeasureTypeShortTitle { get; set; }


    public decimal Cost { get; set; }
    public decimal SuggestedAmount { get; set; }

    public ComponentType Type { get; set; }

    public bool IsButton { get; set; }
    public bool IsSelected
    {
        get => _isSelected;
        set => this.RaiseAndSetIfChanged(ref _isSelected, value);
    }

    public bool ShowAmountInButton { get; set; }

    public ICommand EditCommand { get; set; }
    public ICommand DeleteCommand { get; set; }

    private bool _isSelected;
    private decimal _buyAmount;
    private string _name;
    private Bitmap _imagePath;
}