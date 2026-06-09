using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Threadelle.Migrations
{
    /// <inheritdoc />
    public partial class CompletedCustomOrdersGallery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ShowInGallery",
                table: "CustomOrders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsFinishedProduct",
                table: "CustomOrderImages",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShowInGallery",
                table: "CustomOrders");

            migrationBuilder.DropColumn(
                name: "IsFinishedProduct",
                table: "CustomOrderImages");
        }
    }
}
