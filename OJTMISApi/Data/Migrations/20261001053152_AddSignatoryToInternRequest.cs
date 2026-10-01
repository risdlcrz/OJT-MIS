using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OJTMISApi.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSignatoryToInternRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SignatoryId",
                table: "InternRequests",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_InternRequests_SignatoryId",
                table: "InternRequests",
                column: "SignatoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_InternRequests_Signatories_SignatoryId",
                table: "InternRequests",
                column: "SignatoryId",
                principalTable: "Signatories",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InternRequests_Signatories_SignatoryId",
                table: "InternRequests");

            migrationBuilder.DropIndex(
                name: "IX_InternRequests_SignatoryId",
                table: "InternRequests");

            migrationBuilder.DropColumn(
                name: "SignatoryId",
                table: "InternRequests");
        }
    }
}
