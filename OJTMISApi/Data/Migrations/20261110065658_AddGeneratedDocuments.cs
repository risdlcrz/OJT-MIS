using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OJTMISApi.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGeneratedDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GeneratedDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InternId = table.Column<int>(type: "int", nullable: true),
                    InternName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DocumentType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    DateIssued = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Office = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    School = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IdNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ValidUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SchoolAddress = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    EmergencyContactName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EmergencyContactNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EmergencyContactAddress = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Honorific = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    HoursRendered = table.Column<double>(type: "float", nullable: true),
                    RequiredHours = table.Column<int>(type: "int", nullable: true),
                    DateStarted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateCompleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RecipientEmail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SignatoryName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SignatoryPosition = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    SignatoryOffice = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    GeneratedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeneratedDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GeneratedDocuments_Interns_InternId",
                        column: x => x.InternId,
                        principalTable: "Interns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GeneratedDocuments_InternId_CreatedAt",
                table: "GeneratedDocuments",
                columns: new[] { "InternId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GeneratedDocuments");
        }
    }
}
