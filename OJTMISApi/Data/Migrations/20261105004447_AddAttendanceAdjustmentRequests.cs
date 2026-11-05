using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OJTMISApi.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAttendanceAdjustmentRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AttendanceAdjustmentRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AttendanceLogId = table.Column<int>(type: "int", nullable: false),
                    InternId = table.Column<int>(type: "int", nullable: false),
                    RequestedTimeOut = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TargetShift = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "AM_Out"),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceAdjustmentRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttendanceAdjustmentRequests_AspNetUsers_ReviewedBy",
                        column: x => x.ReviewedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AttendanceAdjustmentRequests_DailyTimeRecords_AttendanceLogId",
                        column: x => x.AttendanceLogId,
                        principalTable: "DailyTimeRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AttendanceAdjustmentRequests_Interns_InternId",
                        column: x => x.InternId,
                        principalTable: "Interns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceAdjustmentRequests_AttendanceLogId",
                table: "AttendanceAdjustmentRequests",
                column: "AttendanceLogId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceAdjustmentRequests_InternId_RequestedAt",
                table: "AttendanceAdjustmentRequests",
                columns: new[] { "InternId", "RequestedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceAdjustmentRequests_ReviewedBy",
                table: "AttendanceAdjustmentRequests",
                column: "ReviewedBy");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceAdjustmentRequests_Status_RequestedAt",
                table: "AttendanceAdjustmentRequests",
                columns: new[] { "Status", "RequestedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AttendanceAdjustmentRequests");
        }
    }
}
