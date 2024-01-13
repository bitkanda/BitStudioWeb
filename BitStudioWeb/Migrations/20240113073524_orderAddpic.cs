using Microsoft.EntityFrameworkCore.Migrations;

namespace bitkanda.Migrations
{
    public partial class orderAddpic : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImgUrl",
                table: "ProductSkus",
                type: "varchar(500)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ImgUrl",
                table: "Products",
                type: "varchar(500)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(60)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImgUrl",
                table: "OrderDetals",
                type: "varchar(500)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImgUrl",
                table: "ProductSkus");

            migrationBuilder.DropColumn(
                name: "ImgUrl",
                table: "OrderDetals");

            migrationBuilder.AlterColumn<string>(
                name: "ImgUrl",
                table: "Products",
                type: "varchar(60)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(500)",
                oldNullable: true);
        }
    }
}
