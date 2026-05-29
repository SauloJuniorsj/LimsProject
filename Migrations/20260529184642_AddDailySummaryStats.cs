using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LimsProject.Migrations
{
    /// <inheritdoc />
    public partial class AddDailySummaryStats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "MaxTemperature",
                table: "BatchesDailySumaries",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "MinTemperature",
                table: "BatchesDailySumaries",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "ReadingCount",
                table: "BatchesDailySumaries",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxTemperature",
                table: "BatchesDailySumaries");

            migrationBuilder.DropColumn(
                name: "MinTemperature",
                table: "BatchesDailySumaries");

            migrationBuilder.DropColumn(
                name: "ReadingCount",
                table: "BatchesDailySumaries");
        }
    }
}
