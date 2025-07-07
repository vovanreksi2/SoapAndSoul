using System;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace SoupAndSoupApp.Components;

public partial class RecipeMetricsUserControl : UserControl
{
    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<RecipeMetricsUserControl, string>(nameof(Title));
    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }


    public static readonly StyledProperty<decimal> ValueProperty =
        AvaloniaProperty.Register<RecipeMetricsUserControl, decimal>(nameof(Value), defaultBindingMode: BindingMode.TwoWay);
    public decimal Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }


    public static readonly StyledProperty<bool> IsEditingProperty =
        AvaloniaProperty.Register<RecipeMetricsUserControl, bool>(nameof(IsEditing));
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


    public static readonly StyledProperty<string> StringFormatProperty =
        AvaloniaProperty.Register<RecipeMetricsUserControl, string>(nameof(StringFormat));
    public string StringFormat
    {
        get => GetValue(StringFormatProperty);
        set => SetValue(StringFormatProperty, value);
    }


    public static readonly StyledProperty<string> FormattedValueProperty =
        AvaloniaProperty.Register<RecipeMetricsUserControl, string>(nameof(FormattedValue), defaultValue: string.Empty);
    public string FormattedValue
    {
        get => GetValue(FormattedValueProperty);
        private set => SetValue(FormattedValueProperty, value);
    }

    public static readonly StyledProperty<bool> IsReadOnlyProperty = 
        AvaloniaProperty.Register<RecipeMetricsUserControl, bool>(nameof(IsReadOnly), defaultValue: false);
    public bool IsReadOnly
    {
        get => GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }

    public RecipeMetricsUserControl()
    {
        InitializeComponent();

        this.GetObservable(ValueProperty).Subscribe(_ => UpdateFormattedValue());
        this.GetObservable(StringFormatProperty).Subscribe(_ => UpdateFormattedValue());
        UpdateFormattedValue();
    }

    private void UpdateFormattedValue()
    {
        try
        {
            FormattedValue = string.Format(CultureInfo.InvariantCulture, StringFormat, Value);
        }
        catch (FormatException)
        {
            FormattedValue = "Format Error";
        }
        catch (Exception ex)
        {
            FormattedValue = $"Error: {ex.Message}";
        }
    }

    private void DisplayTextBlock_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (IsReadOnly) return; // Do not allow editing if read-only
        
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

    private void EditTextBox_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        
        IsEditing = false;
        e.Handled = true;
    }
}