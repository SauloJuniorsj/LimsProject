using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LimsProject.Migrations
{
    /// <inheritdoc />
    public partial class AddForeignKeysAndIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_SensorData_BatchId_ReadingTime",
                table: "SensorData",
                columns: new[] { "BatchId", "ReadingTime" });

            migrationBuilder.CreateIndex(
                name: "IX_LabAnalyses_BatchId",
                table: "LabAnalyses",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_BatchesDailySumaries_BatchId_Date",
                table: "BatchesDailySumaries",
                columns: new[] { "BatchId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Batches_Status",
                table: "Batches",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Batches_Strain",
                table: "Batches",
                column: "Strain");

            migrationBuilder.AddForeignKey(
                name: "FK_BatchesDailySumaries_Batches_BatchId",
                table: "BatchesDailySumaries",
                column: "BatchId",
                principalTable: "Batches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LabAnalyses_Batches_BatchId",
                table: "LabAnalyses",
                column: "BatchId",
                principalTable: "Batches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SensorData_Batches_BatchId",
                table: "SensorData",
                column: "BatchId",
                principalTable: "Batches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BatchesDailySumaries_Batches_BatchId",
                table: "BatchesDailySumaries");

            migrationBuilder.DropForeignKey(
                name: "FK_LabAnalyses_Batches_BatchId",
                table: "LabAnalyses");

            migrationBuilder.DropForeignKey(
                name: "FK_SensorData_Batches_BatchId",
                table: "SensorData");

            migrationBuilder.DropIndex(
                name: "IX_SensorData_BatchId_ReadingTime",
                table: "SensorData");

            migrationBuilder.DropIndex(
                name: "IX_LabAnalyses_BatchId",
                table: "LabAnalyses");

            migrationBuilder.DropIndex(
                name: "IX_BatchesDailySumaries_BatchId_Date",
                table: "BatchesDailySumaries");

            migrationBuilder.DropIndex(
                name: "IX_Batches_Status",
                table: "Batches");

            migrationBuilder.DropIndex(
                name: "IX_Batches_Strain",
                table: "Batches");
        }
    }
}
