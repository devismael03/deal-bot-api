using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DealBot.Migrations
{
    public partial class Add_Target_Keywords : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "target_keywords",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    keyword = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_target_keywords", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "application_user_target_keyword",
                columns: table => new
                {
                    target_keywords_id = table.Column<Guid>(type: "uuid", nullable: false),
                    users_id = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_application_user_target_keyword", x => new { x.target_keywords_id, x.users_id });
                    table.ForeignKey(
                        name: "fk_application_user_target_keyword_target_keywords_target_keyw",
                        column: x => x.target_keywords_id,
                        principalTable: "target_keywords",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_application_user_target_keyword_users_users_id",
                        column: x => x.users_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_application_user_target_keyword_users_id",
                table: "application_user_target_keyword",
                column: "users_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "application_user_target_keyword");

            migrationBuilder.DropTable(
                name: "target_keywords");
        }
    }
}
