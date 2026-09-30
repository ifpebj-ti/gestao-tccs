using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace gestaotcc.Infra.Database.PostgresSql.Migrations
{
    /// <inheritdoc />
    public partial class AddTccFileToTccEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TccFile",
                table: "Tccs",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TccFile",
                table: "Tccs");
        }
    }
}
