using Microsoft.EntityFrameworkCore.Migrations;

namespace bitkanda.Migrations
{
    public partial class Order_add_mobileAnd_BuyerMsg : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BuyerMsg",
                table: "Orders",
                type: "varchar(200)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Mobile",
                table: "Orders",
                type: "varchar(200)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SellerMsg",
                table: "Orders",
                type: "varchar(200)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BuyerMsg",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Mobile",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "SellerMsg",
                table: "Orders");
        }
    }
}
