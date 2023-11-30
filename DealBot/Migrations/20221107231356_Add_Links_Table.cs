using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DealBot.Migrations
{
    public partial class Add_Links_Table : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "link",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    url = table.Column<string>(type: "text", nullable: false),
                    title = table.Column<string>(type: "text", nullable: true),
                    link_type = table.Column<int>(type: "integer", nullable: false),
                    provider = table.Column<int>(type: "integer", nullable: false),
                    current_price = table.Column<int>(type: "integer", nullable: true),
                    last_price_change = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    user_id = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_link", x => x.id);
                    table.ForeignKey(
                        name: "fk_link_users_user_id",
                        column: x => x.user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_link_user_id",
                table: "link",
                column: "user_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "link");
        }
    }
}
