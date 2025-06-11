using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace gestaotcc.Infra.Database.PostgresSql.Migrations
{
    /// <inheritdoc />
    public partial class updateUserTccs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "ProfileId",
                table: "UserTccs",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_UserTccs_ProfileId",
                table: "UserTccs",
                column: "ProfileId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserTccs_Profiles_ProfileId",
                table: "UserTccs",
                column: "ProfileId",
                principalTable: "Profiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserTccs_Profiles_ProfileId",
                table: "UserTccs");

            migrationBuilder.DropIndex(
                name: "IX_UserTccs_ProfileId",
                table: "UserTccs");

            migrationBuilder.DropColumn(
                name: "ProfileId",
                table: "UserTccs");
        }
    }
}
