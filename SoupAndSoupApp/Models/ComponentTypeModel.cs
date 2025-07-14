using System.Collections.Generic;
using System.Linq;

namespace SoupAndSoupApp.Models;

public class ComponentTypeModel
{
    public ComponentTypeModel(SoupAndSoup.Data.Models.ComponentType componentType)
    {
        Type = (ComponentType)componentType.Id;
        Title = componentType.Name;
        ShortTitle = componentType.ShortName;
        Order = componentType.Order;
        BuyAmount = componentType.BuyAmount;
        IsSingleSelected = componentType.IsSingleSelected;

        //TODO
        UseMeasureTypesId = componentType.UseMeasureTypes.Select(m => m.Id);
        BuyMeasureTypesId = componentType.BuyMeasureTypes.Select(m => m.Id);
    }

    public ComponentType Type { get; set; }
    public string Title { get; set; }
    public string ShortTitle { get; set; }
    public int BuyAmount { get; set; }
    public int Order { get; set; }
    public bool IsSingleSelected { get; set; }

    public IEnumerable<int> UseMeasureTypesId { get; set; }
    public IEnumerable<int> BuyMeasureTypesId { get; set; }
}