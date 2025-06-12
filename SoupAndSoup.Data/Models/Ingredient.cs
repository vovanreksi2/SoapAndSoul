using System.ComponentModel.DataAnnotations;

namespace SoupAndSoup.Data.Models;

public class Ingredient
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; }

    public decimal Cost { get; set; }

    public bool IsActive { get; set; } = true;


    // Foreign key to IngredientTypes
    public int IngredientTypeId { get; set; }
    public IngredientType IngredientType { get; set; }


    public ICollection<AmountType> AmountTypes { get; set; } = new List<AmountType>();

    public ICollection<RecipeIngredient> RecipeIngredients { get; set; } = new List<RecipeIngredient>();

    public ICollection<IngredientImage> Images { get; set; } = new List<IngredientImage>();
}