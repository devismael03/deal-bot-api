using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DealBot.Migrations
{
    public partial class Add_Links_Table2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_link_users_user_id",
                table: "link");

            migrationBuilder.DropPrimaryKey(
                name: "pk_link",
                table: "link");

            migrationBuilder.RenameTable(
                name: "link",
                newName: "links");

            migrationBuilder.RenameIndex(
                name: "ix_link_user_id",
                table: "links",
                newName: "ix_links_user_id");

            migrationBuilder.AlterColumn<string>(
                name: "user_id",
                table: "links",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddPrimaryKey(
                name: "pk_links",
                table: "links",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_links_users_user_id",
                table: "links",
                column: "user_id",
                principalTable: "AspNetUsers",
                principalColumn: "id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_links_users_user_id",
                table: "links");

            migrationBuilder.DropPrimaryKey(
                name: "pk_links",
                table: "links");

            migrationBuilder.RenameTable(
                name: "links",
                newName: "link");

            migrationBuilder.RenameIndex(
                name: "ix_links_user_id",
                table: "link",
                newName: "ix_link_user_id");

            migrationBuilder.AlterColumn<string>(
                name: "user_id",
                table: "link",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "pk_link",
                table: "link",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_link_users_user_id",
                table: "link",
                column: "user_id",
                principalTable: "AspNetUsers",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
