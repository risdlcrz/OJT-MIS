using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OJTMISApi.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateProgramModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Code",
                table: "Programs");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Programs");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "Programs",
                newName: "Level");

            migrationBuilder.RenameColumn(
                name: "DurationMonths",
                table: "Programs",
                newName: "Abbrev");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Level",
                table: "Programs",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "Abbrev",
                table: "Programs",
                newName: "DurationMonths");

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Programs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Programs",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
