using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SoupAndSoup.Data.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ComponentTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ShortName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    BuyAmount = table.Column<int>(type: "int", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    IsSingleSelected = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComponentTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CosmeticTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CosmeticTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MeasureTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ShortName = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeasureTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Recipes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DateOfCreate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PreparationTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recipes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ComponentTypeCosmeticTypes",
                columns: table => new
                {
                    ComponentTypesId = table.Column<int>(type: "int", nullable: false),
                    CosmeticTypesId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComponentTypeCosmeticTypes", x => new { x.ComponentTypesId, x.CosmeticTypesId });
                    table.ForeignKey(
                        name: "FK_ComponentTypeCosmeticTypes_ComponentTypes_ComponentTypesId",
                        column: x => x.ComponentTypesId,
                        principalTable: "ComponentTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ComponentTypeCosmeticTypes_CosmeticTypes_CosmeticTypesId",
                        column: x => x.CosmeticTypesId,
                        principalTable: "CosmeticTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Components",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Cost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SuggestedAmount = table.Column<int>(type: "int", nullable: false),
                    BuyAmount = table.Column<int>(type: "int", nullable: false),
                    BuyPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ComponentTypeId = table.Column<int>(type: "int", nullable: false),
                    UseMeasureTypeId = table.Column<int>(type: "int", nullable: false),
                    BuyMeasureTypeId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Components", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Components_ComponentTypes_ComponentTypeId",
                        column: x => x.ComponentTypeId,
                        principalTable: "ComponentTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Components_MeasureTypes_BuyMeasureTypeId",
                        column: x => x.BuyMeasureTypeId,
                        principalTable: "MeasureTypes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Components_MeasureTypes_UseMeasureTypeId",
                        column: x => x.UseMeasureTypeId,
                        principalTable: "MeasureTypes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ComponentTypeBuyMeasureTypes",
                columns: table => new
                {
                    ComponentTypesId = table.Column<int>(type: "int", nullable: false),
                    MeasureTypesId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComponentTypeBuyMeasureTypes", x => new { x.ComponentTypesId, x.MeasureTypesId });
                    table.ForeignKey(
                        name: "FK_ComponentTypeBuyMeasureTypes_ComponentTypes_ComponentTypesId",
                        column: x => x.ComponentTypesId,
                        principalTable: "ComponentTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ComponentTypeBuyMeasureTypes_MeasureTypes_MeasureTypesId",
                        column: x => x.MeasureTypesId,
                        principalTable: "MeasureTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ComponentTypeUseMeasureTypes",
                columns: table => new
                {
                    ComponentTypesId = table.Column<int>(type: "int", nullable: false),
                    MeasureTypesId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComponentTypeUseMeasureTypes", x => new { x.ComponentTypesId, x.MeasureTypesId });
                    table.ForeignKey(
                        name: "FK_ComponentTypeUseMeasureTypes_ComponentTypes_ComponentTypesId",
                        column: x => x.ComponentTypesId,
                        principalTable: "ComponentTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ComponentTypeUseMeasureTypes_MeasureTypes_MeasureTypesId",
                        column: x => x.MeasureTypesId,
                        principalTable: "MeasureTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RecipeImages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RecipeId = table.Column<int>(type: "int", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AltText = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecipeImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecipeImages_Recipes_RecipeId",
                        column: x => x.RecipeId,
                        principalTable: "Recipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ComponentImages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ComponentId = table.Column<int>(type: "int", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AltText = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComponentImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComponentImages_Components_ComponentId",
                        column: x => x.ComponentId,
                        principalTable: "Components",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RecipeComponents",
                columns: table => new
                {
                    RecipeId = table.Column<int>(type: "int", nullable: false),
                    ComponentId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecipeComponents", x => new { x.RecipeId, x.ComponentId });
                    table.ForeignKey(
                        name: "FK_RecipeComponents_Components_ComponentId",
                        column: x => x.ComponentId,
                        principalTable: "Components",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RecipeComponents_Recipes_RecipeId",
                        column: x => x.RecipeId,
                        principalTable: "Recipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "ComponentTypes",
                columns: new[] { "Id", "BuyAmount", "IsSingleSelected", "Name", "Order", "ShortName" },
                values: new object[,]
                {
                    { 1, 1, true, "Форма для мила", 1, "форму" },
                    { 2, 200, false, "Мильна основа", 2, "основу" },
                    { 3, 10, false, "Запашка", 3, "запашку" },
                    { 4, 10, false, "Пігмент", 4, "пігмент" },
                    { 5, 10, false, "Ефірне масло", 5, "ефірне масло" },
                    { 6, 10, false, "Екстракт", 6, "екстракт" },
                    { 7, 1, false, "Інструменти", 7, "інструмент" },
                    { 8, 1, false, "Інші", 8, "" },
                    { 9, 1, true, "Флакон для парфумів", 1, "флакон" },
                    { 10, 500, false, "Основа для парфумів", 2, "основу" }
                });

            migrationBuilder.InsertData(
                table: "CosmeticTypes",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "Мило" },
                    { 2, "Духи" }
                });

            migrationBuilder.InsertData(
                table: "MeasureTypes",
                columns: new[] { "Id", "Name", "ShortName" },
                values: new object[,]
                {
                    { 1, "Грам", "г" },
                    { 2, "Мілілітр", "мл" },
                    { 3, "Краплі", "крап" },
                    { 4, "Штука", "шт" }
                });

            migrationBuilder.InsertData(
                table: "ComponentTypeBuyMeasureTypes",
                columns: new[] { "ComponentTypesId", "MeasureTypesId" },
                values: new object[,]
                {
                    { 1, 4 },
                    { 2, 1 },
                    { 3, 2 },
                    { 4, 1 },
                    { 4, 2 },
                    { 5, 2 },
                    { 6, 2 },
                    { 7, 4 },
                    { 8, 4 },
                    { 9, 4 },
                    { 10, 1 }
                });

            migrationBuilder.InsertData(
                table: "ComponentTypeCosmeticTypes",
                columns: new[] { "ComponentTypesId", "CosmeticTypesId" },
                values: new object[,]
                {
                    { 1, 1 },
                    { 2, 1 },
                    { 3, 1 },
                    { 3, 2 },
                    { 4, 1 },
                    { 5, 1 },
                    { 6, 1 },
                    { 7, 1 },
                    { 8, 1 },
                    { 9, 2 },
                    { 10, 2 }
                });

            migrationBuilder.InsertData(
                table: "ComponentTypeUseMeasureTypes",
                columns: new[] { "ComponentTypesId", "MeasureTypesId" },
                values: new object[,]
                {
                    { 1, 4 },
                    { 2, 1 },
                    { 3, 3 },
                    { 4, 1 },
                    { 4, 3 },
                    { 5, 3 },
                    { 6, 3 },
                    { 7, 4 },
                    { 8, 4 },
                    { 9, 4 },
                    { 10, 1 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ComponentImages_ComponentId",
                table: "ComponentImages",
                column: "ComponentId");

            migrationBuilder.CreateIndex(
                name: "IX_Components_BuyMeasureTypeId",
                table: "Components",
                column: "BuyMeasureTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Components_ComponentTypeId",
                table: "Components",
                column: "ComponentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Components_Name",
                table: "Components",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Components_UseMeasureTypeId",
                table: "Components",
                column: "UseMeasureTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ComponentTypeBuyMeasureTypes_MeasureTypesId",
                table: "ComponentTypeBuyMeasureTypes",
                column: "MeasureTypesId");

            migrationBuilder.CreateIndex(
                name: "IX_ComponentTypeCosmeticTypes_CosmeticTypesId",
                table: "ComponentTypeCosmeticTypes",
                column: "CosmeticTypesId");

            migrationBuilder.CreateIndex(
                name: "IX_ComponentTypeUseMeasureTypes_MeasureTypesId",
                table: "ComponentTypeUseMeasureTypes",
                column: "MeasureTypesId");

            migrationBuilder.CreateIndex(
                name: "IX_RecipeComponents_ComponentId",
                table: "RecipeComponents",
                column: "ComponentId");

            migrationBuilder.CreateIndex(
                name: "IX_RecipeImages_RecipeId",
                table: "RecipeImages",
                column: "RecipeId");

            migrationBuilder.CreateIndex(
                name: "IX_Recipes_Name",
                table: "Recipes",
                column: "Name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ComponentImages");

            migrationBuilder.DropTable(
                name: "ComponentTypeBuyMeasureTypes");

            migrationBuilder.DropTable(
                name: "ComponentTypeCosmeticTypes");

            migrationBuilder.DropTable(
                name: "ComponentTypeUseMeasureTypes");

            migrationBuilder.DropTable(
                name: "RecipeComponents");

            migrationBuilder.DropTable(
                name: "RecipeImages");

            migrationBuilder.DropTable(
                name: "CosmeticTypes");

            migrationBuilder.DropTable(
                name: "Components");

            migrationBuilder.DropTable(
                name: "Recipes");

            migrationBuilder.DropTable(
                name: "ComponentTypes");

            migrationBuilder.DropTable(
                name: "MeasureTypes");
        }
    }
}
