using System.ComponentModel.DataAnnotations;

namespace SoupAndSoup.Data.Models;

public class Component
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; }

    public decimal Cost { get; set; }

    public bool IsActive { get; set; } = true;

    public int SuggestedAmount { get; set; }

    public int BuyAmount { get; set; }
    public decimal BuyPrice { get; set; }

    // Foreign key to ComponentTypesForUse
    public int ComponentTypeId { get; set; }
    public ComponentType ComponentType { get; set; }

    public int UseMeasureTypeId { get; set; }
    public MeasureType? UseMeasureType { get; set; } 

    public int BuyMeasureTypeId { get; set; }
    public MeasureType? BuyMeasureType { get; set; } 

    public ICollection<RecipeComponent> RecipeComponents { get; set; } = new List<RecipeComponent>();

    public ICollection<ComponentImage> Images { get; set; } = new List<ComponentImage>();
    
    public int CosmeticTypeId { get; set; }
    public CosmeticType CosmeticType { get; set; }
}