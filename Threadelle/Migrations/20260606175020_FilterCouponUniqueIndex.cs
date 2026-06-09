using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Threadelle.Migrations
{
    /// <inheritdoc />
    public partial class FilterCouponUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Coupons_Code_Unique",
                table: "Coupons");

            migrationBuilder.CreateIndex(
                name: "IX_Coupons_Code_Unique",
                table: "Coupons",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Coupons_Code_Unique",
                table: "Coupons");

            migrationBuilder.CreateIndex(
                name: "IX_Coupons_Code_Unique",
                table: "Coupons",
                column: "Code",
                unique: true);
        }
    }
}
