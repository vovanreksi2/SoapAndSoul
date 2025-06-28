using System.ComponentModel.DataAnnotations;

namespace SoupAndSoup.Data.Models;

public class MeasureType
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string Name { get; set; }

    [MaxLength(20)]
    public string ShortName { get; set; }

    public ICollection<ComponentType> ComponentTypesForUse { get; set; }
    public ICollection<ComponentType> ComponentTypesForBuy { get; set; }
    
    public ICollection<Component> ComponentsUsingAsUseMeasureType { get; set; }
    public ICollection<Component> ComponentsUsingAsBuyMeasureType { get; set; }
}