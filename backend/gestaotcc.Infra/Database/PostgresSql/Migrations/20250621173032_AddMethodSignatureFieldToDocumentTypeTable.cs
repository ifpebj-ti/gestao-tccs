using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace gestaotcc.Infra.Database.PostgresSql.Migrations
{
    /// <inheritdoc />
    public partial class AddMethodSignatureFieldToDocumentTypeTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "File",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "IsSigned",
                table: "Documents");

            migrationBuilder.AddColumn<string>(
                name: "MethodSignature",
                table: "DocumentTypes",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MethodSignature",
                table: "DocumentTypes");

            migrationBuilder.AddColumn<byte[]>(
                name: "File",
                table: "Documents",
                type: "bytea",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<bool>(
                name: "IsSigned",
                table: "Documents",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
