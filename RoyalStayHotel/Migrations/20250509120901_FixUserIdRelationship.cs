using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoyalStayHotel.Migrations
{
    /// <inheritdoc />
    public partial class FixUserIdRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 1,
                column: "Id",
                value: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 1,
                column: "Id",
                value: 1);
        }
    }
}
