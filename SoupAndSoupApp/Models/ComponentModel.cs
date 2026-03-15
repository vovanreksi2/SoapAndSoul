using System.Windows.Input;
using ReactiveUI;

namespace SoupAndSoupApp.Models;

public class ComponentModel : BaseModel
{
    public const decimal DefaultIncreaseAmount = 10m;

    public ComponentModel()
    {
        IncreaseAmountCommand = ReactiveCommand.Create(() => AmountInRecipe+= DefaultIncreaseAmount);
        DecreaseAmountCommand = ReactiveCommand.Create(() => { if (AmountInRecipe > 0) AmountInRecipe-= DefaultIncreaseAmount; });
    }


    public decimal AmountInRecipe
    {
        get => _amountInRecipe;
        set => this.RaiseAndSetIfChanged(ref _amountInRecipe, value);
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

    public bool IsInCurrentRecipe
    {
        get => _isInCurrentRecipe;
        set => this.RaiseAndSetIfChanged(ref _isInCurrentRecipe, value);
    }

    public bool ShowAmountInButton { get; set; }

    public ICommand EditCommand { get; set; }
    public ICommand DeleteCommand { get; set; }
    public ICommand ToggleInRecipeCommand { get; set; }
    public ICommand IncreaseAmountCommand { get; }
    public ICommand DecreaseAmountCommand { get; }

    private bool _isInCurrentRecipe;
    private decimal _buyAmount;
    private decimal _amountInRecipe;
}
