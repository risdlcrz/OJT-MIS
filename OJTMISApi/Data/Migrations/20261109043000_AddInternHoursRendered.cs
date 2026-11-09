using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OJTMISApi.Data;

#nullable disable

namespace OJTMISApi.Data.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261109043000_AddInternHoursRendered")]
    public partial class AddInternHoursRendered : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "HoursRendered",
                table: "Interns",
                type: "float",
                nullable: false,
                defaultValue: 0.0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "HoursRendered", table: "Interns");
        }
    }
}
