using System.ComponentModel.DataAnnotations;

namespace SoupAndSoup.Data.Models;

public class IngredientImage
{
    [Key]
    public int Id { get; set; }
    public int IngredientId { get; set; }

    [Required]
    public string ImageUrl { get; set; } = string.Empty;
    public string? AltText { get; set; }

    public Ingredient? Ingredient { get; set; }
}