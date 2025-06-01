using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace gestaotcc.Infra.Database.PostgresSql.Migrations
{
    /// <inheritdoc />
    public partial class v7 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "documentType_profile",
                columns: table => new
                {
                    DocumentTypesId = table.Column<long>(type: "bigint", nullable: false),
                    ProfilesId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_documentType_profile", x => new { x.DocumentTypesId, x.ProfilesId });
                    table.ForeignKey(
                        name: "FK_documentType_profile_DocumentTypes_DocumentTypesId",
                        column: x => x.DocumentTypesId,
                        principalTable: "DocumentTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_documentType_profile_Profiles_ProfilesId",
                        column: x => x.ProfilesId,
                        principalTable: "Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_documentType_profile_ProfilesId",
                table: "documentType_profile",
                column: "ProfilesId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "documentType_profile");
        }
    }
}
