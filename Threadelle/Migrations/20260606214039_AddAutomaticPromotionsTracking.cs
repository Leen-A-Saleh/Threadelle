using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Threadelle.Migrations
{
    /// <inheritdoc />
    public partial class AddAutomaticPromotionsTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CouponType",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountPercentage",
                table: "Orders",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DiscountType",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DiscountedProducts",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "FreeShippingApplied",
                table: "Orders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsAutomaticPromotion",
                table: "Orders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsAutomaticPromotion",
                table: "Coupons",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CouponType",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DiscountPercentage",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DiscountType",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DiscountedProducts",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "FreeShippingApplied",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "IsAutomaticPromotion",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "IsAutomaticPromotion",
                table: "Coupons");
        }
    }
}
