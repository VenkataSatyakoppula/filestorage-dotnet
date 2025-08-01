using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace crud_api.Migrations
{
    /// <inheritdoc />
    public partial class addedSizeLimitForUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RemainingSize",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TotalSize",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RemainingSize",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TotalSize",
                table: "Users");
        }
    }
}
