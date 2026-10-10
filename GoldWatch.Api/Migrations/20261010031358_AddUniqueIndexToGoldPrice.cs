using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoldWatch.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueIndexToGoldPrice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_GoldPrices_GoldType_Source_CollectedAt",
                table: "GoldPrices",
                columns: new[] { "GoldType", "Source", "CollectedAt" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GoldPrices_GoldType_Source_CollectedAt",
                table: "GoldPrices");
        }
    }
}
