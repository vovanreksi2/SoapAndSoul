using System.ComponentModel.DataAnnotations;

namespace SoupAndSoup.Data.Models;

public  class CosmeticType
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; }

    public ICollection<ComponentType> ComponentTypes { get; set; }
}