using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ticksi.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ChangePasswordToPasswordHash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Password",
                table: "AppUsers",
                newName: "PasswordHash");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PasswordHash",
                table: "AppUsers",
                newName: "Password");
        }
    }
}
