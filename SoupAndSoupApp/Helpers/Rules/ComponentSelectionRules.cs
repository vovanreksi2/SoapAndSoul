using System.Collections.Generic;
using System.Linq;
using SoupAndSoupApp.Models;
using ComponentType = SoupAndSoupApp.Models.ComponentType;

namespace SoupAndSoupApp.Helpers.Rules;

/// <summary>
/// When a Form is selected, deselect all other Forms and set the CraftingBase amount
/// to the Form's suggested amount.
/// </summary>
public class FormSelectionRule : IComponentSelectionRule
{
    public bool CanHandle(ComponentType type) => type == ComponentType.Form;

    public void Apply(ComponentModel component, IReadOnlyCollection<ComponentModel> selectedComponents)
    {
        DeselectOthers(selectedComponents, component, ComponentType.Form);

        var craftingBase = selectedComponents.FirstOrDefault(c => c.Type == ComponentType.CraftingBase);
        craftingBase?.AmountInRecipe = component.SuggestedAmount;
    }

    private static void DeselectOthers(IEnumerable<ComponentModel> components, ComponentModel current, ComponentType type)
    {
        foreach (var c in components.Where(c => c.Type == type && c.Id != current.Id))
            c.IsSelected = false;
    }
}

/// <summary>
/// When an EssentialOil is selected, deselect all FragranceOils and set the amount
/// to the suggested amount.
/// </summary>
public class EssentialOilSelectionRule : IComponentSelectionRule
{
    public bool CanHandle(ComponentType type) => type == ComponentType.EssentialOil;

    public void Apply(ComponentModel component, IReadOnlyCollection<ComponentModel> selectedComponents)
    {
        foreach (var c in selectedComponents.Where(c => c.Type == ComponentType.FragranceOil && c.Id != component.Id))
            c.IsSelected = false;

        component.AmountInRecipe = component.SuggestedAmount;
    }
}

/// <summary>
/// When a FragranceOil is selected, deselect all EssentialOils and set the amount
/// to the suggested amount.
/// </summary>
public class FragranceOilSelectionRule : IComponentSelectionRule
{
    public bool CanHandle(ComponentType type) => type == ComponentType.FragranceOil;

    public void Apply(ComponentModel component, IReadOnlyCollection<ComponentModel> selectedComponents)
    {
        foreach (var c in selectedComponents.Where(c => c.Type == ComponentType.EssentialOil && c.Id != component.Id))
            c.IsSelected = false;

        component.AmountInRecipe = component.SuggestedAmount;
    }
}

/// <summary>
/// When a CraftingBase is selected, its amount is set to the Form's suggested amount
/// if a Form is selected, otherwise to its own suggested amount.
/// </summary>
public class CraftingBaseSelectionRule : IComponentSelectionRule
{
    public bool CanHandle(ComponentType type) => type == ComponentType.CraftingBase;

    public void Apply(ComponentModel component, IReadOnlyCollection<ComponentModel> selectedComponents)
    {
        var form = selectedComponents.FirstOrDefault(c => c.Type == ComponentType.Form);
        component.AmountInRecipe = form != null && component.IsSelected
            ? form.SuggestedAmount
            : component.SuggestedAmount;
    }
}

/// <summary>
/// Default rule for component types that simply use their suggested amount.
/// Handles: Pigment, HerbalExtract, Tools, Other.
/// </summary>
public class DefaultAmountSelectionRule : IComponentSelectionRule
{
    private static readonly ComponentType[] HandledTypes =
    [
        ComponentType.Pigment,
        ComponentType.HerbalExtract,
        ComponentType.Tools,
        ComponentType.Other
    ];

    public bool CanHandle(ComponentType type) => HandledTypes.Contains(type);

    public void Apply(ComponentModel component, IReadOnlyCollection<ComponentModel> selectedComponents)
    {
        component.AmountInRecipe = component.SuggestedAmount;
    }
}
