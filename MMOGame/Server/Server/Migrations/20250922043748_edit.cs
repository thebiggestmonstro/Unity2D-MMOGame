using Microsoft.EntityFrameworkCore.Migrations;

namespace Server.Migrations
{
    public partial class edit : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Item_Player_OwnerId",
                table: "Item");

            migrationBuilder.DropIndex(
                name: "IX_Item_OwnerId",
                table: "Item");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "Item");

            migrationBuilder.AddColumn<int>(
                name: "OwnerDbId",
                table: "Item",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Item_OwnerDbId",
                table: "Item",
                column: "OwnerDbId");

            migrationBuilder.AddForeignKey(
                name: "FK_Item_Player_OwnerDbId",
                table: "Item",
                column: "OwnerDbId",
                principalTable: "Player",
                principalColumn: "PlayerDbId",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Item_Player_OwnerDbId",
                table: "Item");

            migrationBuilder.DropIndex(
                name: "IX_Item_OwnerDbId",
                table: "Item");

            migrationBuilder.DropColumn(
                name: "OwnerDbId",
                table: "Item");

            migrationBuilder.AddColumn<int>(
                name: "OwnerId",
                table: "Item",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Item_OwnerId",
                table: "Item",
                column: "OwnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Item_Player_OwnerId",
                table: "Item",
                column: "OwnerId",
                principalTable: "Player",
                principalColumn: "PlayerDbId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
