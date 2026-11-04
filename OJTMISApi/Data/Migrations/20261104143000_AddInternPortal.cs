using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OJTMISApi.Data;

#nullable disable

namespace OJTMISApi.Data.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261104143000_AddInternPortal")]
    public partial class AddInternPortal : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InternEmail",
                table: "DailyTimeRecords",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InternId",
                table: "DailyTimeRecords",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InternName",
                table: "DailyTimeRecords",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DocumentRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InternId = table.Column<int>(type: "int", nullable: false),
                    InternName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    InternEmail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DocumentType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentRequests", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "DocumentRequests");
            migrationBuilder.DropColumn(name: "InternEmail", table: "DailyTimeRecords");
            migrationBuilder.DropColumn(name: "InternId", table: "DailyTimeRecords");
            migrationBuilder.DropColumn(name: "InternName", table: "DailyTimeRecords");
        }
    }
}
