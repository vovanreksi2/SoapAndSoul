using System.Windows.Input;
using Avalonia.Media.Imaging;
using ReactiveUI;

namespace SoupAndSoupApp.Models;

public class IngredientModel : ReactiveObject
{
    public int Id { get; set; }

    public Bitmap ImagePath { get; set; }
    public string Name { get; set; }
    public decimal Amount { get; set; }
    public decimal AmountTitle { get; set; }
    public decimal Cost { get; set; }

    public bool IsButton { get; set; }

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => this.RaiseAndSetIfChanged(ref _isSelected, value);
    }

    public ICommand EditCommand { get; set; }
    public ICommand DeleteCommand { get; set; }
}