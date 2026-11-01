using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OJTMISApi.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddActivityLogVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsVerified",
                table: "ActivityLogs",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsVerified",
                table: "ActivityLogs");
        }
    }
}
