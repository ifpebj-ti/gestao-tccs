using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace gestaotcc.Infra.Database.PostgresSql.Migrations
{
    /// <inheritdoc />
    public partial class AddSemester : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TccInvites_Tccs_TccId",
                table: "TccInvites");

            migrationBuilder.AddColumn<long>(
                name: "SemesterId",
                table: "Tccs",
                type: "bigint",
                nullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "TccId",
                table: "TccInvites",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<long>(
                name: "CourseId",
                table: "TccInvites",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<long>(
                name: "CampiId",
                table: "TccInvites",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.CreateTable(
                name: "Semesters",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Semesters", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tccs_SemesterId",
                table: "Tccs",
                column: "SemesterId");

            migrationBuilder.AddForeignKey(
                name: "FK_TccInvites_Tccs_TccId",
                table: "TccInvites",
                column: "TccId",
                principalTable: "Tccs",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Tccs_Semesters_SemesterId",
                table: "Tccs",
                column: "SemesterId",
                principalTable: "Semesters",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TccInvites_Tccs_TccId",
                table: "TccInvites");

            migrationBuilder.DropForeignKey(
                name: "FK_Tccs_Semesters_SemesterId",
                table: "Tccs");

            migrationBuilder.DropTable(
                name: "Semesters");

            migrationBuilder.DropIndex(
                name: "IX_Tccs_SemesterId",
                table: "Tccs");

            migrationBuilder.DropColumn(
                name: "SemesterId",
                table: "Tccs");

            migrationBuilder.AlterColumn<long>(
                name: "TccId",
                table: "TccInvites",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "CourseId",
                table: "TccInvites",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "CampiId",
                table: "TccInvites",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_TccInvites_Tccs_TccId",
                table: "TccInvites",
                column: "TccId",
                principalTable: "Tccs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
