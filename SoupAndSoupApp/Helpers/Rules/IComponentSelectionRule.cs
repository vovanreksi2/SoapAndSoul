using System.Collections.Generic;
using DynamicData;
using SoupAndSoupApp.Models;
using ComponentType = SoupAndSoupApp.Models.ComponentType;

namespace SoupAndSoupApp.Helpers.Rules;

public interface IComponentSelectionRule
{
    bool CanHandle(ComponentType type);
    void Apply(ComponentByRecipeModel added, SourceCache<ComponentByRecipeModel, int> selections,
        SourceCache<ComponentModel, int> masterComponents);
}
