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

    /// <summary>
    /// Reference to the master component data. Populated when the recipe is active.
    /// </summary>
    public ComponentModel? Component { get; set; }

    private decimal _amount;
}