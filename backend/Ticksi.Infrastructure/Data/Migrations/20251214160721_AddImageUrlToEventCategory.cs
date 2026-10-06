using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ticksi.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddImageUrlToEventCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "EventCategories",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "EventCategories");
        }
    }
}
