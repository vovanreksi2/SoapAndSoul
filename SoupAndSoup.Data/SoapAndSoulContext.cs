using System.Reflection;
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
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

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
    }
}