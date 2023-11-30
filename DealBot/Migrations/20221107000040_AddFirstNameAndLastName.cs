using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DealBot.Migrations
{
    public partial class AddFirstNameAndLastName : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "first_name",
                table: "AspNetUsers",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "last_name",
                table: "AspNetUsers",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "first_name",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "last_name",
                table: "AspNetUsers");
        }
    }
}
