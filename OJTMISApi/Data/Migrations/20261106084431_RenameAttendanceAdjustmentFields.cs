using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OJTMISApi.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameAttendanceAdjustmentFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TargetShift",
                table: "AttendanceAdjustmentRequests",
                newName: "TargetField");

            migrationBuilder.RenameColumn(
                name: "RequestedTimeOut",
                table: "AttendanceAdjustmentRequests",
                newName: "RequestedTime");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TargetField",
                table: "AttendanceAdjustmentRequests",
                newName: "TargetShift");

            migrationBuilder.RenameColumn(
                name: "RequestedTime",
                table: "AttendanceAdjustmentRequests",
                newName: "RequestedTimeOut");
        }
    }
}
