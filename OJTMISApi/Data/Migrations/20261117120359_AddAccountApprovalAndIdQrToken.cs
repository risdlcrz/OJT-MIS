using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OJTMISApi.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountApprovalAndIdQrToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AttendanceQrIssuedAt",
                table: "Interns",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AttendanceQrToken",
                table: "Interns",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsApproved",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // Accounts that existed before HR approval was introduced were
            // already in use, so treat them as approved. New accounts get the
            // value the code sets (approved when HR creates them, pending when
            // self-registered).
            migrationBuilder.Sql("UPDATE [AspNetUsers] SET [IsApproved] = 1;");

            migrationBuilder.CreateIndex(
                name: "IX_Interns_AttendanceQrToken",
                table: "Interns",
                column: "AttendanceQrToken",
                unique: true,
                filter: "[AttendanceQrToken] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Interns_AttendanceQrToken",
                table: "Interns");

            migrationBuilder.DropColumn(
                name: "AttendanceQrIssuedAt",
                table: "Interns");

            migrationBuilder.DropColumn(
                name: "AttendanceQrToken",
                table: "Interns");

            migrationBuilder.DropColumn(
                name: "IsApproved",
                table: "AspNetUsers");
        }
    }
}
