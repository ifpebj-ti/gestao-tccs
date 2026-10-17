using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace gestaotcc.Infra.Database.PostgresSql.Migrations
{
    /// <inheritdoc />
    public partial class AddFinalStageFieldsToTcc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FinalSubmissionDeadline",
                table: "Tccs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FinalTccFile",
                table: "Tccs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ForwardedToLibraryAt",
                table: "Tccs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "HardCoverReceivedAt",
                table: "Tccs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "HardCoverReceivedByUserId",
                table: "Tccs",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LibraryCatalogCode",
                table: "Tccs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LibraryReceivedAt",
                table: "Tccs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "LibraryReceivedByUserId",
                table: "Tccs",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FinalSubmissionDeadline",
                table: "Tccs");

            migrationBuilder.DropColumn(
                name: "FinalTccFile",
                table: "Tccs");

            migrationBuilder.DropColumn(
                name: "ForwardedToLibraryAt",
                table: "Tccs");

            migrationBuilder.DropColumn(
                name: "HardCoverReceivedAt",
                table: "Tccs");

            migrationBuilder.DropColumn(
                name: "HardCoverReceivedByUserId",
                table: "Tccs");

            migrationBuilder.DropColumn(
                name: "LibraryCatalogCode",
                table: "Tccs");

            migrationBuilder.DropColumn(
                name: "LibraryReceivedAt",
                table: "Tccs");

            migrationBuilder.DropColumn(
                name: "LibraryReceivedByUserId",
                table: "Tccs");
        }
    }
}
