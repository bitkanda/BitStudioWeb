using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace bitkanda.Migrations
{
    public partial class AddUsedLogs : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UsedLogs",
                columns: table => new
                {
                    ID = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserName = table.Column<string>(type: "TEXT", nullable: false),
                    CreateTime = table.Column<DateTime>(type: "DateTime", nullable: false),
                    RequestID = table.Column<string>(type: "varchar(40)", nullable: false),
                    Model = table.Column<string>(type: "varchar(30)", nullable: false),
                    Object = table.Column<string>(type: "varchar(30)", nullable: false),
                    PromptTokens = table.Column<int>(type: "INTEGER", nullable: false),
                    CompletionTokens = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalTokens = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsedLogs", x => x.ID);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UsedLogs");
        }
    }
}
