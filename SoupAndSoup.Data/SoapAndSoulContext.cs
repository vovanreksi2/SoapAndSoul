using Microsoft.EntityFrameworkCore;
using SoupAndSoup.Data.Models;

namespace SoupAndSoup.Data;

public class SoapAndSoulContext : DbContext
{
    public DbSet<Recipe> Recipes { get; set; }
    public DbSet<Ingredient> Ingredients { get; set; }
    public DbSet<IngredientType> IngredientTypes { get; set; }
    public DbSet<AmountType> AmountTypes { get; set; }
    public DbSet<RecipeIngredient> RecipeIngredients { get; set; }
    public DbSet<IngredientImage> IngredientImages { get; set; }
    public DbSet<RecipeImage> RecipeImages { get; set; }

    public SoapAndSoulContext(DbContextOptions<SoapAndSoulContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Index on Recipe.Name for fast searching
        modelBuilder.Entity<Recipe>()
            .HasIndex(r => r.Name);

        modelBuilder.Entity<Ingredient>(entity =>
        {
            // Indexes on Ingredient.Name and Ingredient.IngredientTypeID
            entity.HasIndex(i => i.Name);
            entity.HasIndex(i => i.IngredientTypeId);

            // Relationship: Ingredient -> IngredientTypes
            entity.HasOne(i => i.IngredientType)
                .WithMany(it => it.Ingredients)
                .HasForeignKey(i => i.IngredientTypeId);

            // Relationship: Ingredient -> AmountTypes
            entity.HasMany(i => i.AmountTypes)
                .WithMany(it => it.Ingredients)
                .UsingEntity(j => j.ToTable("IngredientAmountTypes"));
        });

        modelBuilder.Entity<RecipeIngredient>(entity =>
        {
            // Composite primary key for RecipeIngredient (many-to-many)
            entity.HasKey(ri => new { RecipeID = ri.RecipeId, IngredientID = ri.IngredientId });

            // Relationship: RecipeIngredient -> Recipe
            entity.HasOne(ri => ri.Recipe)
                .WithMany(r => r.RecipeIngredients)
                .HasForeignKey(ri => ri.RecipeId);

            // Relationship: RecipeIngredient -> Ingredient
            entity.HasOne(ri => ri.Ingredient)
                .WithMany(i => i.RecipeIngredients)
                .HasForeignKey(ri => ri.IngredientId);
        });

        modelBuilder.Entity<IngredientType>(entity =>
        {
            // Relationship: IngredientTypes -> AmountType
            entity.HasMany(i => i.AmountTypes)
                .WithMany(at => at.IngredientTypes)
                .UsingEntity<Dictionary<string, object>>(
                    "IngredientTypeAmountTypes",
                    j => j
                        .HasOne<AmountType>()
                        .WithMany()
                        .HasForeignKey("AmountTypesId")
                        .OnDelete(DeleteBehavior.Cascade),
                    j => j
                        .HasOne<IngredientType>()
                        .WithMany()
                        .HasForeignKey("IngredientTypesId")
                        .OnDelete(DeleteBehavior.Cascade),
                    j =>
                    {
                        j.HasKey("IngredientTypesId", "AmountTypesId"); 
                        j.ToTable("IngredientTypeAmountTypes");
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

        modelBuilder.Entity<IngredientImage>(entity =>
        {
            entity.HasKey(i => i.Id);
            entity.HasIndex(i => i.IngredientId);

            entity.HasOne(i => i.Ingredient)
                .WithMany(i => i.Images)
                .HasForeignKey(i => i.IngredientId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        SeedData(modelBuilder);

        base.OnModelCreating(modelBuilder);
    }

    private void SeedData(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AmountType>().HasData(
            new AmountType { Id = 1, Name = "Грам", ShortName = "г" },
            new AmountType { Id = 2, Name = "Мілілітр", ShortName = "мл" },
            new AmountType { Id = 3, Name = "Штука", ShortName = "шт" }
        );

        modelBuilder.Entity<IngredientType>().HasData(
            new IngredientType { Id = 1, Name = "Форма для мила", ShortName = "форму", Order = 1 },
            new IngredientType { Id = 2, Name = "Мильна основа", ShortName = "основу", Order = 2 },
            new IngredientType { Id = 3, Name = "Запашка", ShortName = "запашку", Order = 3 },
            new IngredientType { Id = 4, Name = "Пігмент", ShortName = "пігмент", Order = 4 },
            new IngredientType { Id = 5, Name = "Ефірне масло", ShortName = "ефірне масло", Order = 5 },
            new IngredientType { Id = 6, Name = "Екстракт", ShortName = "екстракт", Order = 6 },
            new IngredientType { Id = 7, Name = "Інструменти", ShortName = "інструмент", Order = 7 },
            new IngredientType { Id = 8, Name = "Інші", ShortName = "", Order = 8 }
        );

        modelBuilder.Entity("IngredientTypeAmountTypes").HasData(
            new { IngredientTypesId = 1, AmountTypesId = 3 }, // Форма - шт
            new { IngredientTypesId = 2, AmountTypesId = 1 }, // Основа - г
            new { IngredientTypesId = 3, AmountTypesId = 2 }, // Запашка - мл
            new { IngredientTypesId = 4, AmountTypesId = 2 }, // Пігмент - мл
            new { IngredientTypesId = 4, AmountTypesId = 1 }, // Пігмент - г
            new { IngredientTypesId = 5, AmountTypesId = 2 }, // Еф.масло - мл
            new { IngredientTypesId = 6, AmountTypesId = 2 }, // Екстракт - мл
            new { IngredientTypesId = 7, AmountTypesId = 3 }, // Інструменти - шт
            new { IngredientTypesId = 8, AmountTypesId = 3 }  // Інші - шт
        );


        base.OnModelCreating(modelBuilder); 
    }
}