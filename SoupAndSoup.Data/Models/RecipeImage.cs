using System.ComponentModel.DataAnnotations;

namespace SoupAndSoup.Data.Models;

public class RecipeImage
{
    [Key]
    public int Id { get; set; }
    public int RecipeId { get; set; }

    [Required]
    public string ImageUrl { get; set; } = string.Empty;
    public string? AltText { get; set; }

    public Recipe? Recipe { get; set; }
}