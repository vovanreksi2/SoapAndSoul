using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;

namespace SoupAndSoupApp.Components;

public partial class EditableTextBlock : UserControl
{
    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<EditableTextBlock, string>(nameof(Text), defaultBindingMode: BindingMode.TwoWay);

    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public static readonly StyledProperty<bool> IsEditingProperty =
        AvaloniaProperty.Register<EditableTextBlock, bool>(nameof(IsEditing));
    public bool IsEditing
    {
        get => GetValue(IsEditingProperty);
        private set
        {
            SetValue(IsEditingProperty, value);

            if (value)
            {
                // When entering editing mode, subscribe to global pointer events
                // to detect clicks outside this control.
                if (this.GetVisualRoot() is InputElement visualRoot)
                {
                    visualRoot.AddHandler(PointerPressedEvent, OnGlobalPointerPressed, RoutingStrategies.Tunnel);
                }

                // Defer focus to allow UI to update
                this.FindControl<TextBox>("EditTextBox")?.Focus();
            }
            else
            {
                // When exiting editing mode, unsubscribe from global pointer events.
                if (this.GetVisualRoot() is InputElement visualRoot)
                {
                    visualRoot.RemoveHandler(PointerPressedEvent, OnGlobalPointerPressed);
                }
            }
        }
    }

    public EditableTextBlock()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        // Load the AXML defined in the EditableTextBlock.axaml file
        AvaloniaXamlLoader.Load(this);
    }

    private void DisplayTextBlock_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;

        IsEditing = true;
        e.Handled = true; // Mark as handled so it doesn't propagate further unnecessarily
    }

    private void OnGlobalPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        // Get the visual element that was the source of the pointer event.
        // This is the element directly under the mouse when the click occurred.
        if (e.Source is not Visual visualSource) return;
        // Check if the clicked element (visualSource) is NOT a descendant of THIS EditableTextBlock.
        // The GetSelfAndVisualDescendants() extension method from Avalonia.VisualTree
        // provides an enumerable of the current visual and all its visual descendants.
        // We then check if the visualSource is contained within that set.
        if (!this.GetSelfAndVisualDescendants().Contains(visualSource))
        {
            // The click was outside this EditableTextBlock.
            IsEditing = false;
            // It's generally good practice to mark the event as handled if you've
            // consumed it in a way that prevents further processing for this specific
            // interaction. However, be cautious as handling here might prevent other
            // controls from getting their PointerPressed events if they are
            // also listening to the same global event. For this specific "click outside"
            // scenario, it's often fine to leave it unhandled if other elements
            // are expecting to respond to clicks.
            // e.Handled = true;
        }
    }
 
    private void EditTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;

        IsEditing = false;
        e.Handled = true; // Consume the Enter key event
    }
}
