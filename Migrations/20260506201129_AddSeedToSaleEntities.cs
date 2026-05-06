using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LimsProject.Migrations
{
    /// <inheritdoc />
    public partial class AddSeedToSaleEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AvarageTemperature",
                table: "Batches");

            migrationBuilder.DropColumn(
                name: "CbdPercentage",
                table: "Batches");

            migrationBuilder.DropColumn(
                name: "CurrentMoisture",
                table: "Batches");

            migrationBuilder.DropColumn(
                name: "CurrentTemperature",
                table: "Batches");

            migrationBuilder.DropColumn(
                name: "HasContaminants",
                table: "Batches");

            migrationBuilder.DropColumn(
                name: "Strain",
                table: "Batches");

            migrationBuilder.DropColumn(
                name: "ThcPercentage",
                table: "Batches");

            migrationBuilder.AddColumn<decimal>(
                name: "Co2",
                table: "SensorData",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Humidity",
                table: "SensorData",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SensorType",
                table: "SensorData",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "Terpenes",
                table: "LabAnalyses",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<bool>(
                name: "HasContaminants",
                table: "LabAnalyses",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "MoisturePercentage",
                table: "LabAnalyses",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "RoomId",
                table: "Batches",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SeedLotId",
                table: "Batches",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "ChainOfCustodyEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    PayloadJson = table.Column<string>(type: "jsonb", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OperatorId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChainOfCustodyEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChainOfCustodyEvents_Batches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "Batches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EnvironmentalAlerts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    SensorDataId = table.Column<Guid>(type: "uuid", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    Resolved = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnvironmentalAlerts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EnvironmentalAlerts_Batches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "Batches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HarvestRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    HarvestDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    WetWeightGrams = table.Column<decimal>(type: "numeric", nullable: false),
                    OperatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HarvestRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HarvestRecords_Batches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "Batches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Plants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    TagCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    MotherPlantId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Plants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Plants_Batches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "Batches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Strains",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    ThcMaxLimit = table.Column<decimal>(type: "numeric", nullable: false),
                    IsHemp = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Strains", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DryingRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HarvestId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DryWeightGrams = table.Column<decimal>(type: "numeric", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DryingRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DryingRecords_HarvestRecords_HarvestId",
                        column: x => x.HarvestId,
                        principalTable: "HarvestRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AlertThresholds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StrainId = table.Column<Guid>(type: "uuid", nullable: false),
                    MinTemperature = table.Column<decimal>(type: "numeric", nullable: false),
                    MaxTemperature = table.Column<decimal>(type: "numeric", nullable: false),
                    MaxHumidity = table.Column<decimal>(type: "numeric", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertThresholds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AlertThresholds_Strains_StrainId",
                        column: x => x.StrainId,
                        principalTable: "Strains",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FinishedProducts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    StrainId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitWeightGrams = table.Column<decimal>(type: "numeric", nullable: false),
                    Format = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinishedProducts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinishedProducts_Strains_StrainId",
                        column: x => x.StrainId,
                        principalTable: "Strains",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SeedLots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StrainId = table.Column<Guid>(type: "uuid", nullable: false),
                    Supplier = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    LotCode = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    ReceivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeedLots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SeedLots_Strains_StrainId",
                        column: x => x.StrainId,
                        principalTable: "Strains",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CuringRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DryingId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FinalMoisture = table.Column<decimal>(type: "numeric", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CuringRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CuringRecords_DryingRecords_DryingId",
                        column: x => x.DryingId,
                        principalTable: "DryingRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductPackages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    FinishedProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    SerialNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    WeightGrams = table.Column<decimal>(type: "numeric", nullable: false),
                    PackagedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsSold = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductPackages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductPackages_Batches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "Batches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductPackages_FinishedProducts_FinishedProductId",
                        column: x => x.FinishedProductId,
                        principalTable: "FinishedProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

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
                name: "IX_Batches_SeedLotId",
                table: "Batches",
                column: "SeedLotId");

            migrationBuilder.CreateIndex(
                name: "IX_Batches_Status",
                table: "Batches",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AlertThresholds_StrainId",
                table: "AlertThresholds",
                column: "StrainId");

            migrationBuilder.CreateIndex(
                name: "IX_ChainOfCustodyEvents_BatchId_OccurredAt",
                table: "ChainOfCustodyEvents",
                columns: new[] { "BatchId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CuringRecords_DryingId",
                table: "CuringRecords",
                column: "DryingId");

            migrationBuilder.CreateIndex(
                name: "IX_DryingRecords_HarvestId",
                table: "DryingRecords",
                column: "HarvestId");

            migrationBuilder.CreateIndex(
                name: "IX_EnvironmentalAlerts_BatchId_Resolved",
                table: "EnvironmentalAlerts",
                columns: new[] { "BatchId", "Resolved" });

            migrationBuilder.CreateIndex(
                name: "IX_FinishedProducts_StrainId",
                table: "FinishedProducts",
                column: "StrainId");

            migrationBuilder.CreateIndex(
                name: "IX_HarvestRecords_BatchId",
                table: "HarvestRecords",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_Plants_BatchId",
                table: "Plants",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_Plants_TagCode",
                table: "Plants",
                column: "TagCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductPackages_BatchId",
                table: "ProductPackages",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductPackages_FinishedProductId",
                table: "ProductPackages",
                column: "FinishedProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductPackages_SerialNumber",
                table: "ProductPackages",
                column: "SerialNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SeedLots_StrainId_LotCode",
                table: "SeedLots",
                columns: new[] { "StrainId", "LotCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Strains_Name",
                table: "Strains",
                column: "Name");

            migrationBuilder.AddForeignKey(
                name: "FK_Batches_SeedLots_SeedLotId",
                table: "Batches",
                column: "SeedLotId",
                principalTable: "SeedLots",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

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
                name: "FK_Batches_SeedLots_SeedLotId",
                table: "Batches");

            migrationBuilder.DropForeignKey(
                name: "FK_BatchesDailySumaries_Batches_BatchId",
                table: "BatchesDailySumaries");

            migrationBuilder.DropForeignKey(
                name: "FK_LabAnalyses_Batches_BatchId",
                table: "LabAnalyses");

            migrationBuilder.DropForeignKey(
                name: "FK_SensorData_Batches_BatchId",
                table: "SensorData");

            migrationBuilder.DropTable(
                name: "AlertThresholds");

            migrationBuilder.DropTable(
                name: "ChainOfCustodyEvents");

            migrationBuilder.DropTable(
                name: "CuringRecords");

            migrationBuilder.DropTable(
                name: "EnvironmentalAlerts");

            migrationBuilder.DropTable(
                name: "Plants");

            migrationBuilder.DropTable(
                name: "ProductPackages");

            migrationBuilder.DropTable(
                name: "SeedLots");

            migrationBuilder.DropTable(
                name: "DryingRecords");

            migrationBuilder.DropTable(
                name: "FinishedProducts");

            migrationBuilder.DropTable(
                name: "HarvestRecords");

            migrationBuilder.DropTable(
                name: "Strains");

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
                name: "IX_Batches_SeedLotId",
                table: "Batches");

            migrationBuilder.DropIndex(
                name: "IX_Batches_Status",
                table: "Batches");

            migrationBuilder.DropColumn(
                name: "Co2",
                table: "SensorData");

            migrationBuilder.DropColumn(
                name: "Humidity",
                table: "SensorData");

            migrationBuilder.DropColumn(
                name: "SensorType",
                table: "SensorData");

            migrationBuilder.DropColumn(
                name: "HasContaminants",
                table: "LabAnalyses");

            migrationBuilder.DropColumn(
                name: "MoisturePercentage",
                table: "LabAnalyses");

            migrationBuilder.DropColumn(
                name: "RoomId",
                table: "Batches");

            migrationBuilder.DropColumn(
                name: "SeedLotId",
                table: "Batches");

            migrationBuilder.AlterColumn<string>(
                name: "Terpenes",
                table: "LabAnalyses",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(1024)",
                oldMaxLength: 1024);

            migrationBuilder.AddColumn<decimal>(
                name: "AvarageTemperature",
                table: "Batches",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "CbdPercentage",
                table: "Batches",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CurrentMoisture",
                table: "Batches",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CurrentTemperature",
                table: "Batches",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasContaminants",
                table: "Batches",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Strain",
                table: "Batches",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "ThcPercentage",
                table: "Batches",
                type: "numeric",
                nullable: true);
        }
    }
}
