using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoupAndSoup.Data.Models;

namespace SoupAndSoup.Data.Configurations;

public class ComponentConfiguration : IEntityTypeConfiguration<Component>
{
    public void Configure(EntityTypeBuilder<Component> builder)
    {
        builder.HasIndex(i => i.Name);
        builder.HasIndex(i => i.ComponentTypeId);
        builder.HasIndex(i => i.UseMeasureTypeId);

        builder.HasQueryFilter(i => i.IsActive);

        builder.HasOne(i => i.ComponentType)
            .WithMany(it => it.Components)
            .HasForeignKey(i => i.ComponentTypeId);

        builder.HasOne(i => i.UseMeasureType)
            .WithMany(at => at.ComponentsUsingAsUseMeasureType)
            .HasForeignKey(i => i.UseMeasureTypeId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(i => i.BuyMeasureType)
            .WithMany(at => at.ComponentsUsingAsBuyMeasureType)
            .HasForeignKey(i => i.BuyMeasureTypeId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
