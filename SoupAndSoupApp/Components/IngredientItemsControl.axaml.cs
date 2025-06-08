using System.Collections.ObjectModel;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using SoupAndSoupApp.Models;

namespace SoupAndSoupApp.Components;

public partial class IngredientItemsControl : UserControl
{
    public static readonly StyledProperty<ObservableCollection<IngredientModel>> IngredientsProperty =
        AvaloniaProperty.Register<IngredientItemsControl, ObservableCollection<IngredientModel>>(nameof(Ingredients));
    public ObservableCollection<IngredientModel> Ingredients
    {
        get => GetValue(IngredientsProperty);
        set => SetValue(IngredientsProperty, value);
    }

    public static readonly StyledProperty<string> GroupTitleProperty =
        AvaloniaProperty.Register<IngredientItemsControl, string>(nameof(GroupTitle));
    public string GroupTitle
    {
        get => GetValue(GroupTitleProperty);
        set => SetValue(GroupTitleProperty, value);
    }

    public static readonly StyledProperty<ICommand> NewItemCommandProperty =
        AvaloniaProperty.Register<IngredientItemsControl, ICommand>(nameof(NewItemCommandProperty));
    public ICommand NewItemCommand
    {
        get => GetValue(NewItemCommandProperty);
        set => SetValue(NewItemCommandProperty, value);
    }

    public static readonly StyledProperty<object> CommandParameterProperty =
        AvaloniaProperty.Register<IngredientItemsControl, object>(nameof(CommandParameterProperty));
    public object CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    public static readonly StyledProperty<SoapTypeComponent> TypeProperty =
        AvaloniaProperty.Register<IngredientItemsControl, SoapTypeComponent>(nameof(Type));
    public SoapTypeComponent Type
    {
        get => GetValue(TypeProperty);
        set => SetValue(TypeProperty, value);
    }

    public IngredientItemsControl()
    {
        InitializeComponent();
        IngredientsProperty.Changed.AddClassHandler<IngredientItemsControl>((x, e) => x.OnIngredientsChanged(e));
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnIngredientsChanged(AvaloniaPropertyChangedEventArgs e)
    {
    }
}