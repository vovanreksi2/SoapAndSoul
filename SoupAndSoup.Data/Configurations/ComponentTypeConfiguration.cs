using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoupAndSoup.Data.Models;

namespace SoupAndSoup.Data.Configurations;

public class ComponentTypeConfiguration : IEntityTypeConfiguration<ComponentType>
{
    public void Configure(EntityTypeBuilder<ComponentType> builder)
    {
        builder.HasMany(i => i.UseMeasureTypes)
            .WithMany(at => at.ComponentTypesForUse)
            .UsingEntity<Dictionary<string, object>>(
                "ComponentTypeUseMeasureTypes",
                j => j
                    .HasOne<MeasureType>()
                    .WithMany()
                    .HasForeignKey("MeasureTypesId")
                    .OnDelete(DeleteBehavior.Restrict),
                j => j
                    .HasOne<ComponentType>()
                    .WithMany()
                    .HasForeignKey("ComponentTypesId")
                    .OnDelete(DeleteBehavior.Restrict),
                j =>
                {
                    j.HasKey("ComponentTypesId", "MeasureTypesId");
                    j.ToTable("ComponentTypeUseMeasureTypes");
                });

        builder.HasMany(i => i.BuyMeasureTypes)
            .WithMany(at => at.ComponentTypesForBuy)
            .UsingEntity<Dictionary<string, object>>(
                "ComponentTypeBuyMeasureTypes",
                j => j
                    .HasOne<MeasureType>()
                    .WithMany()
                    .HasForeignKey("MeasureTypesId")
                    .OnDelete(DeleteBehavior.Restrict),
                j => j
                    .HasOne<ComponentType>()
                    .WithMany()
                    .HasForeignKey("ComponentTypesId")
                    .OnDelete(DeleteBehavior.Restrict),
                j =>
                {
                    j.HasKey("ComponentTypesId", "MeasureTypesId");
                    j.ToTable("ComponentTypeBuyMeasureTypes");
                });

        builder.HasMany(i => i.CosmeticTypes)
            .WithMany(at => at.ComponentTypes)
            .UsingEntity<Dictionary<string, object>>(
                "ComponentTypeCosmeticTypes",
                j => j
                    .HasOne<CosmeticType>()
                    .WithMany()
                    .HasForeignKey("CosmeticTypesId")
                    .OnDelete(DeleteBehavior.Cascade),
                j => j
                    .HasOne<ComponentType>()
                    .WithMany()
                    .HasForeignKey("ComponentTypesId")
                    .OnDelete(DeleteBehavior.Cascade),
                j =>
                {
                    j.HasKey("ComponentTypesId", "CosmeticTypesId");
                    j.ToTable("ComponentTypeCosmeticTypes");
                });
    }
}
