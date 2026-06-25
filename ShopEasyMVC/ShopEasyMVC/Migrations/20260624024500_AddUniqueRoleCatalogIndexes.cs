using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ShopEasyMVC.Data;

#nullable disable

namespace ShopEasyMVC.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260624024500_AddUniqueRoleCatalogIndexes")]
    public partial class AddUniqueRoleCatalogIndexes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserRoles_UserId",
                table: "UserRoles");

            migrationBuilder.Sql("""
                WITH DuplicatedRoles AS (
                    SELECT [Id],
                           ROW_NUMBER() OVER (PARTITION BY [UserId], [Name] ORDER BY [Id]) AS RowNumber
                    FROM [UserRoles]
                )
                DELETE FROM DuplicatedRoles
                WHERE [RowNumber] > 1
                """);

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_Name",
                table: "UserRoles",
                column: "Name",
                unique: true,
                filter: "[UserId] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_UserId_Name",
                table: "UserRoles",
                columns: new[] { "UserId", "Name" },
                unique: true,
                filter: "[UserId] IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserRoles_Name",
                table: "UserRoles");

            migrationBuilder.DropIndex(
                name: "IX_UserRoles_UserId_Name",
                table: "UserRoles");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_UserId",
                table: "UserRoles",
                column: "UserId");
        }
    }
}
