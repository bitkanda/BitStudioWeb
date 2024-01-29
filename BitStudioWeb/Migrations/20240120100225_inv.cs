using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace bitkanda.Migrations
{
    public partial class inv : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InvTotalMaster",
                columns: table => new
                {
                    ID = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserID = table.Column<long>(type: "INTEGER", nullable: false),
                    ProductId = table.Column<long>(type: "INTEGER", nullable: false),
                    SkuId = table.Column<long>(type: "INTEGER", nullable: false),
                    CreateTime = table.Column<DateTime>(type: "DateTime", nullable: false),
                    ModifyTime = table.Column<DateTime>(type: "DateTime", nullable: false),
                    Value = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    ExpDayTime = table.Column<DateTime>(type: "DateTime", nullable: false),
                    Count = table.Column<decimal>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvTotalMaster", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "InvUsedMaster",
                columns: table => new
                {
                    ID = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserID = table.Column<long>(type: "INTEGER", nullable: false),
                    ProductId = table.Column<long>(type: "INTEGER", nullable: false),
                    SkuId = table.Column<long>(type: "INTEGER", nullable: false),
                    Qty = table.Column<long>(type: "INTEGER", nullable: false),
                    CreateTime = table.Column<DateTime>(type: "DateTime", nullable: false),
                    Value = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    ExpDayTime = table.Column<decimal>(type: "DateTime", nullable: false),
                    Count = table.Column<decimal>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvUsedMaster", x => x.ID);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InvTotalMaster");

            migrationBuilder.DropTable(
                name: "InvUsedMaster");
        }
    }
}
