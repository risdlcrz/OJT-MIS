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

            migrationBuilder.AddColumn<string>(
                name: "Abbrev",
                table: "Programs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE [Programs] SET [Abbrev] = CONVERT(nvarchar(max), [DurationMonths]) WHERE [DurationMonths] IS NOT NULL");

            migrationBuilder.DropColumn(
                name: "DurationMonths",
                table: "Programs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Level",
                table: "Programs",
                newName: "Status");

            migrationBuilder.AddColumn<int>(
                name: "DurationMonths",
                table: "Programs",
                type: "int",
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE [Programs] SET [DurationMonths] = TRY_CONVERT(int, [Abbrev])");

            migrationBuilder.DropColumn(
                name: "Abbrev",
                table: "Programs");

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
