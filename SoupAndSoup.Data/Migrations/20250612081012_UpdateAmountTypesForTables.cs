using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SoupAndSoup.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAmountTypesForTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_IngredientTypes_AmountTypes_AmountTypeId",
                table: "IngredientTypes");

            migrationBuilder.DropIndex(
                name: "IX_IngredientTypes_AmountTypeId",
                table: "IngredientTypes");

            migrationBuilder.DropColumn(
                name: "AmountTypeId",
                table: "IngredientTypes");

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

            migrationBuilder.CreateTable(
                name: "IngredientTypeAmountTypes",
                columns: table => new
                {
                    IngredientTypesId = table.Column<int>(type: "int", nullable: false),
                    AmountTypesId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IngredientTypeAmountTypes", x => new { x.IngredientTypesId, x.AmountTypesId });
                    table.ForeignKey(
                        name: "FK_IngredientTypeAmountTypes_AmountTypes_AmountTypesId",
                        column: x => x.AmountTypesId,
                        principalTable: "AmountTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IngredientTypeAmountTypes_IngredientTypes_IngredientTypesId",
                        column: x => x.IngredientTypesId,
                        principalTable: "IngredientTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "IngredientTypeAmountTypes",
                columns: new[] { "AmountTypesId", "IngredientTypesId" },
                values: new object[,]
                {
                    { 3, 1 },
                    { 1, 2 },
                    { 2, 3 },
                    { 1, 4 },
                    { 2, 4 },
                    { 2, 5 },
                    { 2, 6 },
                    { 3, 7 },
                    { 3, 8 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_IngredientAmountTypes_IngredientsId",
                table: "IngredientAmountTypes",
                column: "IngredientsId");

            migrationBuilder.CreateIndex(
                name: "IX_IngredientTypeAmountTypes_AmountTypesId",
                table: "IngredientTypeAmountTypes",
                column: "AmountTypesId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IngredientAmountTypes");

            migrationBuilder.DropTable(
                name: "IngredientTypeAmountTypes");

            migrationBuilder.AddColumn<int>(
                name: "AmountTypeId",
                table: "IngredientTypes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "IngredientTypes",
                keyColumn: "Id",
                keyValue: 1,
                column: "AmountTypeId",
                value: 3);

            migrationBuilder.UpdateData(
                table: "IngredientTypes",
                keyColumn: "Id",
                keyValue: 2,
                column: "AmountTypeId",
                value: 1);

            migrationBuilder.UpdateData(
                table: "IngredientTypes",
                keyColumn: "Id",
                keyValue: 3,
                column: "AmountTypeId",
                value: 2);

            migrationBuilder.UpdateData(
                table: "IngredientTypes",
                keyColumn: "Id",
                keyValue: 4,
                column: "AmountTypeId",
                value: 2);

            migrationBuilder.UpdateData(
                table: "IngredientTypes",
                keyColumn: "Id",
                keyValue: 5,
                column: "AmountTypeId",
                value: 2);

            migrationBuilder.UpdateData(
                table: "IngredientTypes",
                keyColumn: "Id",
                keyValue: 6,
                column: "AmountTypeId",
                value: 2);

            migrationBuilder.UpdateData(
                table: "IngredientTypes",
                keyColumn: "Id",
                keyValue: 7,
                column: "AmountTypeId",
                value: 2);

            migrationBuilder.UpdateData(
                table: "IngredientTypes",
                keyColumn: "Id",
                keyValue: 8,
                column: "AmountTypeId",
                value: 3);

            migrationBuilder.CreateIndex(
                name: "IX_IngredientTypes_AmountTypeId",
                table: "IngredientTypes",
                column: "AmountTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_IngredientTypes_AmountTypes_AmountTypeId",
                table: "IngredientTypes",
                column: "AmountTypeId",
                principalTable: "AmountTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
