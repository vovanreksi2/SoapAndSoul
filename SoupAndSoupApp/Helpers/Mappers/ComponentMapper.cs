using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using SoupAndSoup.Data.Models;
using SoupAndSoupApp.Helpers.Cache;
using SoupAndSoupApp.Helpers.Images;
using SoupAndSoupApp.Models;
using ComponentType = SoupAndSoupApp.Models.ComponentType;
using CosmeticType = SoupAndSoupApp.Models.CosmeticType;

namespace SoupAndSoupApp.Helpers.Mappers;

public class ComponentMapper(MeasureTypeCache measureTypeCache, IImageService imageService) : IComponentMapper
{
    public async Task<ComponentModel> MapToModelAsync(Component component, ICommand editCommand,
        ICommand deleteCommand, ICommand toggleInRecipeCommand, string noImageUrl)
    {
        var componentType = (ComponentType)component.ComponentTypeId;

        var result = new ComponentModel
        {
            Id = component.Id,
            Name = component.Name,
            Cost = component.Cost,

            SuggestedAmount = component.SuggestedAmount,
            BuyPrice = component.BuyPrice,
            BuyAmount = component.BuyAmount,

            DeleteCommand = deleteCommand,
            EditCommand = editCommand,
            ToggleInRecipeCommand = toggleInRecipeCommand,
            ShowAmountInButton = componentType != ComponentType.Form,
            Type = componentType,

            BuyMeasureTypeId = component.BuyMeasureTypeId,
            UseMeasureTypeId = component.UseMeasureTypeId,
            BuyMeasureTypeShortTitle = (await measureTypeCache.GetOrAddAsync(component.BuyMeasureTypeId)).ShortTitle,
            UseMeasureTypeShortTitle = (await measureTypeCache.GetOrAddAsync(component.UseMeasureTypeId)).ShortTitle,

            ImagePathString = component.Images.FirstOrDefault()?.ImageUrl,
        };

        result.ImagePath = await imageService.LoadImageOrDefaultAsync(result.ImagePathString, result.Id, noImageUrl);

        return result;
    }

    public Component MapToEntity(NewComponentDto dto, ComponentType type,
        CosmeticType cosmeticType, int? existingId = null)
    {
        var component = new Component
        {
            Cost = dto.Cost,
            Name = dto.Name,
            ComponentTypeId = (int)type,
            UseMeasureTypeId = dto.UseMeasureType.Id,
            BuyMeasureTypeId = dto.BuyMeasureType.Id,
            SuggestedAmount = (int)dto.SuggestedAmount,
            BuyAmount = (int)dto.BuyAmount,
            BuyPrice = dto.BuyPrice,
            CosmeticTypeId = (int)cosmeticType
        };

        if (existingId.HasValue)
            component.Id = existingId.Value;

        if (!string.IsNullOrEmpty(dto.ImagePath))
            component.Images.Add(new ComponentImage { ImageUrl = dto.ImagePath });

        return component;
    }
}
