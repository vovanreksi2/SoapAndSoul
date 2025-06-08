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

        // Indexes on Ingredient.Name and Ingredient.IngredientTypeID
        modelBuilder.Entity<Ingredient>()
            .HasIndex(i => i.Name);
        modelBuilder.Entity<Ingredient>()
            .HasIndex(i => i.IngredientTypeId);

        // Composite primary key for RecipeIngredient (many-to-many)
        modelBuilder.Entity<RecipeIngredient>()
            .HasKey(ri => new { RecipeID = ri.RecipeId, IngredientID = ri.IngredientId });

        // Relationship: RecipeIngredient -> Recipe
        modelBuilder.Entity<RecipeIngredient>()
            .HasOne(ri => ri.Recipe)
            .WithMany(r => r.RecipeIngredients)
            .HasForeignKey(ri => ri.RecipeId);

        // Relationship: RecipeIngredient -> Ingredient
        modelBuilder.Entity<RecipeIngredient>()
            .HasOne(ri => ri.Ingredient)
            .WithMany(i => i.RecipeIngredients)
            .HasForeignKey(ri => ri.IngredientId);  

        // Relationship: Ingredient -> IngredientType
        modelBuilder.Entity<Ingredient>()
            .HasOne(i => i.IngredientType)
            .WithMany(it => it.Ingredients)
            .HasForeignKey(i => i.IngredientTypeId);
 
        // Relationship: IngredientType -> AmountType
        modelBuilder.Entity<IngredientType>()
            .HasOne(i => i.AmountType)
            .WithMany(at => at.IngredientType)
            .HasForeignKey(it => it.AmountTypeId); 

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
            new IngredientType { Id = 1, Name = "Форма для мила", ShortName = "форму"  , Order = 1, AmountTypeId = 3},
            new IngredientType { Id = 2, Name = "Мильна основа", ShortName = "основу" , Order = 2, AmountTypeId = 1 },
            new IngredientType { Id = 3, Name = "Запашка", ShortName = "запашку", Order = 3, AmountTypeId = 2 },
            new IngredientType { Id = 4, Name = "Пігмент", ShortName = "пігмент", Order = 4, AmountTypeId = 2 },
            new IngredientType { Id = 5, Name = "Ефірне масло", ShortName = "ефірне масло", Order = 5, AmountTypeId = 2 },
            new IngredientType { Id = 6, Name = "Екстракт", ShortName = "екстракт", Order = 6, AmountTypeId = 2 },
            new IngredientType { Id = 7, Name = "Інструменти", ShortName = "інструмент" , Order = 7, AmountTypeId = 2 },
            new IngredientType { Id = 8, Name = "Інші", ShortName = "" , Order = 8, AmountTypeId = 3 }
        );

        base.OnModelCreating(modelBuilder); 
    }
}