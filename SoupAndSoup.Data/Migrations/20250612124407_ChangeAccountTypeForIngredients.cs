using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoupAndSoup.Data.Migrations
{
    /// <inheritdoc />
    public partial class ChangeAccountTypeForIngredients : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IngredientAmountTypes");

            migrationBuilder.AddColumn<int>(
                name: "AmountTypeId",
                table: "Ingredients",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ingredients_AmountTypeId",
                table: "Ingredients",
                column: "AmountTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Ingredients_AmountTypes_AmountTypeId",
                table: "Ingredients",
                column: "AmountTypeId",
                principalTable: "AmountTypes",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Ingredients_AmountTypes_AmountTypeId",
                table: "Ingredients");

            migrationBuilder.DropIndex(
                name: "IX_Ingredients_AmountTypeId",
                table: "Ingredients");

            migrationBuilder.DropColumn(
                name: "AmountTypeId",
                table: "Ingredients");

            migrationBuilder.CreateTable(
                name: "IngredientAmountTypes",
                columns: table => new
                {
                    AmountTypesId = table.Column<int>(type: "int", nullable: false),
                    IngredientsId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IngredientAmountTypes", x => new { x.AmountTypesId, x.IngredientsId });
                    table.ForeignKey(
                        name: "FK_IngredientAmountTypes_AmountTypes_AmountTypesId",
                        column: x => x.AmountTypesId,
                        principalTable: "AmountTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IngredientAmountTypes_Ingredients_IngredientsId",
                        column: x => x.IngredientsId,
                        principalTable: "Ingredients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IngredientAmountTypes_IngredientsId",
                table: "IngredientAmountTypes",
                column: "IngredientsId");
        }
    }
}
