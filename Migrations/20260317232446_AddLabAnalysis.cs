using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LimsProject.Migrations
{
    /// <inheritdoc />
    public partial class AddLabAnalysis : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LabAnalyses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    THC = table.Column<decimal>(type: "numeric", nullable: false),
                    CBD = table.Column<decimal>(type: "numeric", nullable: false),
                    Terpenes = table.Column<string>(type: "text", nullable: false),
                    AnalysisDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsPassed = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabAnalyses", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LabAnalyses");
        }
    }
}
