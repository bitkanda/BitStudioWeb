using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace bitkanda.Migrations
{
    public partial class order : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ProductId",
                table: "Orders",
                newName: "PayUserId");

            migrationBuilder.RenameColumn(
                name: "Price",
                table: "Orders",
                newName: "RetailAmount");

            migrationBuilder.AlterColumn<bool>(
                name: "IsPay",
                table: "Orders",
                type: "INTEGER",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifyTime",
                table: "Orders",
                type: "DateTime",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<decimal>(
                name: "PayAmount",
                table: "Orders",
                type: "decimal(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "PayOrderNo",
                table: "Orders",
                type: "varchar(200)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PromotionAmount",
                table: "Orders",
                type: "decimal(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "OrderDetals",
                columns: table => new
                {
                    ID = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OrderId = table.Column<long>(type: "INTEGER", nullable: false),
                    ProductId = table.Column<long>(type: "INTEGER", nullable: false),
                    SkuId = table.Column<long>(type: "INTEGER", nullable: false),
                    Qty = table.Column<long>(type: "INTEGER", nullable: false),
                    PromotionAmount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    CreateTime = table.Column<DateTime>(type: "DateTime", nullable: false),
                    PayAmount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    RetailAmount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Name = table.Column<string>(type: "varchar(100)", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Value = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    ExpDay = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Count = table.Column<decimal>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderDetals", x => x.ID);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderDetals");

            migrationBuilder.DropColumn(
                name: "ModifyTime",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PayAmount",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PayOrderNo",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PromotionAmount",
                table: "Orders");

            migrationBuilder.RenameColumn(
                name: "RetailAmount",
                table: "Orders",
                newName: "Price");

            migrationBuilder.RenameColumn(
                name: "PayUserId",
                table: "Orders",
                newName: "ProductId");

            migrationBuilder.AlterColumn<bool>(
                name: "IsPay",
                table: "Orders",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "INTEGER");
        }
    }
}
