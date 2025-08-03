using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using SoupAndSoup.Data.Models;

namespace SoupAndSoup.Data;

public class SoapAndSoulContext : DbContext
{
    public DbSet<Recipe> Recipes { get; set; }
    public DbSet<Component> Components { get; set; }
    public DbSet<ComponentType> ComponentTypes { get; set; }
    public DbSet<MeasureType> MeasureTypes { get; set; }
    public DbSet<CosmeticType> CosmeticTypes { get; set; }
    public DbSet<RecipeComponent> RecipeComponents { get; set; }
    public DbSet<ComponentImage> ComponentImages { get; set; }
    public DbSet<RecipeImage> RecipeImages { get; set; }

    public SoapAndSoulContext(DbContextOptions<SoapAndSoulContext> options)
        : base(options)
    {
        Debug.WriteLine("==> Using DB: " + Database.GetDbConnection().ConnectionString);
        Database.Migrate();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Index on Recipe.Name for fast searching
        modelBuilder.Entity<Recipe>(entity =>
        {
            entity.HasIndex(r => r.Name);
            entity.HasQueryFilter(r => r.IsActive);
        });

        modelBuilder.Entity<Component>(entity =>
        {
            // Indexes on Component.Name and Component.ComponentTypeID
            entity.HasIndex(i => i.Name);
            entity.HasIndex(i => i.ComponentTypeId);
            entity.HasIndex(i => i.UseMeasureTypeId);

            entity.HasQueryFilter(i => i.IsActive); // Filter for active components

            // Relationship: Component -> ComponentTypesForUse
            entity.HasOne(i => i.ComponentType)
                .WithMany(it => it.Components)
                .HasForeignKey(i => i.ComponentTypeId);

            // Relationship: Component -> UseMeasureTypes
            entity.HasOne(i=>i.UseMeasureType)
                .WithMany(at => at.ComponentsUsingAsUseMeasureType)
                .HasForeignKey(i => i.UseMeasureTypeId)
                .OnDelete(DeleteBehavior.NoAction);

            // Relationship: Component -> BuyMeasureTypes
            entity.HasOne(i=>i.BuyMeasureType)
                .WithMany(at => at.ComponentsUsingAsBuyMeasureType)
                .HasForeignKey(i => i.BuyMeasureTypeId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<RecipeComponent>(entity =>
        {
            // Composite primary key for RecipeComponent (many-to-many)
            entity.HasKey(ri => new { RecipeID = ri.RecipeId, ComponentID = ri.ComponentId });

            // Relationship: RecipeComponent -> Recipe
            entity.HasOne(ri => ri.Recipe)
                .WithMany(r => r.RecipeComponents)
                .HasForeignKey(ri => ri.RecipeId);

            // Relationship: RecipeComponent -> Component
            entity.HasOne(ri => ri.Component)
                .WithMany(i => i.RecipeComponents)
                .HasForeignKey(ri => ri.ComponentId);
        });

        modelBuilder.Entity<ComponentType>(entity =>
        {
            // Relationship: ComponentTypesForUse -> UseMeasureType
            entity.HasMany(i => i.UseMeasureTypes)
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

            // Relationship: ComponentTypesForUse -> UseMeasureType
            entity.HasMany(i => i.BuyMeasureTypes)
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

            // Relationship: ComponentTypesForUse -> CosmeticType 
            entity.HasMany(i => i.CosmeticTypes)
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
        });

        // Index on Images by EntityType and EntityID for fast lookup
        modelBuilder.Entity<RecipeImage>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.HasIndex(r => r.RecipeId);

            entity.HasOne(r => r.Recipe)
                .WithMany(r => r.Images)
                .HasForeignKey(r => r.RecipeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ComponentImage>(entity =>
        {
            entity.HasKey(i => i.Id);
            entity.HasIndex(i => i.ComponentId);

            entity.HasOne(i => i.Component)
                .WithMany(i => i.Images)
                .HasForeignKey(i => i.ComponentId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Cascade);
        });

        SeedData(modelBuilder);

        base.OnModelCreating(modelBuilder);
    }

    private void SeedData(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CosmeticType>().HasData(
            new CosmeticType{Id = 1, Name = "Мило"},
            new CosmeticType{Id = 2, Name = "Духи"},
            new CosmeticType{Id = 3, Name = "Всі"}
        );

        modelBuilder.Entity<MeasureType>().HasData(
            new MeasureType { Id = 1, Name = "Грам", ShortName = "г" },
            new MeasureType { Id = 2, Name = "Мілілітр", ShortName = "мл" },
            new MeasureType { Id = 3, Name = "Краплі", ShortName = "крап" },
            new MeasureType { Id = 4, Name = "Штука", ShortName = "шт" }
        );

        modelBuilder.Entity<ComponentType>().HasData(
            new ComponentType { Id = 1, Name = "Форма для мила", ShortName = "форму", Order = 1,  BuyAmount = 1, IsSingleSelected = true},
            new ComponentType { Id = 2, Name = "Мильна основа", ShortName = "основу", Order = 2, BuyAmount = 200 },
            new ComponentType { Id = 3, Name = "Запашка", ShortName = "запашку", Order = 3, BuyAmount = 10 },
            new ComponentType { Id = 4, Name = "Пігмент", ShortName = "пігмент", Order = 4, BuyAmount = 10 },
            new ComponentType { Id = 5, Name = "Ефірне масло", ShortName = "ефірне масло", Order = 5, BuyAmount = 10 },
            new ComponentType { Id = 6, Name = "Екстракт", ShortName = "екстракт", Order = 6 , BuyAmount = 10 },
            new ComponentType { Id = 7, Name = "Інструменти", ShortName = "інструмент", Order = 7, BuyAmount = 1 },
            new ComponentType { Id = 8, Name = "Інші", ShortName = "", Order = 8 , BuyAmount = 1 },

            new ComponentType { Id = 9, Name = "Флакон для парфумів", ShortName = "флакон", Order = 1, BuyAmount = 1 , IsSingleSelected = true},
            new ComponentType { Id = 10, Name = "Основа для парфумів", ShortName = "основу", Order = 2 , BuyAmount = 500 }
        );

        modelBuilder.Entity("ComponentTypeUseMeasureTypes").HasData(
            new { ComponentTypesId = 1, MeasureTypesId = 4 }, // Форма - шт
            new { ComponentTypesId = 2, MeasureTypesId = 1 }, // Основа - г
            new { ComponentTypesId = 3, MeasureTypesId = 3 }, // Запашка - мл
            new { ComponentTypesId = 4, MeasureTypesId = 3 }, // Пігмент - крап
            new { ComponentTypesId = 4, MeasureTypesId = 1 }, // Пігмент - крап
            new { ComponentTypesId = 5, MeasureTypesId = 3 }, // Еф.масло - крап
            new { ComponentTypesId = 6, MeasureTypesId = 3 }, // Екстракт - крап
            new { ComponentTypesId = 7, MeasureTypesId = 4 }, // Інструменти - шт
            new { ComponentTypesId = 8, MeasureTypesId = 4 }, // Інші - шт
            
            new { ComponentTypesId = 9, MeasureTypesId = 4 },  //Флакон для парфумів- шт
            new { ComponentTypesId = 10, MeasureTypesId = 1 } // Основа для парфумів - г
        );        
        
        modelBuilder.Entity("ComponentTypeBuyMeasureTypes").HasData(
            new { ComponentTypesId = 1, MeasureTypesId = 4 }, // Форма - шт
            new { ComponentTypesId = 2, MeasureTypesId = 1 }, // Основа - г
            new { ComponentTypesId = 3, MeasureTypesId = 2 }, // Запашка - мл
            new { ComponentTypesId = 4, MeasureTypesId = 2 }, // Пігмент - крап
            new { ComponentTypesId = 4, MeasureTypesId = 1 }, // Пігмент - г
            new { ComponentTypesId = 5, MeasureTypesId = 2 }, // Еф.масло - мл
            new { ComponentTypesId = 6, MeasureTypesId = 2 }, // Екстракт - мл
            new { ComponentTypesId = 7, MeasureTypesId = 4 }, // Інструменти - шт
            new { ComponentTypesId = 8, MeasureTypesId = 4 }, // Інші - шт
            
            new { ComponentTypesId = 9, MeasureTypesId = 4 },  //Флакон для парфумів- шт
            new { ComponentTypesId = 10, MeasureTypesId = 1 } // Основа для парфумів - г
        );

        modelBuilder.Entity("ComponentTypeCosmeticTypes").HasData(
            new { ComponentTypesId = 1, CosmeticTypesId = 1 }, 
            new { ComponentTypesId = 2, CosmeticTypesId = 1 }, 
            new { ComponentTypesId = 3, CosmeticTypesId = 1 }, 
            new { ComponentTypesId = 3, CosmeticTypesId = 2 }, 
            new { ComponentTypesId = 4, CosmeticTypesId = 1 }, 
            new { ComponentTypesId = 5, CosmeticTypesId = 1 }, 
            new { ComponentTypesId = 6, CosmeticTypesId = 1 }, 
            new { ComponentTypesId = 7, CosmeticTypesId = 1 }, 
            new { ComponentTypesId = 8, CosmeticTypesId = 1 }, 

            new { ComponentTypesId = 9, CosmeticTypesId = 2 }, 
            new { ComponentTypesId = 10, CosmeticTypesId = 2 }
        );

        base.OnModelCreating(modelBuilder); 
    }
}