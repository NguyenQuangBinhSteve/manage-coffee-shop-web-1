using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace manage_coffee_shop_web.Migrations
{
    /// <inheritdoc />
    public partial class RenameStatusEnumToFeedbackState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Feedbacks_Products_ProductId1",
                table: "Feedbacks");

            migrationBuilder.DropIndex(
                name: "IX_Feedbacks_ProductId1",
                table: "Feedbacks");

            migrationBuilder.DropColumn(
                name: "ProductId1",
                table: "Feedbacks");

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Feedbacks",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Feedbacks",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Feedbacks");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Feedbacks");

            migrationBuilder.AddColumn<int>(
                name: "ProductId1",
                table: "Feedbacks",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Feedbacks_ProductId1",
                table: "Feedbacks",
                column: "ProductId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Feedbacks_Products_ProductId1",
                table: "Feedbacks",
                column: "ProductId1",
                principalTable: "Products",
                principalColumn: "Id");
        }
    }
}
