using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudyApp.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddExtractionRunsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DocxS3Key",
                table: "Modules");

            migrationBuilder.DropColumn(
                name: "ExtractionError",
                table: "Modules");

            migrationBuilder.DropColumn(
                name: "ExtractionStatus",
                table: "Modules");

            migrationBuilder.CreateTable(
                name: "ExtractionRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ModuleId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    DocxS3Key = table.Column<string>(type: "text", nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExtractionRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExtractionRuns_Modules_ModuleId",
                        column: x => x.ModuleId,
                        principalTable: "Modules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExtractionRuns_ModuleId",
                table: "ExtractionRuns",
                column: "ModuleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExtractionRuns");

            migrationBuilder.AddColumn<string>(
                name: "DocxS3Key",
                table: "Modules",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExtractionError",
                table: "Modules",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExtractionStatus",
                table: "Modules",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
