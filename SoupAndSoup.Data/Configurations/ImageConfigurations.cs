using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoupAndSoup.Data.Models;

namespace SoupAndSoup.Data.Configurations;

public class RecipeImageConfiguration : IEntityTypeConfiguration<RecipeImage>
{
    public void Configure(EntityTypeBuilder<RecipeImage> builder)
    {
        builder.HasKey(r => r.Id);
        builder.HasIndex(r => r.RecipeId);

        builder.HasOne(r => r.Recipe)
            .WithMany(r => r.Images)
            .HasForeignKey(r => r.RecipeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ComponentImageConfiguration : IEntityTypeConfiguration<ComponentImage>
{
    public void Configure(EntityTypeBuilder<ComponentImage> builder)
    {
        builder.HasKey(i => i.Id);
        builder.HasIndex(i => i.ComponentId);

        builder.HasOne(i => i.Component)
            .WithMany(i => i.Images)
            .HasForeignKey(i => i.ComponentId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
