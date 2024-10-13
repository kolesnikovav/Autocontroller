using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace webapi.Migrations
{
    /// <inheritdoc />
    public partial class Cats2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Cats",
                columns: new[] { "Id", "Nickname", "ParentId" },
                values: new object[,]
                {
                    { new Guid("13a85ab7-249f-4246-b4df-e2dbcd8d90b4"), "Jack", null },
                    { new Guid("4b7eb1bc-b261-477b-b0f3-20fc1af02d33"), "Tom", null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Cats",
                keyColumn: "Id",
                keyValue: new Guid("13a85ab7-249f-4246-b4df-e2dbcd8d90b4"));

            migrationBuilder.DeleteData(
                table: "Cats",
                keyColumn: "Id",
                keyValue: new Guid("4b7eb1bc-b261-477b-b0f3-20fc1af02d33"));
        }
    }
}
