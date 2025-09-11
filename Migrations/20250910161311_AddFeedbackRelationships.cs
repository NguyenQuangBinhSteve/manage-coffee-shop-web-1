using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace manage_coffee_shop_web.Migrations
{
    /// <inheritdoc />
    public partial class AddFeedbackRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProductId",
                table: "Feedbacks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ProductId1",
                table: "Feedbacks",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ArchivedOrders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApplicationUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    OrderDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ArchivedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArchivedOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArchivedOrders_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ArchivedOrderDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ArchivedOrderId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArchivedOrderDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArchivedOrderDetails_ArchivedOrders_ArchivedOrderId",
                        column: x => x.ArchivedOrderId,
                        principalTable: "ArchivedOrders",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ArchivedOrderDetails_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Feedbacks_ProductId",
                table: "Feedbacks",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Feedbacks_ProductId1",
                table: "Feedbacks",
                column: "ProductId1");

            migrationBuilder.CreateIndex(
                name: "IX_ArchivedOrderDetails_ArchivedOrderId",
                table: "ArchivedOrderDetails",
                column: "ArchivedOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ArchivedOrderDetails_ProductId",
                table: "ArchivedOrderDetails",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ArchivedOrders_ApplicationUserId",
                table: "ArchivedOrders",
                column: "ApplicationUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Feedbacks_Products_ProductId",
                table: "Feedbacks",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Feedbacks_Products_ProductId1",
                table: "Feedbacks",
                column: "ProductId1",
                principalTable: "Products",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Feedbacks_Products_ProductId",
                table: "Feedbacks");

            migrationBuilder.DropForeignKey(
                name: "FK_Feedbacks_Products_ProductId1",
                table: "Feedbacks");

            migrationBuilder.DropTable(
                name: "ArchivedOrderDetails");

            migrationBuilder.DropTable(
                name: "ArchivedOrders");

            migrationBuilder.DropIndex(
                name: "IX_Feedbacks_ProductId",
                table: "Feedbacks");

            migrationBuilder.DropIndex(
                name: "IX_Feedbacks_ProductId1",
                table: "Feedbacks");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "Feedbacks");

            migrationBuilder.DropColumn(
                name: "ProductId1",
                table: "Feedbacks");
        }
    }
}
