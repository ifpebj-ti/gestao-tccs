using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace gestaotcc.Infra.Database.PostgresSql.Migrations
{
    /// <inheritdoc />
    public partial class LinkBankingMemberToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "UserId",
                table: "TccBankingMembers",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TccBankingMembers_UserId",
                table: "TccBankingMembers",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_TccBankingMembers_Users_UserId",
                table: "TccBankingMembers",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TccBankingMembers_Users_UserId",
                table: "TccBankingMembers");

            migrationBuilder.DropIndex(
                name: "IX_TccBankingMembers_UserId",
                table: "TccBankingMembers");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "TccBankingMembers");
        }
    }
}
