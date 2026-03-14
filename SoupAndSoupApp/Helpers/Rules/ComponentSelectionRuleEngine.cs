using SoupAndSoupApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SoupAndSoupApp.Helpers.Rules;

public interface IComponentSelectionRuleEngine
{
    void ApplySelectionRules(ComponentModel component, IReadOnlyCollection<ComponentModel> selectedComponents);
}

public class ComponentSelectionRuleEngine : IComponentSelectionRuleEngine
{
    private readonly IEnumerable<IComponentSelectionRule> _rules;

    public ComponentSelectionRuleEngine(IEnumerable<IComponentSelectionRule> rules)
    {
        _rules = rules;
    }

    public void ApplySelectionRules(ComponentModel component, IReadOnlyCollection<ComponentModel> selectedComponents)
    {
        var rule = _rules.FirstOrDefault(r => r.CanHandle(component.Type))
            ?? throw new ArgumentOutOfRangeException(nameof(component), component.Type, "No selection rule found for component type");

        rule.Apply(component, selectedComponents);
    }
}
