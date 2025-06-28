using System.ComponentModel.DataAnnotations;

namespace SoupAndSoup.Data.Models;

public class Recipe
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; }

    public string Description { get; set; }

    [Required, MaxLength(50)]
    public string Type { get; set; } 

    public DateTime DateOfCreate { get; set; }

    public decimal Amount { get; set; }

    public TimeSpan PreparationTime { get; set; }

    public bool IsActive { get; set; }

    public long Version { get; set; }

    public ICollection<RecipeComponent> RecipeComponents { get; set; }
        
    public ICollection<RecipeImage> Images { get; set; } 

    public Recipe()
    {
        IsActive = true;
        Version = 1;
        DateOfCreate = DateTime.UtcNow;

        RecipeComponents = new List<RecipeComponent>();
        Images = new List<RecipeImage>();
    }
}