using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoldWatch.Api.Migrations
{
    /// <inheritdoc />
    public partial class FixNewsTableNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NewsArticle_NewsSource_SourceID",
                table: "NewsArticle");

            migrationBuilder.DropPrimaryKey(
                name: "PK_NewsSource",
                table: "NewsSource");

            migrationBuilder.DropPrimaryKey(
                name: "PK_NewsArticle",
                table: "NewsArticle");

            migrationBuilder.RenameTable(
                name: "NewsSource",
                newName: "NewsSources");

            migrationBuilder.RenameTable(
                name: "NewsArticle",
                newName: "NewsArticles");

            migrationBuilder.RenameIndex(
                name: "IX_NewsArticle_Url",
                table: "NewsArticles",
                newName: "IX_NewsArticles_Url");

            migrationBuilder.RenameIndex(
                name: "IX_NewsArticle_SourceID",
                table: "NewsArticles",
                newName: "IX_NewsArticles_SourceID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_NewsSources",
                table: "NewsSources",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_NewsArticles",
                table: "NewsArticles",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_NewsArticles_NewsSources_SourceID",
                table: "NewsArticles",
                column: "SourceID",
                principalTable: "NewsSources",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NewsArticles_NewsSources_SourceID",
                table: "NewsArticles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_NewsSources",
                table: "NewsSources");

            migrationBuilder.DropPrimaryKey(
                name: "PK_NewsArticles",
                table: "NewsArticles");

            migrationBuilder.RenameTable(
                name: "NewsSources",
                newName: "NewsSource");

            migrationBuilder.RenameTable(
                name: "NewsArticles",
                newName: "NewsArticle");

            migrationBuilder.RenameIndex(
                name: "IX_NewsArticles_Url",
                table: "NewsArticle",
                newName: "IX_NewsArticle_Url");

            migrationBuilder.RenameIndex(
                name: "IX_NewsArticles_SourceID",
                table: "NewsArticle",
                newName: "IX_NewsArticle_SourceID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_NewsSource",
                table: "NewsSource",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_NewsArticle",
                table: "NewsArticle",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_NewsArticle_NewsSource_SourceID",
                table: "NewsArticle",
                column: "SourceID",
                principalTable: "NewsSource",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
