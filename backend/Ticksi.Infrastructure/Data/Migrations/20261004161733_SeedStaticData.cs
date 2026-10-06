using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Ticksi.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedStaticData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "EventTypes",
                columns: new[] { "Id", "Description", "Name", "PublicId" },
                values: new object[,]
                {
                    { 1, "A live music performance.", "Concert", new Guid("4b7e9a10-2c5d-4e8f-a1b3-0d6c8e2f4a01") },
                    { 2, "A match, race or other sports competition.", "Sport", new Guid("4b7e9a10-2c5d-4e8f-a1b3-0d6c8e2f4a02") },
                    { 3, "A play, opera, ballet or stand-up performance.", "Theatre", new Guid("4b7e9a10-2c5d-4e8f-a1b3-0d6c8e2f4a03") },
                    { 4, "A hands-on session where visitors learn by doing.", "Workshop", new Guid("4b7e9a10-2c5d-4e8f-a1b3-0d6c8e2f4a04") },
                    { 5, "Talks and panels on a professional topic.", "Conference", new Guid("4b7e9a10-2c5d-4e8f-a1b3-0d6c8e2f4a05") },
                    { 6, "A multi-day programme with several performers.", "Festival", new Guid("4b7e9a10-2c5d-4e8f-a1b3-0d6c8e2f4a06") },
                    { 7, "An art or design show open to visitors.", "Exhibition", new Guid("4b7e9a10-2c5d-4e8f-a1b3-0d6c8e2f4a07") },
                    { 8, "Any event that fits none of the other types.", "Other", new Guid("4b7e9a10-2c5d-4e8f-a1b3-0d6c8e2f4a08") }
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "Name", "PublicId" },
                values: new object[,]
                {
                    { 1, "Admin", new Guid("8d0c3f52-6b1e-4f4a-9c35-1f2e7a9b0c01") },
                    { 2, "User", new Guid("8d0c3f52-6b1e-4f4a-9c35-1f2e7a9b0c02") },
                    { 3, "Organizer", new Guid("8d0c3f52-6b1e-4f4a-9c35-1f2e7a9b0c03") }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "EventTypes",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "EventTypes",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "EventTypes",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "EventTypes",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "EventTypes",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "EventTypes",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "EventTypes",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "EventTypes",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 3);
        }
    }
}
