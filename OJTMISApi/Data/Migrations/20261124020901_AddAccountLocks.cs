using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OJTMISApi.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountLocks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LockReason",
                table: "AspNetUsers",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LockedAt",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LockedByName",
                table: "AspNetUsers",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LockedByUserId",
                table: "AspNetUsers",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LockReason",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "LockedAt",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "LockedByName",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "LockedByUserId",
                table: "AspNetUsers");
        }
    }
}
