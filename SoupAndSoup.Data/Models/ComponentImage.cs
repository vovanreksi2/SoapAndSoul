using System.ComponentModel.DataAnnotations;

namespace SoupAndSoup.Data.Models;

public class ComponentImage
{
    [Key]
    public int Id { get; set; }
    public int ComponentId { get; set; }

    [Required]
    public string ImageUrl { get; set; } = string.Empty;
    public string? AltText { get; set; }

    public Component? Component { get; set; }
}