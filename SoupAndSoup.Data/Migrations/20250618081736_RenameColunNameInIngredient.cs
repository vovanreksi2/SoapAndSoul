using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoupAndSoup.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameColunNameInIngredient : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Price",
                table: "Ingredients",
                newName: "BuyPrice");

            migrationBuilder.RenameColumn(
                name: "DefaultAmount",
                table: "Ingredients",
                newName: "TypicalAmountInRecipe");

            migrationBuilder.RenameColumn(
                name: "Amount",
                table: "Ingredients",
                newName: "BuyAmount");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TypicalAmountInRecipe",
                table: "Ingredients",
                newName: "DefaultAmount");

            migrationBuilder.RenameColumn(
                name: "BuyPrice",
                table: "Ingredients",
                newName: "Price");

            migrationBuilder.RenameColumn(
                name: "BuyAmount",
                table: "Ingredients",
                newName: "Amount");
        }
    }
}
