using System.Windows.Input;
using DynamicData.Binding;
using ReactiveUI;

namespace SoupAndSoupApp.Models;

public class ComponentGroup : ReactiveObject
{
    public ICommand NewComponentCommand { get; set; }

    public ComponentTypeModel ComponentType { get; set; } 

    public IObservableCollection<ComponentModel> Components { get; set; }
}