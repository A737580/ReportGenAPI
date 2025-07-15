using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ReportGen.Migrations
{
    /// <inheritdoc />
    public partial class InitDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Results",
                columns: table => new
                {
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    DeltaTime = table.Column<int>(type: "integer", nullable: false),
                    MinimumDateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AvgExecutionTime = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    AvgStoreValue = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    MedianStoreValue = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    MaximumStoreValue = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    MinimumStoreValue = table.Column<decimal>(type: "numeric(18,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Results", x => x.FileName);
                });

            migrationBuilder.CreateTable(
                name: "Values",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StartDateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ExecutionTimeS = table.Column<int>(type: "integer", nullable: false),
                    StoreValue = table.Column<decimal>(type: "numeric(18,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Values", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Values_Results_FileName",
                        column: x => x.FileName,
                        principalTable: "Results",
                        principalColumn: "FileName",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Values_FileName",
                table: "Values",
                column: "FileName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Values");

            migrationBuilder.DropTable(
                name: "Results");
        }
    }
}
