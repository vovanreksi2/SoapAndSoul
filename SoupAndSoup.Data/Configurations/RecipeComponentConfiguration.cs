using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoupAndSoup.Data.Models;

namespace SoupAndSoup.Data.Configurations;

public class RecipeComponentConfiguration : IEntityTypeConfiguration<RecipeComponent>
{
    public void Configure(EntityTypeBuilder<RecipeComponent> builder)
    {
        builder.HasKey(ri => new { RecipeID = ri.RecipeId, ComponentID = ri.ComponentId });

        builder.HasOne(ri => ri.Recipe)
            .WithMany(r => r.RecipeComponents)
            .HasForeignKey(ri => ri.RecipeId);

        builder.HasOne(ri => ri.Component)
            .WithMany(i => i.RecipeComponents)
            .HasForeignKey(ri => ri.ComponentId);
    }
}
