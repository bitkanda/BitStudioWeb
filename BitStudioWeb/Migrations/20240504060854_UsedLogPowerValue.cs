using Microsoft.EntityFrameworkCore.Migrations;

namespace bitkanda.Migrations
{
    public partial class UsedLogPowerValue : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Object",
                table: "UsedLogs",
                type: "varchar(100)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(30)");

            migrationBuilder.AddColumn<decimal>(
                name: "PowerValue",
                table: "UsedLogs",
                type: "decimal(10,2)",
                nullable: false,
                defaultValue: 0m);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PowerValue",
                table: "UsedLogs");

            migrationBuilder.AlterColumn<string>(
                name: "Object",
                table: "UsedLogs",
                type: "varchar(30)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(100)");
        }
    }
}
