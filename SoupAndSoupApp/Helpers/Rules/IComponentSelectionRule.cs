using System.Collections.Generic;
using SoupAndSoupApp.Models;
using ComponentType = SoupAndSoupApp.Models.ComponentType;

namespace SoupAndSoupApp.Helpers.Rules;

public interface IComponentSelectionRule
{
    bool CanHandle(ComponentType type);
    void Apply(ComponentModel component, IReadOnlyCollection<ComponentModel> selectedComponents);
}
