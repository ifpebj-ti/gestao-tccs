using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using gestaotcc.Infra.Database;

#nullable disable

namespace gestaotcc.Infra.Database.PostgresSql.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260924000000_AddRejectionReasonToTcc")]
    public partial class AddRejectionReasonToTcc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "Tccs",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "Tccs");
        }
    }
}
