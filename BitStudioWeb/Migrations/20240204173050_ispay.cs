using Microsoft.EntityFrameworkCore.Migrations;

namespace bitkanda.Migrations
{
    public partial class ispay : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPay",
                table: "UsedLogs",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPay",
                table: "UsedLogs");
        }
    }
}
