using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Threadelle.Migrations
{
    /// <inheritdoc />
    public partial class AddGalleryFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GalleryDescription",
                table: "CustomOrders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "GalleryLastViewedAt",
                table: "CustomOrders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "GalleryPublishedAt",
                table: "CustomOrders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GalleryViewCount",
                table: "CustomOrders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DisplayOrder",
                table: "CustomOrderImages",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsCoverImage",
                table: "CustomOrderImages",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "CustomOrderImages",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GalleryDescription",
                table: "CustomOrders");

            migrationBuilder.DropColumn(
                name: "GalleryLastViewedAt",
                table: "CustomOrders");

            migrationBuilder.DropColumn(
                name: "GalleryPublishedAt",
                table: "CustomOrders");

            migrationBuilder.DropColumn(
                name: "GalleryViewCount",
                table: "CustomOrders");

            migrationBuilder.DropColumn(
                name: "DisplayOrder",
                table: "CustomOrderImages");

            migrationBuilder.DropColumn(
                name: "IsCoverImage",
                table: "CustomOrderImages");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "CustomOrderImages");
        }
    }
}
