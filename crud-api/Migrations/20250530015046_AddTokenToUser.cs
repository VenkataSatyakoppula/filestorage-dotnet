using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace crud_api.Migrations
{
    /// <inheritdoc />
    public partial class AddTokenToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Files",
                keyColumn: "FileId",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "Token",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FilePath",
                table: "Files",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Token",
                table: "Users");

            migrationBuilder.AlterColumn<string>(
                name: "FilePath",
                table: "Files",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "Email", "Name", "Password" },
                values: new object[,]
                {
                    { 1, "Venkata@test.com", "Venkata", "03AC674216F3E15C761EE1A5E255F067953623C8B388B4459E13F978D7C846F4" },
                    { 2, "satya@test.com", "Satya", "03AC674216F3E15C761EE1A5E255F067953623C8B388B4459E13F978D7C846F4" },
                    { 3, "test@test.com", "test", "03AC674216F3E15C761EE1A5E255F067953623C8B388B4459E13F978D7C846F4" },
                    { 4, "test2@test.com", "test2", "03AC674216F3E15C761EE1A5E255F067953623C8B388B4459E13F978D7C846F4" }
                });

            migrationBuilder.InsertData(
                table: "Files",
                columns: new[] { "FileId", "CreatedAt", "FileName", "FilePath", "FileSize", "FileType", "UserId" },
                values: new object[] { 1, null, "Random_Turtle.jpg", "storage/Random_Turtle.jpg", null, null, 1 });
        }
    }
}
