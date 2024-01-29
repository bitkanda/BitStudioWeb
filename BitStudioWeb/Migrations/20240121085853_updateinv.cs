using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace bitkanda.Migrations
{
    public partial class updateinv : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserName",
                table: "UsedLogs");

            migrationBuilder.DropColumn(
                name: "Qty",
                table: "InvUsedMaster");

            migrationBuilder.AddColumn<long>(
                name: "UserID",
                table: "UsedLogs",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifyTime",
                table: "InvUsedMaster",
                type: "DateTime",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserID",
                table: "UsedLogs");

            migrationBuilder.DropColumn(
                name: "ModifyTime",
                table: "InvUsedMaster");

            migrationBuilder.AddColumn<string>(
                name: "UserName",
                table: "UsedLogs",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "Qty",
                table: "InvUsedMaster",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);
        }
    }
}
