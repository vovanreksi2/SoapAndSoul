using ReactiveUI;

namespace SoupAndSoupApp.Models;

public class ComponentByRecipeModel: ReactiveObject
{
    public int ComponentId { get; set; }

    public decimal Amount
    {
        get => _amount;
        set => this.RaiseAndSetIfChanged(ref _amount, value);
    }
 
    private decimal _amount;
}