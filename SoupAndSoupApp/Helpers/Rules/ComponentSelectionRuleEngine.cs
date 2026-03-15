using System;
using System.Collections.Generic;
using System.Linq;
using DynamicData;
using SoupAndSoupApp.Models;

namespace SoupAndSoupApp.Helpers.Rules;

public interface IComponentSelectionRuleEngine
{
    void ApplySelectionRules(ComponentByRecipeModel added, SourceCache<ComponentByRecipeModel, int> selections,
        SourceCache<ComponentModel, int> masterComponents);
}

public class ComponentSelectionRuleEngine : IComponentSelectionRuleEngine
{
    private readonly IEnumerable<IComponentSelectionRule> _rules;

    public ComponentSelectionRuleEngine(IEnumerable<IComponentSelectionRule> rules)
    {
        _rules = rules;
    }

    public void ApplySelectionRules(ComponentByRecipeModel added, SourceCache<ComponentByRecipeModel, int> selections,
        SourceCache<ComponentModel, int> masterComponents)
    {
        var component = added.Component
            ?? masterComponents.Lookup(added.ComponentId).Value;

        var rule = _rules.FirstOrDefault(r => r.CanHandle(component.Type))
            ?? throw new ArgumentOutOfRangeException(nameof(added), component.Type, "No selection rule found for component type");

        rule.Apply(added, selections, masterComponents);
    }
}
