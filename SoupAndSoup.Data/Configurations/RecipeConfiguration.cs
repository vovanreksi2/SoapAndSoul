using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoupAndSoup.Data.Models;

namespace SoupAndSoup.Data.Configurations;

public class RecipeConfiguration : IEntityTypeConfiguration<Recipe>
{
    public void Configure(EntityTypeBuilder<Recipe> builder)
    {
        builder.HasIndex(r => r.Name);
        builder.HasQueryFilter(r => r.IsActive);
    }
}
