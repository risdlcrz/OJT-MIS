using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OJTMISApi.Data.Migrations
{
    /// <inheritdoc />
    public partial class LinkSignatoriesToUsersAndApprovalWorkflows : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApprovalScope",
                table: "Signatories",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Both");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Signatories",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SignatoryId",
                table: "GeneratedDocuments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ApproverSignatoryId",
                table: "AttendanceAdjustmentRequests",
                type: "int",
                nullable: true);

            // Backfill the credential link for existing directory entries whose
            // email already belongs to a login account. Only the first record
            // per account is linked so the filtered unique index below can be
            // created safely.
            migrationBuilder.Sql(
                """
                WITH matched AS (
                    SELECT s.Id AS SignatoryId,
                           u.Id AS UserId,
                           ROW_NUMBER() OVER (PARTITION BY u.Id ORDER BY s.Id) AS RowNumber
                    FROM Signatories AS s
                    INNER JOIN AspNetUsers AS u
                        ON LOWER(u.Email) = LOWER(s.Email)
                    WHERE s.UserId IS NULL
                      AND LTRIM(RTRIM(s.Email)) <> ''
                )
                UPDATE s
                SET s.UserId = m.UserId
                FROM Signatories AS s
                INNER JOIN matched AS m ON m.SignatoryId = s.Id
                WHERE m.RowNumber = 1;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Signatories_Department_ApprovalScope",
                table: "Signatories",
                columns: new[] { "Department", "ApprovalScope" });

            migrationBuilder.CreateIndex(
                name: "IX_Signatories_UserId",
                table: "Signatories",
                column: "UserId",
                unique: true,
                filter: "[UserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_GeneratedDocuments_SignatoryId",
                table: "GeneratedDocuments",
                column: "SignatoryId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceAdjustmentRequests_ApproverSignatoryId",
                table: "AttendanceAdjustmentRequests",
                column: "ApproverSignatoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceAdjustmentRequests_Signatories_ApproverSignatoryId",
                table: "AttendanceAdjustmentRequests",
                column: "ApproverSignatoryId",
                principalTable: "Signatories",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_GeneratedDocuments_Signatories_SignatoryId",
                table: "GeneratedDocuments",
                column: "SignatoryId",
                principalTable: "Signatories",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Signatories_AspNetUsers_UserId",
                table: "Signatories",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AttendanceAdjustmentRequests_Signatories_ApproverSignatoryId",
                table: "AttendanceAdjustmentRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_GeneratedDocuments_Signatories_SignatoryId",
                table: "GeneratedDocuments");

            migrationBuilder.DropForeignKey(
                name: "FK_Signatories_AspNetUsers_UserId",
                table: "Signatories");

            migrationBuilder.DropIndex(
                name: "IX_Signatories_Department_ApprovalScope",
                table: "Signatories");

            migrationBuilder.DropIndex(
                name: "IX_Signatories_UserId",
                table: "Signatories");

            migrationBuilder.DropIndex(
                name: "IX_GeneratedDocuments_SignatoryId",
                table: "GeneratedDocuments");

            migrationBuilder.DropIndex(
                name: "IX_AttendanceAdjustmentRequests_ApproverSignatoryId",
                table: "AttendanceAdjustmentRequests");

            migrationBuilder.DropColumn(
                name: "ApprovalScope",
                table: "Signatories");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Signatories");

            migrationBuilder.DropColumn(
                name: "SignatoryId",
                table: "GeneratedDocuments");

            migrationBuilder.DropColumn(
                name: "ApproverSignatoryId",
                table: "AttendanceAdjustmentRequests");
        }
    }
}
