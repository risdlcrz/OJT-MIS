using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OJTMISApi.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddApplicantsAndHireDates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ApplicantId",
                table: "Interns",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApplicantNo",
                table: "Interns",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ApplicantOffice",
                table: "Interns",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ContactNumber",
                table: "Interns",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CoordName",
                table: "Interns",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "DurationDays",
                table: "Interns",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "EducationLevel",
                table: "Interns",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Interns",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "EndDate",
                table: "Interns",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ExcludeFriday",
                table: "Interns",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "Interns",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "GuardianContact",
                table: "Interns",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "GuardianName",
                table: "Interns",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "HireDate",
                table: "Interns",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HouseAddress",
                table: "Interns",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "Interns",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MiddleName",
                table: "Interns",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Program",
                table: "Interns",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Remarks",
                table: "Interns",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "RequiredHours",
                table: "Interns",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "RequirementsCsv",
                table: "Interns",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SchoolName",
                table: "Interns",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "StartDate",
                table: "Interns",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Suffix",
                table: "Interns",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "Applicants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApplicantNo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MiddleName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Suffix = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ContactNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    HouseAddress = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    SchoolName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EducationLevel = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Program = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CoordName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RequiredHours = table.Column<int>(type: "int", nullable: false),
                    GuardianName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    GuardianContact = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RequirementsCsv = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "0,0,0,0,0,0"),
                    Accepted = table.Column<bool>(type: "bit", nullable: false),
                    Rejected = table.Column<bool>(type: "bit", nullable: false),
                    Hired = table.Column<bool>(type: "bit", nullable: false),
                    ApplicantOffice = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    RequestNo = table.Column<int>(type: "int", nullable: true),
                    RequestNoLabel = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ActionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OrientationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OrientationTime = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    OrientationOffice = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    OrientationConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    HireDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DurationDays = table.Column<int>(type: "int", nullable: false),
                    ExcludeFriday = table.Column<bool>(type: "bit", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Applicants", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Applicants_ApplicantNo",
                table: "Applicants",
                column: "ApplicantNo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Applicants");

            migrationBuilder.DropColumn(
                name: "ApplicantId",
                table: "Interns");

            migrationBuilder.DropColumn(
                name: "ApplicantNo",
                table: "Interns");

            migrationBuilder.DropColumn(
                name: "ApplicantOffice",
                table: "Interns");

            migrationBuilder.DropColumn(
                name: "ContactNumber",
                table: "Interns");

            migrationBuilder.DropColumn(
                name: "CoordName",
                table: "Interns");

            migrationBuilder.DropColumn(
                name: "DurationDays",
                table: "Interns");

            migrationBuilder.DropColumn(
                name: "EducationLevel",
                table: "Interns");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Interns");

            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "Interns");

            migrationBuilder.DropColumn(
                name: "ExcludeFriday",
                table: "Interns");

            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "Interns");

            migrationBuilder.DropColumn(
                name: "GuardianContact",
                table: "Interns");

            migrationBuilder.DropColumn(
                name: "GuardianName",
                table: "Interns");

            migrationBuilder.DropColumn(
                name: "HireDate",
                table: "Interns");

            migrationBuilder.DropColumn(
                name: "HouseAddress",
                table: "Interns");

            migrationBuilder.DropColumn(
                name: "LastName",
                table: "Interns");

            migrationBuilder.DropColumn(
                name: "MiddleName",
                table: "Interns");

            migrationBuilder.DropColumn(
                name: "Program",
                table: "Interns");

            migrationBuilder.DropColumn(
                name: "Remarks",
                table: "Interns");

            migrationBuilder.DropColumn(
                name: "RequiredHours",
                table: "Interns");

            migrationBuilder.DropColumn(
                name: "RequirementsCsv",
                table: "Interns");

            migrationBuilder.DropColumn(
                name: "SchoolName",
                table: "Interns");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "Interns");

            migrationBuilder.DropColumn(
                name: "Suffix",
                table: "Interns");
        }
    }
}
