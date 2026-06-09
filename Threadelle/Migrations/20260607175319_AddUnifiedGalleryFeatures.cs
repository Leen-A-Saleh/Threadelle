using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Threadelle.Migrations
{
    /// <inheritdoc />
    public partial class AddUnifiedGalleryFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GalleryDisplayOrder",
                table: "Products",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsGalleryFeatured",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShowInGallery",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "AltText",
                table: "ProductImages",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GalleryDisplayOrder",
                table: "CustomOrders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsGalleryFeatured",
                table: "CustomOrders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "AltText",
                table: "CustomOrderImages",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GalleryDisplayOrder",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "IsGalleryFeatured",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ShowInGallery",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "AltText",
                table: "ProductImages");

            migrationBuilder.DropColumn(
                name: "GalleryDisplayOrder",
                table: "CustomOrders");

            migrationBuilder.DropColumn(
                name: "IsGalleryFeatured",
                table: "CustomOrders");

            migrationBuilder.DropColumn(
                name: "AltText",
                table: "CustomOrderImages");
        }
    }
}
