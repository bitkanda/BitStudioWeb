using Microsoft.EntityFrameworkCore.Migrations;

namespace bitkanda.Migrations
{
    public partial class airdrop : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AirDropTrans",
                columns: table => new
                {
                    ID = table.Column<long>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Address = table.Column<string>(type: "varchar(60)", nullable: true),
                    TxnHash = table.Column<string>(type: "varchar(80)", nullable: true),
                    TokenAmount = table.Column<string>(type: "varchar(100)", nullable: true),
                    AddDTM = table.Column<string>(type: "varchar(50)", nullable: true),
                    Message = table.Column<string>(nullable: true),
                    IsSuccess = table.Column<bool>(nullable: false),
                    SourceAddress = table.Column<string>(type: "varchar(60)", nullable: true),
                    ActivityCode = table.Column<string>(type: "varchar(60)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AirDropTrans", x => x.ID);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AirDropTrans");
        }
    }
}
