using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace gestaotcc.Infra.Database.PostgresSql.Migrations
{
    /// <inheritdoc />
    public partial class RenameTccScheduleTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TccScheduleEntity_Tccs_TccId",
                table: "TccScheduleEntity");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TccScheduleEntity",
                table: "TccScheduleEntity");

            migrationBuilder.RenameTable(
                name: "TccScheduleEntity",
                newName: "TccSchedules");

            migrationBuilder.RenameIndex(
                name: "IX_TccScheduleEntity_TccId",
                table: "TccSchedules",
                newName: "IX_TccSchedules_TccId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TccSchedules",
                table: "TccSchedules",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TccSchedules_Tccs_TccId",
                table: "TccSchedules",
                column: "TccId",
                principalTable: "Tccs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TccSchedules_Tccs_TccId",
                table: "TccSchedules");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TccSchedules",
                table: "TccSchedules");

            migrationBuilder.RenameTable(
                name: "TccSchedules",
                newName: "TccScheduleEntity");

            migrationBuilder.RenameIndex(
                name: "IX_TccSchedules_TccId",
                table: "TccScheduleEntity",
                newName: "IX_TccScheduleEntity_TccId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TccScheduleEntity",
                table: "TccScheduleEntity",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TccScheduleEntity_Tccs_TccId",
                table: "TccScheduleEntity",
                column: "TccId",
                principalTable: "Tccs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
