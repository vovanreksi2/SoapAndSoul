using System.ComponentModel.DataAnnotations;

namespace SoupAndSoup.Data.Models;

public class IngredientType
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; }

    [MaxLength(50)]
    public string ShortName { get; set; }

    public int Order { get; set; }

    public ICollection<AmountType> AmountTypes { get; set; }

    public ICollection<Ingredient> Ingredients { get; set; } = new List<Ingredient>();
}