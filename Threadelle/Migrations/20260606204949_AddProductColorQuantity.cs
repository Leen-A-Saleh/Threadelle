using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Threadelle.Migrations
{
    /// <inheritdoc />
    public partial class AddProductColorQuantity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "ProductColors",
                type: "int",
                nullable: false,
                defaultValue: 0);
                
            migrationBuilder.Sql("UPDATE ProductColors SET Quantity = (SELECT Quantity FROM Products WHERE Products.Id = ProductColors.ProductId)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "ProductColors");
        }
    }
}
