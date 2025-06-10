using System.Collections.ObjectModel;
using System.Windows.Input;
using ReactiveUI;

namespace SoupAndSoupApp.Models;

public class ComponentGroup: ReactiveObject
{
    public SoapTypeComponent Type { get; set; }

    public string Title { get; set; } = string.Empty;
    public string ShortTitle { get; set; } = string.Empty;
    public ObservableCollection<IngredientModel> Components { get; set; } = new()
    {
        new IngredientModel
        {
            IsButton = true,
            IsSelected = false
        }
    };

    public ICommand NewComponentCommand { get; set; }
}