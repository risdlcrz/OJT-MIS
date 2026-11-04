using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OJTMISApi.Data;

#nullable disable

namespace OJTMISApi.Data.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261104160000_AddAttendanceShiftType")]
    public partial class AddAttendanceShiftType : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ShiftType",
                table: "DailyTimeRecords",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Morning");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "ShiftType", table: "DailyTimeRecords");
        }
    }
}
