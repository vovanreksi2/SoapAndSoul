using System.Collections.Generic;
using System.Linq;
using DynamicData;
using SoupAndSoupApp.Models;
using ComponentType = SoupAndSoupApp.Models.ComponentType;
using static SoupAndSoupApp.Helpers.Rules.SelectionRuleHelpers;

namespace SoupAndSoupApp.Helpers.Rules;

/// <summary>
/// When a Form is selected, deselect all other Forms and set the CraftingBase amount
/// to the Form's suggested amount.
/// </summary>
public class FormSelectionRule : IComponentSelectionRule
{
    public bool CanHandle(ComponentType type) => type == ComponentType.Form;

    public void Apply(ComponentByRecipeModel added, SourceCache<ComponentByRecipeModel, int> selections,
        SourceCache<ComponentModel, int> masterComponents)
    {
        var addedComponent = ResolveComponent(added, masterComponents);
        if (addedComponent is null) return;

        // Deselect other Forms — remove from recipe selections
        var otherForms = selections.Items
            .Where(s =>
            {
                var comp = ResolveComponent(s, masterComponents);
                return comp?.Type == ComponentType.Form && s.ComponentId != added.ComponentId;
            })
            .ToList();
        selections.RemoveKeys(otherForms.Select(f => f.ComponentId));

        // Set CraftingBase amount to the Form's suggested amount
        var craftingBase = selections.Items
            .FirstOrDefault(s => ResolveComponent(s, masterComponents)?.Type == ComponentType.CraftingBase);
        if (craftingBase != null)
            craftingBase.Amount = addedComponent.SuggestedAmount;
    }
}

/// <summary>
/// When an EssentialOil is selected, deselect all FragranceOils and set the amount
/// to the suggested amount.
/// </summary>
public class EssentialOilSelectionRule : IComponentSelectionRule
{
    public bool CanHandle(ComponentType type) => type == ComponentType.EssentialOil;

    public void Apply(ComponentByRecipeModel added, SourceCache<ComponentByRecipeModel, int> selections,
        SourceCache<ComponentModel, int> masterComponents)
    {
        var addedComponent = ResolveComponent(added, masterComponents);
        if (addedComponent is null) return;

        // Deselect all FragranceOils
        var fragranceOils = selections.Items
            .Where(s =>
            {
                var comp = ResolveComponent(s, masterComponents);
                return comp?.Type == ComponentType.FragranceOil && s.ComponentId != added.ComponentId;
            })
            .ToList();
        selections.RemoveKeys(fragranceOils.Select(f => f.ComponentId));

        added.Amount = addedComponent.SuggestedAmount;
    }
}

/// <summary>
/// When a FragranceOil is selected, deselect all EssentialOils and set the amount
/// to the suggested amount.
/// </summary>
public class FragranceOilSelectionRule : IComponentSelectionRule
{
    public bool CanHandle(ComponentType type) => type == ComponentType.FragranceOil;

    public void Apply(ComponentByRecipeModel added, SourceCache<ComponentByRecipeModel, int> selections,
        SourceCache<ComponentModel, int> masterComponents)
    {
        var addedComponent = ResolveComponent(added, masterComponents);
        if (addedComponent is null) return;

        // Deselect all EssentialOils
        var essentialOils = selections.Items
            .Where(s =>
            {
                var comp = ResolveComponent(s, masterComponents);
                return comp?.Type == ComponentType.EssentialOil && s.ComponentId != added.ComponentId;
            })
            .ToList();
        selections.RemoveKeys(essentialOils.Select(f => f.ComponentId));

        added.Amount = addedComponent.SuggestedAmount;
    }
}

/// <summary>
/// When a CraftingBase is selected, its amount is set to the Form's suggested amount
/// if a Form is selected, otherwise to its own suggested amount.
/// </summary>
public class CraftingBaseSelectionRule : IComponentSelectionRule
{
    public bool CanHandle(ComponentType type) => type == ComponentType.CraftingBase;

    public void Apply(ComponentByRecipeModel added, SourceCache<ComponentByRecipeModel, int> selections,
        SourceCache<ComponentModel, int> masterComponents)
    {
        var addedComponent = ResolveComponent(added, masterComponents);
        if (addedComponent is null) return;

        var form = selections.Items
            .FirstOrDefault(s => ResolveComponent(s, masterComponents)?.Type == ComponentType.Form);

        var formComponent = form is not null ? ResolveComponent(form, masterComponents) : null;
        added.Amount = formComponent?.SuggestedAmount ?? addedComponent.SuggestedAmount;
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

    public void Apply(ComponentByRecipeModel added, SourceCache<ComponentByRecipeModel, int> selections,
        SourceCache<ComponentModel, int> masterComponents)
    {
        var addedComponent = ResolveComponent(added, masterComponents);
        if (addedComponent is null) return;

        added.Amount = addedComponent.SuggestedAmount;
    }
}

/// <summary>
/// Shared helpers for all selection rules.
/// </summary>
internal static class SelectionRuleHelpers
{
    /// <summary>
    /// Returns the resolved <see cref="ComponentModel"/> for a selection entry,
    /// preferring the inline reference and falling back to the master cache lookup.
    /// Returns <c>null</c> when the component cannot be found in either source.
    /// </summary>
    internal static ComponentModel? ResolveComponent(
        ComponentByRecipeModel selection,
        SourceCache<ComponentModel, int> masterComponents)
    {
        if (selection.Component is not null) return selection.Component;
        var lookup = masterComponents.Lookup(selection.ComponentId);
        return lookup.HasValue ? lookup.Value : null;
    }
}
