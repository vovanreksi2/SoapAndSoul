using System.ComponentModel.DataAnnotations;

namespace SoupAndSoup.Data.Models;

public class AmountType
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string Name { get; set; }

    [MaxLength(20)]
    public string ShortName { get; set; }

    public ICollection<IngredientType> IngredientTypes { get; set; }
    public ICollection<Ingredient> Ingredients { get; set; }
}