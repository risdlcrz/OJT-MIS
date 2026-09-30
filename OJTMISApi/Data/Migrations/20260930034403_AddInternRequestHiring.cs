using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OJTMISApi.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInternRequestHiring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RequestId",
                table: "Interns",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Interns",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Applicant");

            migrationBuilder.CreateTable(
                name: "InternRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OfficeCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OfficeName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Count = table.Column<int>(type: "int", nullable: false),
                    Filled = table.Column<int>(type: "int", nullable: false),
                    Skills = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Open"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InternRequests", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Interns_RequestId",
                table: "Interns",
                column: "RequestId");

            migrationBuilder.AddForeignKey(
                name: "FK_Interns_InternRequests_RequestId",
                table: "Interns",
                column: "RequestId",
                principalTable: "InternRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Interns_InternRequests_RequestId",
                table: "Interns");

            migrationBuilder.DropTable(
                name: "InternRequests");

            migrationBuilder.DropIndex(
                name: "IX_Interns_RequestId",
                table: "Interns");

            migrationBuilder.DropColumn(
                name: "RequestId",
                table: "Interns");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Interns");
        }
    }
}
