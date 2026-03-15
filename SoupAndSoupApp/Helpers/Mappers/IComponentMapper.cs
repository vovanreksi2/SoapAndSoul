using System.Threading.Tasks;
using System.Windows.Input;
using SoupAndSoup.Data.Models;
using SoupAndSoupApp.Models;
using ComponentType = SoupAndSoupApp.Models.ComponentType;
using CosmeticType = SoupAndSoupApp.Models.CosmeticType;

namespace SoupAndSoupApp.Helpers.Mappers;

public interface IComponentMapper
{
    Task<ComponentModel> MapToModelAsync(Component component, ICommand editCommand,
        ICommand deleteCommand, ICommand toggleInRecipeCommand, string noImageUrl);

    Component MapToEntity(NewComponentDto dto, ComponentType type,
        CosmeticType cosmeticType, int? existingId = null);
}
