using Microsoft.EntityFrameworkCore.Migrations;

namespace Prima.Migrations.TextBlacklist
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RegexStrings",
                columns: table => new
                {
                    RegexString = table.Column<string>(nullable: false),
                    GuildId = table.Column<ulong>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegexStrings", x => x.RegexString);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RegexStrings");
        }
    }
}
