using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ticksi.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPosterToCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PosterUrl",
                table: "EventCategories",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PosterUrl",
                table: "EventCategories");
        }
    }
}
