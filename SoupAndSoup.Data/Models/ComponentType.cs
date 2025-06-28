using System.ComponentModel.DataAnnotations;

namespace SoupAndSoup.Data.Models;

//Type like: CraftingBase, SoapForm, ParfumForm...
public class ComponentType
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; }

    [MaxLength(50)]
    public string ShortName { get; set; }

    public int BuyAmount { get; set; }

    public int Order { get; set; }

    public bool IsSingleSelected { get; set; }

    //Used for Soap, Parfum
    public ICollection<CosmeticType> CosmeticTypes { get; set; }

    public ICollection<MeasureType> UseMeasureTypes { get; set; }
    
    public ICollection<MeasureType> BuyMeasureTypes { get; set; }

    public ICollection<Component> Components { get; set; } = new List<Component>();
}